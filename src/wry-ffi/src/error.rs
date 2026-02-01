//! Error handling for wry-ffi
//!
//! Uses thread-local storage to store the last error message.

use std::cell::RefCell;
use std::ffi::{c_char, CString};

use crate::types::{WryErrorCode, WryResult};

thread_local! {
    static LAST_ERROR: RefCell<Option<CString>> = const { RefCell::new(None) };
}

/// Set the last error message for the current thread
pub fn set_last_error(message: impl Into<String>) {
    let msg = message.into();
    log::error!("wry-ffi error: {}", msg);
    LAST_ERROR.with(|e| {
        *e.borrow_mut() = CString::new(msg).ok();
    });
}

/// Get the last error message pointer (valid until next error)
pub fn get_last_error_ptr() -> *const c_char {
    LAST_ERROR.with(|e| {
        e.borrow()
            .as_ref()
            .map(|s| s.as_ptr())
            .unwrap_or(std::ptr::null())
    })
}

/// Clear the last error
pub fn clear_last_error() {
    LAST_ERROR.with(|e| {
        *e.borrow_mut() = None;
    });
}

/// Trait for converting errors to WryResult
pub trait IntoWryResult<T> {
    fn into_wry_result(self) -> Result<T, WryResult>;
}

impl<T, E: std::fmt::Display> IntoWryResult<T> for Result<T, E> {
    fn into_wry_result(self) -> Result<T, WryResult> {
        self.map_err(|e| {
            set_last_error(e.to_string());
            WryResult::err(WryErrorCode::Unknown as i32)
        })
    }
}

/// Helper to create an error result with a specific code
pub fn error_result(code: WryErrorCode, message: impl Into<String>) -> WryResult {
    set_last_error(message);
    WryResult::err(code as i32)
}

/// Helper macro for returning early with an error
#[macro_export]
macro_rules! try_or_return {
    ($expr:expr, $code:expr, $msg:expr) => {
        match $expr {
            Ok(val) => val,
            Err(e) => {
                $crate::error::set_last_error(format!("{}: {}", $msg, e));
                return $crate::types::WryResult::err($code as i32);
            }
        }
    };
    ($expr:expr, $default:expr) => {
        match $expr {
            Some(val) => val,
            None => return $default,
        }
    };
}

/// Helper macro for null pointer checks
#[macro_export]
macro_rules! null_check {
    ($ptr:expr, $name:expr) => {
        if $ptr.is_null() {
            $crate::error::set_last_error(concat!("Null pointer: ", $name));
            return $crate::types::WryResult::err(
                $crate::types::WryErrorCode::InvalidHandle as i32,
            );
        }
    };
    ($ptr:expr, $name:expr, $ret:expr) => {
        if $ptr.is_null() {
            $crate::error::set_last_error(concat!("Null pointer: ", $name));
            return $ret;
        }
    };
}
