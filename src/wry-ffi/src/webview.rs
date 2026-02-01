//! Webview operations
//!
//! Navigation, script execution, and webview control.

use std::ffi::c_char;

use crate::error::{error_result, set_last_error};
use crate::string::{c_str_to_string, string_to_c_string};
use crate::types::{WryErrorCode, WryResult, WryWindow};
use crate::window::get_window_state;

// ============================================================================
// FFI Functions
// ============================================================================

/// Navigate to URL
#[no_mangle]
pub unsafe extern "C" fn wry_webview_navigate(window: WryWindow, url: *const c_char) -> WryResult {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidHandle, "Invalid window handle"),
    };

    let url = match c_str_to_string(url) {
        Some(u) => u,
        None => return error_result(WryErrorCode::InvalidParameter, "Null or invalid URL"),
    };

    log::debug!("Navigating to: {}", url);

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return error_result(WryErrorCode::WebviewCreationFailed, "No webview available"),
    };

    match webview.load_url(&url) {
        Ok(()) => WryResult::ok(),
        Err(e) => error_result(WryErrorCode::NavigationFailed, format!("Navigation failed: {}", e)),
    }
}

/// Load HTML content directly
#[no_mangle]
pub unsafe extern "C" fn wry_webview_load_html(
    window: WryWindow,
    html: *const c_char,
) -> WryResult {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidHandle, "Invalid window handle"),
    };

    let html = match c_str_to_string(html) {
        Some(h) => h,
        None => return error_result(WryErrorCode::InvalidParameter, "Null or invalid HTML"),
    };

    log::debug!("Loading HTML content ({} bytes)", html.len());

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return error_result(WryErrorCode::WebviewCreationFailed, "No webview available"),
    };

    match webview.load_html(&html) {
        Ok(()) => WryResult::ok(),
        Err(e) => error_result(WryErrorCode::NavigationFailed, format!("Failed to load HTML: {}", e)),
    }
}

/// Execute JavaScript in webview context
#[no_mangle]
pub unsafe extern "C" fn wry_webview_evaluate_script(
    window: WryWindow,
    script: *const c_char,
) -> WryResult {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidHandle, "Invalid window handle"),
    };

    let script = match c_str_to_string(script) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidParameter, "Null or invalid script"),
    };

    log::debug!("Evaluating script ({} bytes)", script.len());

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return error_result(WryErrorCode::WebviewCreationFailed, "No webview available"),
    };

    match webview.evaluate_script(&script) {
        Ok(()) => WryResult::ok(),
        Err(e) => error_result(WryErrorCode::ScriptError, format!("Script execution failed: {}", e)),
    }
}

/// Send message to JavaScript (calls window.tauri.__receive)
///
/// This function is thread-safe - it can be called from any thread.
#[no_mangle]
pub unsafe extern "C" fn wry_webview_send_message(
    window: WryWindow,
    message: *const c_char,
) -> WryResult {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidHandle, "Invalid window handle"),
    };

    let message = match c_str_to_string(message) {
        Some(m) => m,
        None => return error_result(WryErrorCode::InvalidParameter, "Null or invalid message"),
    };

    log::debug!("Sending message to webview: {}", message);

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return error_result(WryErrorCode::WebviewCreationFailed, "No webview available"),
    };

    // Call the JavaScript receive function
    let script = format!(
        "if(window.tauri && window.tauri.__receive) {{ window.tauri.__receive({:?}); }}",
        message
    );

    match webview.evaluate_script(&script) {
        Ok(()) => WryResult::ok(),
        Err(e) => error_result(WryErrorCode::ScriptError, format!("Failed to send message: {}", e)),
    }
}

/// Get current URL (caller must free with wry_string_free)
#[no_mangle]
pub unsafe extern "C" fn wry_webview_get_url(window: WryWindow) -> *mut c_char {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => {
            set_last_error("Invalid window handle");
            return std::ptr::null_mut();
        }
    };

    let webview = match &state.webview {
        Some(wv) => wv,
        None => {
            set_last_error("No webview available");
            return std::ptr::null_mut();
        }
    };

    match webview.url() {
        Ok(url) => string_to_c_string(url.as_str()),
        Err(e) => {
            set_last_error(format!("Failed to get URL: {}", e));
            std::ptr::null_mut()
        }
    }
}

/// Set zoom level (1.0 = 100%)
#[no_mangle]
pub unsafe extern "C" fn wry_webview_set_zoom(window: WryWindow, zoom: f64) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Setting zoom to: {}", zoom);

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return,
    };

    if let Err(e) = webview.zoom(zoom) {
        log::error!("Failed to set zoom: {}", e);
    }
}

/// Open devtools (if enabled)
#[no_mangle]
pub unsafe extern "C" fn wry_webview_open_devtools(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Opening devtools");

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return,
    };

    // Devtools are enabled in wry via the "devtools" feature we include
    webview.open_devtools();
}

/// Close devtools
#[no_mangle]
pub unsafe extern "C" fn wry_webview_close_devtools(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Closing devtools");

    let webview = match &state.webview {
        Some(wv) => wv,
        None => return,
    };

    webview.close_devtools();
}
