//! FFI type definitions for wry-ffi
//!
//! All types are C-compatible and use explicit memory layout.

use std::ffi::c_char;
use std::os::raw::c_void;

/// Opaque handle to the application/event loop
pub type WryApp = *mut c_void;

/// Opaque handle to a window with webview
pub type WryWindow = *mut c_void;

/// Window creation parameters
#[repr(C)]
pub struct WryWindowParams {
    // Strings (UTF-8, null-terminated)
    pub title: *const c_char,
    pub url: *const c_char,
    pub html: *const c_char,
    pub user_agent: *const c_char,
    pub data_directory: *const c_char,

    // Dimensions
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub min_width: u32,
    pub min_height: u32,
    pub max_width: u32,  // 0 = no max
    pub max_height: u32, // 0 = no max

    // Flags
    pub resizable: bool,
    pub fullscreen: bool,
    pub maximized: bool,
    pub minimized: bool,
    pub visible: bool,
    pub transparent: bool,
    pub decorations: bool,
    pub always_on_top: bool,
    pub devtools_enabled: bool,
    pub autoplay_enabled: bool,
}

impl Default for WryWindowParams {
    fn default() -> Self {
        Self {
            title: std::ptr::null(),
            url: std::ptr::null(),
            html: std::ptr::null(),
            user_agent: std::ptr::null(),
            data_directory: std::ptr::null(),
            x: 0,
            y: 0,
            width: 800,
            height: 600,
            min_width: 0,
            min_height: 0,
            max_width: 0,
            max_height: 0,
            resizable: true,
            fullscreen: false,
            maximized: false,
            minimized: false,
            visible: true,
            transparent: false,
            decorations: true,
            always_on_top: false,
            devtools_enabled: true,
            autoplay_enabled: false,
        }
    }
}

/// Window size
#[repr(C)]
#[derive(Debug, Clone, Copy, Default)]
pub struct WrySize {
    pub width: u32,
    pub height: u32,
}

/// Window position
#[repr(C)]
#[derive(Debug, Clone, Copy, Default)]
pub struct WryPosition {
    pub x: i32,
    pub y: i32,
}

/// Result type for FFI operations
#[repr(C)]
pub struct WryResult {
    pub success: bool,
    pub error_code: i32,
    pub error_message: *const c_char,
}

impl WryResult {
    pub fn ok() -> Self {
        Self {
            success: true,
            error_code: 0,
            error_message: std::ptr::null(),
        }
    }

    pub fn err(code: i32) -> Self {
        Self {
            success: false,
            error_code: code,
            error_message: std::ptr::null(), // Set via get_last_error
        }
    }
}

impl Default for WryResult {
    fn default() -> Self {
        Self::ok()
    }
}

/// Error codes
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum WryErrorCode {
    Success = 0,
    InvalidHandle = 1,
    WindowCreationFailed = 2,
    WebviewCreationFailed = 3,
    NavigationFailed = 4,
    ScriptError = 5,
    ProtocolError = 6,
    InvalidParameter = 7,
    NotSupported = 8,
    DialogCancelled = 9,
    NotificationFailed = 10,
    IconLoadFailed = 11,
    EventLoopError = 12,
    Unknown = 255,
}

// ============================================================================
// Callback Types
// ============================================================================

/// Called when webview sends a message to backend
pub type WebMessageCallback = extern "C" fn(
    window: WryWindow,
    message: *const c_char,
    user_data: *mut c_void,
);

/// Called when custom protocol request is made
pub type CustomProtocolCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    out_data: *mut *const u8,
    out_len: *mut usize,
    out_mime_type: *mut *const c_char,
    user_data: *mut c_void,
) -> bool;

/// Called when window is closing (return false to prevent)
pub type WindowClosingCallback =
    extern "C" fn(window: WryWindow, user_data: *mut c_void) -> bool;

/// Called when window is resized
pub type WindowResizedCallback = extern "C" fn(
    window: WryWindow,
    width: u32,
    height: u32,
    user_data: *mut c_void,
);

/// Called when window is moved
pub type WindowMovedCallback =
    extern "C" fn(window: WryWindow, x: i32, y: i32, user_data: *mut c_void);

/// Called when window focus changes
pub type WindowFocusCallback =
    extern "C" fn(window: WryWindow, focused: bool, user_data: *mut c_void);

/// Called when navigation starts (return false to cancel)
pub type NavigationCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    user_data: *mut c_void,
) -> bool;

/// Callback for UI thread invocation
pub type InvokeCallback = extern "C" fn(user_data: *mut c_void);
