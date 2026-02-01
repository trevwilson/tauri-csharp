//! String utilities for FFI
//!
//! Handles conversion between Rust strings and C strings.

use std::ffi::{c_char, CStr, CString};

/// Convert a C string pointer to a Rust String
///
/// # Safety
/// The pointer must be valid and null-terminated, or null.
pub unsafe fn c_str_to_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    CStr::from_ptr(ptr).to_str().ok().map(|s| s.to_string())
}

/// Convert a C string pointer to a Rust &str
///
/// # Safety
/// The pointer must be valid and null-terminated, or null.
pub unsafe fn c_str_to_str<'a>(ptr: *const c_char) -> Option<&'a str> {
    if ptr.is_null() {
        return None;
    }
    CStr::from_ptr(ptr).to_str().ok()
}

/// Allocate a new C string from a Rust string
///
/// The caller is responsible for freeing this with `wry_string_free`.
pub fn string_to_c_string(s: &str) -> *mut c_char {
    match CString::new(s) {
        Ok(cs) => cs.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Free a C string allocated by this library
///
/// # Safety
/// The pointer must have been allocated by `string_to_c_string` or similar.
#[no_mangle]
pub unsafe extern "C" fn wry_string_free(s: *mut c_char) {
    if !s.is_null() {
        drop(CString::from_raw(s));
    }
}
