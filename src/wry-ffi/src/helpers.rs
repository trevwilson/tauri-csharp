//! Helper functions for FFI operations
//!
//! Utility functions for C string handling, panic guards, and conversions.

use std::cell::RefCell;
use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::sync::OnceLock;
use std::thread::LocalKey;

use serde_json::json;
use tao::monitor::MonitorHandle;
use tao::window::Theme;
#[cfg(target_os = "macos")]
use tao::platform::macos::ActivationPolicy;
use tao::window::{
    ResizeDirection as TaoResizeDirection, UserAttentionType as TaoUserAttentionType,
};
use rfd::{FileDialog, MessageLevel};

use crate::types::*;

// ============================================================================
// Global State
// ============================================================================

pub static LIBRARY_NAME: OnceLock<CString> = OnceLock::new();
pub static RUNTIME_VERSION: OnceLock<CString> = OnceLock::new();
pub static WEBVIEW_VERSION: OnceLock<CString> = OnceLock::new();

thread_local! {
    pub static TITLE_BUFFER: RefCell<CString> = RefCell::new(CString::new("").expect("empty string"));
    pub static MONITOR_BUFFER: RefCell<CString> = RefCell::new(CString::new("").expect("empty string"));
    pub static MONITOR_LIST_BUFFER: RefCell<CString> = RefCell::new(CString::new("").expect("empty string"));
}

// ============================================================================
// String Helpers
// ============================================================================

/// Get cached CString, initializing if needed
pub fn cached_cstring(storage: &OnceLock<CString>, builder: impl FnOnce() -> String) -> *const c_char {
    storage
        .get_or_init(|| CString::new(builder()).expect("ffi string contains null byte"))
        .as_ptr()
}

/// Convert C string pointer to optional owned String
pub fn opt_cstring(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        None
    } else {
        unsafe { CStr::from_ptr(ptr).to_str().ok().map(|s| s.to_owned()) }
    }
}

/// Convert optional WryColor pointer to RGBA tuple
pub fn opt_color(color: *const WryColor) -> Option<(u8, u8, u8, u8)> {
    if color.is_null() {
        None
    } else {
        let color = unsafe { &*color };
        Some((color.red, color.green, color.blue, color.alpha))
    }
}

/// Write JSON value to thread-local buffer and return pointer
pub fn write_json_to_buffer(
    buffer: &'static LocalKey<RefCell<CString>>,
    value: serde_json::Value,
) -> *const c_char {
    let json_string = value.to_string();
    buffer.with(|cell| {
        let mut storage = cell.borrow_mut();
        *storage =
            CString::new(json_string).unwrap_or_else(|_| CString::new("{}").expect("static JSON"));
        storage.as_ptr()
    })
}

/// Write string to thread-local buffer and return pointer
pub fn write_string_to_buffer(
    buffer: &'static LocalKey<RefCell<CString>>,
    value: String,
) -> *const c_char {
    buffer.with(|cell| {
        let mut storage = cell.borrow_mut();
        *storage = CString::new(value).unwrap_or_else(|_| CString::new("").expect("empty string"));
        storage.as_ptr()
    })
}

// ============================================================================
// Panic Guards
// ============================================================================

#[cfg(target_os = "macos")]
pub fn guard_panic<T>(f: impl FnOnce() -> *mut T) -> *mut T {
    match catch_unwind(AssertUnwindSafe(f)) {
        Ok(ptr) => ptr,
        Err(_) => ptr::null_mut(),
    }
}

pub fn guard_panic_bool(f: impl FnOnce() -> bool) -> bool {
    match catch_unwind(AssertUnwindSafe(f)) {
        Ok(result) => result,
        Err(_) => false,
    }
}

pub fn guard_panic_value<T: Default>(f: impl FnOnce() -> T) -> T {
    match catch_unwind(AssertUnwindSafe(f)) {
        Ok(value) => value,
        Err(_) => T::default(),
    }
}

// ============================================================================
// Type Conversions
// ============================================================================

/// Convert WryWindowTheme to tao Theme
pub fn theme_from_ffi(theme: WryWindowTheme) -> Option<Theme> {
    match theme {
        WryWindowTheme::Unspecified => None,
        WryWindowTheme::Light => Some(Theme::Light),
        WryWindowTheme::Dark => Some(Theme::Dark),
    }
}

#[cfg(target_os = "macos")]
pub fn activation_policy_from_ffi(policy: WryActivationPolicy) -> ActivationPolicy {
    match policy {
        WryActivationPolicy::Regular => ActivationPolicy::Regular,
        WryActivationPolicy::Accessory => ActivationPolicy::Accessory,
        WryActivationPolicy::Prohibited => ActivationPolicy::Prohibited,
    }
}

pub fn tao_user_attention_from_ffi(kind: WryUserAttentionType) -> TaoUserAttentionType {
    match kind {
        WryUserAttentionType::Informational => TaoUserAttentionType::Informational,
        WryUserAttentionType::Critical => TaoUserAttentionType::Critical,
    }
}

pub fn tao_resize_direction_from_ffi(direction: WryResizeDirection) -> TaoResizeDirection {
    match direction {
        WryResizeDirection::East => TaoResizeDirection::East,
        WryResizeDirection::North => TaoResizeDirection::North,
        WryResizeDirection::NorthEast => TaoResizeDirection::NorthEast,
        WryResizeDirection::NorthWest => TaoResizeDirection::NorthWest,
        WryResizeDirection::South => TaoResizeDirection::South,
        WryResizeDirection::SouthEast => TaoResizeDirection::SouthEast,
        WryResizeDirection::SouthWest => TaoResizeDirection::SouthWest,
        WryResizeDirection::West => TaoResizeDirection::West,
    }
}

pub fn message_level_from_ffi(level: WryMessageDialogLevel) -> MessageLevel {
    match level {
        WryMessageDialogLevel::Info => MessageLevel::Info,
        WryMessageDialogLevel::Warning => MessageLevel::Warning,
        WryMessageDialogLevel::Error => MessageLevel::Error,
    }
}

// ============================================================================
// Monitor Helpers
// ============================================================================

pub fn monitor_to_json(monitor: &MonitorHandle) -> serde_json::Value {
    let name = monitor.name().unwrap_or_default();
    let position = monitor.position();
    let size = monitor.size();
    json!({
        "name": name,
        "scale_factor": monitor.scale_factor(),
        "position": {
            "x": position.x,
            "y": position.y,
        },
        "size": {
            "width": size.width,
            "height": size.height,
        }
    })
}

// ============================================================================
// Dialog Helpers
// ============================================================================

pub fn dialog_apply_filters(mut dialog: FileDialog, filters: &[WryDialogFilter]) -> FileDialog {
    const EMPTY_EXTS: [&str; 0] = [];
    for filter in filters {
        let Some(label) = opt_cstring(filter.label) else {
            continue;
        };

        if filter.extension_count == 0 || filter.extensions.is_null() {
            dialog = dialog.add_filter(&label, &EMPTY_EXTS);
            continue;
        }

        let raw_exts =
            unsafe { std::slice::from_raw_parts(filter.extensions, filter.extension_count) };
        let mut owned_exts = Vec::with_capacity(raw_exts.len());
        for &ext_ptr in raw_exts {
            if ext_ptr.is_null() {
                continue;
            }
            if let Some(ext) = opt_cstring(ext_ptr) {
                owned_exts.push(ext);
            }
        }
        let ext_refs: Vec<&str> = owned_exts.iter().map(|s| s.as_str()).collect();
        dialog = dialog.add_filter(&label, &ext_refs);
    }
    dialog
}

pub fn dialog_selection_from_paths(paths: Vec<std::path::PathBuf>) -> WryDialogSelection {
    if paths.is_empty() {
        return WryDialogSelection::default();
    }

    let mut raw_paths: Vec<*mut c_char> = Vec::with_capacity(paths.len());
    for path in paths {
        let display = path.to_string_lossy().into_owned();
        match CString::new(display) {
            Ok(cstr) => raw_paths.push(cstr.into_raw()),
            Err(_) => continue,
        }
    }

    if raw_paths.is_empty() {
        return WryDialogSelection::default();
    }

    let count = raw_paths.len();
    let boxed = raw_paths.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut *mut c_char;
    WryDialogSelection { paths: ptr, count }
}

pub fn prompt_result_from_string(value: Option<String>) -> WryPromptDialogResult {
    if let Some(value) = value {
        if let Ok(cstr) = CString::new(value) {
            let ptr = cstr.into_raw();
            WryPromptDialogResult {
                value: ptr,
                accepted: true,
            }
        } else {
            WryPromptDialogResult::default()
        }
    } else {
        WryPromptDialogResult::default()
    }
}

// ============================================================================
// Window/Webview Accessors
// ============================================================================

pub fn with_window<R>(window: *mut WryWindowHandle, f: impl FnOnce(&tao::window::Window) -> R) -> Option<R> {
    unsafe { window.as_ref() }.map(|handle| f(&handle.window))
}

pub fn with_webview<R>(webview: *mut WryWebviewHandle, f: impl FnOnce(&wry::WebView) -> R) -> Option<R> {
    unsafe { webview.as_ref() }.map(|handle| f(&handle.webview))
}

// ============================================================================
// Position/Size Writers
// ============================================================================

use tao::dpi::{LogicalPosition, LogicalSize};

pub fn write_position(target: *mut WryPoint, position: LogicalPosition<f64>) {
    unsafe {
        (*target).x = position.x;
        (*target).y = position.y;
    }
}

pub fn write_size(target: *mut WrySize, size: LogicalSize<f64>) {
    unsafe {
        (*target).width = size.width;
        (*target).height = size.height;
    }
}

// ============================================================================
// Accelerator Helper (macOS only)
// ============================================================================

#[cfg(target_os = "macos")]
pub fn accelerator_from_ptr(ptr: *const c_char) -> Option<muda::accelerator::Accelerator> {
    opt_cstring(ptr)?.parse().ok()
}
