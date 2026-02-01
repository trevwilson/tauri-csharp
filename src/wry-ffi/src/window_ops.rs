//! Window state management operations
//!
//! Functions for controlling window visibility, size, position, and state.

use std::ffi::c_char;

use tao::dpi::{LogicalPosition, LogicalSize};
use tao::window::Fullscreen;

use crate::error::set_last_error;
use crate::string::{c_str_to_string, string_to_c_string};
use crate::types::{WryPosition, WrySize, WryWindow};
use crate::window::get_window_state;

// ============================================================================
// Visibility
// ============================================================================

/// Show/hide window
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_visible(window: WryWindow, visible: bool) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Setting window visibility: {}", visible);
    state.window.set_visible(visible);
}

/// Get window visibility
#[no_mangle]
pub unsafe extern "C" fn wry_window_is_visible(window: WryWindow) -> bool {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return false,
    };

    state.window.is_visible()
}

// ============================================================================
// Title
// ============================================================================

/// Set window title
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_title(window: WryWindow, title: *const c_char) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    let title = match c_str_to_string(title) {
        Some(t) => t,
        None => return,
    };

    log::debug!("Setting window title: {}", title);
    state.window.set_title(&title);
}

/// Get window title (caller must free with wry_string_free)
#[no_mangle]
pub unsafe extern "C" fn wry_window_get_title(window: WryWindow) -> *mut c_char {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => {
            set_last_error("Invalid window handle");
            return std::ptr::null_mut();
        }
    };

    string_to_c_string(&state.window.title())
}

// ============================================================================
// Size
// ============================================================================

/// Set window size
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_size(window: WryWindow, size: WrySize) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Setting window size: {}x{}", size.width, size.height);
    state
        .window
        .set_inner_size(LogicalSize::new(size.width, size.height));
}

/// Get window size
#[no_mangle]
pub unsafe extern "C" fn wry_window_get_size(window: WryWindow) -> WrySize {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return WrySize::default(),
    };

    let size = state.window.inner_size();
    WrySize {
        width: size.width,
        height: size.height,
    }
}

// ============================================================================
// Position
// ============================================================================

/// Set window position
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_position(window: WryWindow, pos: WryPosition) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Setting window position: ({}, {})", pos.x, pos.y);
    state
        .window
        .set_outer_position(LogicalPosition::new(pos.x, pos.y));
}

/// Get window position
#[no_mangle]
pub unsafe extern "C" fn wry_window_get_position(window: WryWindow) -> WryPosition {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return WryPosition::default(),
    };

    match state.window.outer_position() {
        Ok(pos) => WryPosition {
            x: pos.x,
            y: pos.y,
        },
        Err(_) => WryPosition::default(),
    }
}

// ============================================================================
// Window State
// ============================================================================

/// Minimize window
#[no_mangle]
pub unsafe extern "C" fn wry_window_minimize(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Minimizing window");
    state.window.set_minimized(true);
}

/// Maximize window
#[no_mangle]
pub unsafe extern "C" fn wry_window_maximize(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Maximizing window");
    state.window.set_maximized(true);
}

/// Unmaximize window
#[no_mangle]
pub unsafe extern "C" fn wry_window_unmaximize(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Unmaximizing window");
    state.window.set_maximized(false);
}

/// Set fullscreen mode
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_fullscreen(window: WryWindow, fullscreen: bool) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Setting fullscreen: {}", fullscreen);
    if fullscreen {
        state
            .window
            .set_fullscreen(Some(Fullscreen::Borderless(None)));
    } else {
        state.window.set_fullscreen(None);
    }
}

/// Focus window
#[no_mangle]
pub unsafe extern "C" fn wry_window_focus(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Focusing window");
    state.window.set_focus();
}

/// Close window
#[no_mangle]
pub unsafe extern "C" fn wry_window_close(window: WryWindow) {
    let state = match get_window_state(window) {
        Some(s) => s,
        None => return,
    };

    log::debug!("Closing window");
    // Note: This doesn't actually close the window immediately,
    // it just hides it. The actual removal happens in the event loop
    // when CloseRequested is received.
    state.window.set_visible(false);
}
