//! Application state and event loop management
//!
//! Manages the Tao event loop and window registry.

use std::collections::HashMap;
use std::ffi::c_char;

use tao::event::{Event, StartCause, WindowEvent};
use tao::event_loop::{ControlFlow, EventLoop, EventLoopBuilder, EventLoopProxy};
use tao::platform::run_return::EventLoopExtRunReturn;
use tao::window::WindowId;

use crate::error::{error_result, get_last_error_ptr, set_last_error};
use crate::types::{WryApp, WryErrorCode, WryResult};
use crate::window::WindowState;

/// User events for cross-thread communication
pub enum UserEvent {
    /// Request to quit the application
    Quit,
    /// Invoke a callback on the UI thread
    InvokeCallback(Box<dyn FnOnce() + Send>),
    /// Send a message to a webview (thread-safe)
    WebViewMessage { window_id: WindowId, message: String },
    /// Close/destroy a specific window
    DestroyWindow(WindowId),
}

impl std::fmt::Debug for UserEvent {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            UserEvent::Quit => write!(f, "Quit"),
            UserEvent::InvokeCallback(_) => write!(f, "InvokeCallback(<fn>)"),
            UserEvent::WebViewMessage { window_id, message } => {
                write!(f, "WebViewMessage {{ window_id: {:?}, message: {:?} }}", window_id, message)
            }
            UserEvent::DestroyWindow(id) => write!(f, "DestroyWindow({:?})", id),
        }
    }
}

/// Application state holding the event loop and windows
pub struct AppState {
    /// The event loop (taken during run)
    pub event_loop: Option<EventLoop<UserEvent>>,
    /// Proxy for sending events from other threads
    pub event_loop_proxy: EventLoopProxy<UserEvent>,
    /// All windows managed by this app - boxed for stable addresses
    pub windows: HashMap<WindowId, Box<WindowState>>,
    /// Custom protocol handlers (scheme -> handler)
    pub custom_protocols: HashMap<String, ProtocolHandler>,
    /// Whether we should quit
    pub should_quit: bool,
}

/// A custom protocol handler
pub struct ProtocolHandler {
    pub callback: crate::types::CustomProtocolCallback,
    pub user_data: *mut std::os::raw::c_void,
}

// Safety: user_data is managed by the caller
unsafe impl Send for ProtocolHandler {}
unsafe impl Sync for ProtocolHandler {}

impl AppState {
    /// Create a new app state
    pub fn new() -> Result<Self, String> {
        let event_loop = EventLoopBuilder::<UserEvent>::with_user_event().build();
        let proxy = event_loop.create_proxy();

        Ok(Self {
            event_loop: Some(event_loop),
            event_loop_proxy: proxy,
            windows: HashMap::new(),
            custom_protocols: HashMap::new(),
            should_quit: false,
        })
    }

    /// Get a window by ID
    pub fn get_window(&self, id: WindowId) -> Option<&WindowState> {
        self.windows.get(&id).map(|b| b.as_ref())
    }

    /// Get a mutable window by ID
    pub fn get_window_mut(&mut self, id: WindowId) -> Option<&mut WindowState> {
        self.windows.get_mut(&id).map(|b| b.as_mut())
    }

    /// Request quit
    pub fn quit(&mut self) {
        self.should_quit = true;
        let _ = self.event_loop_proxy.send_event(UserEvent::Quit);
    }
}

// ============================================================================
// FFI Functions
// ============================================================================

/// Initialize the application. Must be called first, on main thread.
///
/// # Returns
/// App handle or NULL on failure
#[no_mangle]
pub extern "C" fn wry_app_create() -> WryApp {
    // Initialize logging
    let _ = env_logger::try_init();
    log::info!("wry_app_create called");

    #[cfg(target_os = "linux")]
    {
        if gtk::init().is_err() {
            set_last_error("Failed to initialize GTK");
            return std::ptr::null_mut();
        }
        log::debug!("GTK initialized");
    }

    match AppState::new() {
        Ok(state) => {
            let boxed = Box::new(state);
            log::info!("App created successfully");
            Box::into_raw(boxed) as WryApp
        }
        Err(e) => {
            set_last_error(format!("Failed to create app: {}", e));
            std::ptr::null_mut()
        }
    }
}

/// Run the event loop. Blocks until all windows closed or wry_app_quit called.
///
/// # Safety
/// Must be called on main thread with a valid app handle.
#[no_mangle]
pub unsafe extern "C" fn wry_app_run(app: WryApp) -> WryResult {
    if app.is_null() {
        return error_result(WryErrorCode::InvalidHandle, "Null app handle");
    }

    log::info!("wry_app_run called");
    let state = &mut *(app as *mut AppState);

    let mut event_loop = match state.event_loop.take() {
        Some(el) => el,
        None => {
            return error_result(
                WryErrorCode::EventLoopError,
                "Event loop already consumed or running",
            );
        }
    };

    // Use run_return to allow returning control
    // We need to use a raw pointer to access state inside the closure
    let state_ptr = app as *mut AppState;

    let exit_code = event_loop.run_return(move |event, _event_loop, control_flow| {
        // Get state - safe because we're on the main thread
        let state = &mut *state_ptr;

        // Process GTK events on Linux
        #[cfg(target_os = "linux")]
        while gtk::events_pending() {
            gtk::main_iteration_do(false);
        }

        *control_flow = ControlFlow::Wait;

        match event {
            Event::NewEvents(StartCause::Init) => {
                log::debug!("Event loop initialized");
            }

            Event::UserEvent(user_event) => {
                handle_user_event(state, user_event, control_flow);
            }

            Event::WindowEvent { window_id, event, .. } => {
                handle_window_event(state, window_id, event, control_flow);
            }

            Event::LoopDestroyed => {
                log::info!("Event loop destroyed");
            }

            _ => {}
        }

        // Check if we should quit
        if state.should_quit || state.windows.is_empty() {
            *control_flow = ControlFlow::Exit;
        }
    });

    if exit_code != 0 {
        return error_result(
            WryErrorCode::EventLoopError,
            format!("Event loop exited with code: {}", exit_code),
        );
    }

    WryResult::ok()
}

/// Handle user events (from other threads)
fn handle_user_event(state: &mut AppState, event: UserEvent, control_flow: &mut ControlFlow) {
    match event {
        UserEvent::Quit => {
            log::info!("Quit requested");
            state.should_quit = true;
            *control_flow = ControlFlow::Exit;
        }

        UserEvent::InvokeCallback(callback) => {
            log::debug!("Invoking callback on UI thread");
            callback();
        }

        UserEvent::WebViewMessage { window_id, message } => {
            if let Some(window_state) = state.windows.get(&window_id) {
                if let Some(webview) = &window_state.webview {
                    let script = format!(
                        "if(window.tauri && window.tauri.__receive) {{ window.tauri.__receive({:?}); }}",
                        message
                    );
                    if let Err(e) = webview.evaluate_script(&script) {
                        log::error!("Failed to send message to webview: {}", e);
                    }
                }
            }
        }

        UserEvent::DestroyWindow(window_id) => {
            log::debug!("Destroy window requested: {:?}", window_id);
            state.windows.remove(&window_id);
        }
    }
}

/// Handle window events
fn handle_window_event(
    state: &mut AppState,
    window_id: WindowId,
    event: WindowEvent,
    _control_flow: &mut ControlFlow,
) {
    // For CloseRequested, we need to check callback first, then maybe remove
    if let WindowEvent::CloseRequested = &event {
        let should_close = state
            .windows
            .get(&window_id)
            .map(|ws| ws.callbacks.call_closing())
            .unwrap_or(true);

        if should_close {
            log::debug!("Window close requested and approved: {:?}", window_id);
            state.windows.remove(&window_id);
        }
        return;
    }

    // For other events, get mutable reference
    let window_state = match state.windows.get_mut(&window_id) {
        Some(ws) => ws,
        None => return,
    };

    match event {
        WindowEvent::Resized(size) => {
            let (width, height) = size.into();
            log::debug!("Window resized: {:?} -> {}x{}", window_id, width, height);
            window_state.callbacks.call_resized(width, height);
        }

        WindowEvent::Moved(position) => {
            let (x, y) = position.into();
            log::debug!("Window moved: {:?} -> ({}, {})", window_id, x, y);
            window_state.callbacks.call_moved(x, y);
        }

        WindowEvent::Focused(focused) => {
            log::debug!("Window focus changed: {:?} -> {}", window_id, focused);
            window_state.callbacks.call_focus(focused);
        }

        _ => {}
    }
}

/// Request app to quit
///
/// # Safety
/// Must be called with a valid app handle.
#[no_mangle]
pub unsafe extern "C" fn wry_app_quit(app: WryApp) {
    if app.is_null() {
        return;
    }

    log::info!("wry_app_quit called");
    let state = &mut *(app as *mut AppState);
    state.quit();
}

/// Destroy app and free resources
///
/// # Safety
/// The app handle must not be used after this call.
#[no_mangle]
pub unsafe extern "C" fn wry_app_destroy(app: WryApp) {
    if app.is_null() {
        return;
    }

    log::info!("wry_app_destroy called");
    let _ = Box::from_raw(app as *mut AppState);
}

/// Get last error message (valid until next wry_* call)
#[no_mangle]
pub extern "C" fn wry_get_last_error() -> *const c_char {
    get_last_error_ptr()
}

/// Get version string
#[no_mangle]
pub extern "C" fn wry_version() -> *const c_char {
    static VERSION: &str = concat!(env!("CARGO_PKG_VERSION"), "\0");
    VERSION.as_ptr() as *const c_char
}
