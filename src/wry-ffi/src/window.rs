//! Window state and management
//!
//! Creates and manages windows with associated webviews.

use std::borrow::Cow;
use std::ffi::CStr;
use std::os::raw::c_void;

use http::{Response, StatusCode};
use tao::dpi::{LogicalPosition, LogicalSize};
use tao::event_loop::EventLoopProxy;
use tao::window::{Window, WindowBuilder, WindowId};
use wry::{WebView, WebViewBuilder};

use crate::app::{AppState, UserEvent};
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
    /// Event loop proxy for thread-safe operations
    pub event_loop_proxy: EventLoopProxy<UserEvent>,
}

impl WindowState {
    /// Get the window handle as a raw pointer
    /// Since WindowState is stored in Box, this pointer is stable
    pub fn as_ptr(&self) -> WryWindow {
        self as *const WindowState as *mut c_void
    }

    /// Request destruction via event loop (thread-safe)
    pub fn request_destroy(&self) {
        let _ = self.event_loop_proxy.send_event(UserEvent::DestroyWindow(self.id));
    }

    /// Send a message to the webview via event loop (thread-safe)
    pub fn send_message(&self, message: String) {
        let _ = self.event_loop_proxy.send_event(UserEvent::WebViewMessage {
            window_id: self.id,
            message,
        });
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

    // Build webview with IPC handler wired to callbacks
    let proxy = state.event_loop_proxy.clone();

    // Clone the custom protocols for the webview
    // We need to clone the HashMap since we can't borrow state during window creation
    let protocols: Vec<_> = state.custom_protocols.iter()
        .map(|(scheme, handler)| (scheme.clone(), handler.callback, handler.user_data))
        .collect();

    log::info!("Passing {} custom protocol(s) to webview", protocols.len());
    for (scheme, _, _) in &protocols {
        log::debug!("  - scheme: {}", scheme);
    }

    let webview = create_webview_for_window(&window, params, window_id, proxy.clone(), &protocols);

    let window_state = Box::new(WindowState {
        id: window_id,
        window,
        webview,
        callbacks: WindowCallbacks::new(window_id),
        event_loop_proxy: proxy,
    });

    // Get pointer before moving into hashmap
    let ptr = window_state.as_ptr();

    // Store in app state
    state.windows.insert(window_id, window_state);

    ptr
}

/// Protocol info tuple: (scheme, callback, user_data)
type ProtocolInfo = (String, crate::types::CustomProtocolCallback, *mut c_void);

/// Create a webview for a window
fn create_webview_for_window(
    window: &Window,
    params: &WryWindowParams,
    window_id: WindowId,
    _proxy: EventLoopProxy<UserEvent>,
    protocols: &[ProtocolInfo],
) -> Option<WebView> {
    let mut builder = WebViewBuilder::new();

    // Inject the JavaScript bridge as an initialization script
    builder = builder.with_initialization_script(crate::bridge::BRIDGE_SCRIPT);

    // Register custom protocols BEFORE setting URL
    // Use async protocol handler - required for WebKitGTK to work properly
    for (scheme, callback, user_data) in protocols.iter().cloned() {
        log::info!("Registering custom protocol '{}' with webview (async)", scheme);
        builder = builder.with_asynchronous_custom_protocol(
            scheme.clone(),
            move |_webview_id, request, responder| {
                let uri = request.uri().to_string();
                log::info!("Custom protocol request: {}", uri);

                // Prepare output variables
                let mut out_data: *const u8 = std::ptr::null();
                let mut out_len: usize = 0;
                let mut out_mime_type: *const std::ffi::c_char = std::ptr::null();

                // Convert URI to C string
                let uri_cstring = match std::ffi::CString::new(uri.clone()) {
                    Ok(s) => s,
                    Err(_) => {
                        log::error!("Failed to convert URI to CString");
                        responder.respond(
                            Response::builder()
                                .status(StatusCode::INTERNAL_SERVER_ERROR)
                                .body(Cow::Borrowed(&[] as &[u8]))
                                .unwrap(),
                        );
                        return;
                    }
                };

                // Call the C# callback
                let handled = callback(
                    std::ptr::null_mut(), // window handle - not available here
                    uri_cstring.as_ptr(),
                    &mut out_data as *mut *const u8 as *mut *const u8,
                    &mut out_len,
                    &mut out_mime_type as *mut *const std::ffi::c_char as *mut *const std::ffi::c_char,
                    user_data,
                );

                if !handled || out_data.is_null() {
                    log::debug!("Protocol handler returned not handled for: {}", uri);
                    responder.respond(
                        Response::builder()
                            .status(StatusCode::NOT_FOUND)
                            .body(Cow::Borrowed(&[] as &[u8]))
                            .unwrap(),
                    );
                    return;
                }

                // Copy data from C# allocated memory
                let body = unsafe { std::slice::from_raw_parts(out_data, out_len).to_vec() };

                // Get MIME type
                let mime_type = if !out_mime_type.is_null() {
                    unsafe { CStr::from_ptr(out_mime_type).to_string_lossy().into_owned() }
                } else {
                    "application/octet-stream".to_string()
                };

                log::info!("Protocol response: {} bytes, mime: {}", body.len(), mime_type);

                responder.respond(
                    Response::builder()
                        .status(StatusCode::OK)
                        .header("Content-Type", mime_type)
                        .body(Cow::Owned(body))
                        .unwrap(),
                );
            },
        );
    }

    // Set URL or HTML
    let url = unsafe { c_str_to_string(params.url) };
    let html = unsafe { c_str_to_string(params.html) };
    let user_agent = unsafe { c_str_to_string(params.user_agent) };

    if let Some(url) = url {
        log::info!("Setting webview URL: {}", url);
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
    // Note: We can't directly access callbacks here since WindowState doesn't exist yet.
    // The IPC messages need to be routed through a registry or the event loop.
    // For now, we log and the actual callback invocation happens via the callbacks system.
    let wid = window_id;
    builder = builder.with_ipc_handler(move |req| {
        let body = req.body();
        log::debug!("IPC message received from window {:?}: {}", wid, body);
        // Callback invocation is handled by WindowCallbacks which has access to the window pointer
        // The C# side needs to set up callbacks that will be invoked
        // For direct callback access, we'd need a global callback registry
        crate::callbacks::invoke_message_callback(wid, body);
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
/// This function is thread-safe - it dispatches via the event loop.
///
/// # Safety
/// The window handle must not be used after this call.
#[no_mangle]
pub unsafe extern "C" fn wry_window_destroy(window: WryWindow) {
    if window.is_null() {
        return;
    }

    log::info!("wry_window_destroy called");
    let window_state = &*(window as *const WindowState);

    // Request destruction via event loop (thread-safe)
    window_state.request_destroy();
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
