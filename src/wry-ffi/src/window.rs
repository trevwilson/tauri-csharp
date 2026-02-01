//! Window state and management
//!
//! Creates and manages windows with associated webviews.

use std::os::raw::c_void;

use tao::dpi::{LogicalPosition, LogicalSize};
use tao::window::{Window, WindowBuilder, WindowId};
use wry::{WebView, WebViewBuilder};

use crate::app::AppState;
use crate::callbacks::WindowCallbacks;
use crate::error::set_last_error;
use crate::string::c_str_to_string;
use crate::types::{WryApp, WryWindow, WryWindowParams};

/// State for a single window
pub struct WindowState {
    /// Window identifier
    pub id: WindowId,
    /// The Tao window
    pub window: Window,
    /// The Wry webview (optional, created with window)
    pub webview: Option<WebView>,
    /// Registered callbacks
    pub callbacks: WindowCallbacks,
}

impl WindowState {
    /// Get the window handle as a raw pointer
    pub fn as_ptr(&self) -> WryWindow {
        self as *const WindowState as *mut c_void
    }
}

// ============================================================================
// FFI Functions
// ============================================================================

/// Create a new window with webview
///
/// # Safety
/// Must be called on main thread with a valid app handle.
#[no_mangle]
pub unsafe extern "C" fn wry_window_create(
    app: WryApp,
    params: *const WryWindowParams,
) -> WryWindow {
    if app.is_null() {
        set_last_error("Null app handle");
        return std::ptr::null_mut();
    }

    if params.is_null() {
        set_last_error("Null params");
        return std::ptr::null_mut();
    }

    log::info!("wry_window_create called");
    let state = &mut *(app as *mut AppState);
    let params = &*params;

    // Get the event loop for window creation
    let event_loop = match &state.event_loop {
        Some(el) => el,
        None => {
            set_last_error("Event loop not available - app may already be running");
            return std::ptr::null_mut();
        }
    };

    // Build window
    let title = c_str_to_string(params.title).unwrap_or_else(|| "Untitled".to_string());
    log::debug!("Creating window with title: {}", title);

    let mut window_builder = WindowBuilder::new()
        .with_title(&title)
        .with_inner_size(LogicalSize::new(params.width, params.height))
        .with_resizable(params.resizable)
        .with_decorations(params.decorations)
        .with_visible(params.visible)
        .with_always_on_top(params.always_on_top);

    // Set position if specified
    if params.x != 0 || params.y != 0 {
        window_builder = window_builder.with_position(LogicalPosition::new(params.x, params.y));
    }

    // Set min/max size if specified
    if params.min_width > 0 && params.min_height > 0 {
        window_builder =
            window_builder.with_min_inner_size(LogicalSize::new(params.min_width, params.min_height));
    }
    if params.max_width > 0 && params.max_height > 0 {
        window_builder =
            window_builder.with_max_inner_size(LogicalSize::new(params.max_width, params.max_height));
    }

    if params.maximized {
        window_builder = window_builder.with_maximized(true);
    }

    if params.fullscreen {
        window_builder =
            window_builder.with_fullscreen(Some(tao::window::Fullscreen::Borderless(None)));
    }

    if params.transparent {
        window_builder = window_builder.with_transparent(true);
    }

    // Create the window
    let window = match window_builder.build(event_loop) {
        Ok(w) => w,
        Err(e) => {
            set_last_error(format!("Failed to create window: {}", e));
            return std::ptr::null_mut();
        }
    };

    let window_id = window.id();
    log::debug!("Window created with id: {:?}", window_id);

    // Build webview
    let webview = create_webview_for_window(&window, params, state);

    let window_state = WindowState {
        id: window_id,
        window,
        webview,
        callbacks: WindowCallbacks::default(),
    };

    // Store and return
    state.windows.insert(window_id, window_state);

    // Return pointer to the stored WindowState
    state.windows.get(&window_id).unwrap().as_ptr()
}

/// Create a webview for a window
fn create_webview_for_window(
    window: &Window,
    params: &WryWindowParams,
    _state: &AppState,
) -> Option<WebView> {
    let mut builder = WebViewBuilder::new();

    // Inject the JavaScript bridge as an initialization script
    builder = builder.with_initialization_script(crate::bridge::BRIDGE_SCRIPT);

    // Set URL or HTML
    let url = unsafe { c_str_to_string(params.url) };
    let html = unsafe { c_str_to_string(params.html) };
    let user_agent = unsafe { c_str_to_string(params.user_agent) };

    if let Some(url) = url {
        log::debug!("Setting webview URL: {}", url);
        builder = builder.with_url(&url);
    } else if let Some(html) = html {
        log::debug!("Setting webview HTML content");
        builder = builder.with_html(&html);
    }

    if let Some(ua) = user_agent {
        builder = builder.with_user_agent(&ua);
    }

    // Enable devtools in debug or if explicitly requested
    #[cfg(debug_assertions)]
    {
        builder = builder.with_devtools(true);
    }
    #[cfg(not(debug_assertions))]
    {
        builder = builder.with_devtools(params.devtools_enabled);
    }

    if params.transparent {
        builder = builder.with_transparent(true);
    }

    // Add IPC handler for messages from JavaScript
    builder = builder.with_ipc_handler(move |req| {
        let body = req.body();
        log::debug!("IPC message received: {}", body);
        // TODO: Connect to window callbacks for message handling
    });

    // Build the webview
    #[cfg(not(target_os = "linux"))]
    let result = builder.build(window);

    #[cfg(target_os = "linux")]
    let result = {
        use tao::platform::unix::WindowExtUnix;
        use wry::WebViewBuilderExtUnix;

        let vbox = window.default_vbox();
        match vbox {
            Some(vbox) => builder.build_gtk(vbox),
            None => {
                log::error!("Failed to get GTK vbox from window");
                return None;
            }
        }
    };

    match result {
        Ok(webview) => {
            log::info!("Webview created successfully");
            Some(webview)
        }
        Err(e) => {
            log::error!("Failed to create webview: {}", e);
            None
        }
    }
}

/// Destroy window and free resources
///
/// # Safety
/// The window handle must not be used after this call.
#[no_mangle]
pub unsafe extern "C" fn wry_window_destroy(window: WryWindow) {
    if window.is_null() {
        return;
    }

    log::info!("wry_window_destroy called");

    // We don't actually free here - the window is owned by AppState
    // Instead, we just remove it from the map when we have app access
    // For now, just mark it as needing destruction
    let window_state = &*(window as *const WindowState);
    log::debug!("Window {:?} marked for destruction", window_state.id);
}

/// Helper to get WindowState from handle with validation
pub unsafe fn get_window_state<'a>(window: WryWindow) -> Option<&'a WindowState> {
    if window.is_null() {
        set_last_error("Null window handle");
        return None;
    }
    Some(&*(window as *const WindowState))
}

/// Helper to get mutable WindowState from handle with validation
pub unsafe fn get_window_state_mut<'a>(window: WryWindow) -> Option<&'a mut WindowState> {
    if window.is_null() {
        set_last_error("Null window handle");
        return None;
    }
    Some(&mut *(window as *mut WindowState))
}
