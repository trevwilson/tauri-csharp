//! Custom protocol handling
//!
//! Allows registering custom URL schemes like `app://` for serving local resources.

use std::ffi::c_char;
use std::os::raw::c_void;

use crate::app::{AppState, ProtocolHandler};
use crate::error::error_result;
use crate::string::c_str_to_string;
use crate::types::{CustomProtocolCallback, WryApp, WryErrorCode, WryResult};

// ============================================================================
// FFI Functions
// ============================================================================

/// Register custom protocol handler (e.g., "app" for app://...)
///
/// Must be called before window creation. The handler will be called for all
/// requests matching the scheme.
///
/// # Safety
/// Must be called with a valid app handle and callback.
#[no_mangle]
pub unsafe extern "C" fn wry_register_protocol(
    app: WryApp,
    scheme: *const c_char,
    callback: CustomProtocolCallback,
    user_data: *mut c_void,
) -> WryResult {
    if app.is_null() {
        return error_result(WryErrorCode::InvalidHandle, "Null app handle");
    }

    let scheme = match c_str_to_string(scheme) {
        Some(s) => s,
        None => return error_result(WryErrorCode::InvalidParameter, "Null or invalid scheme"),
    };

    log::info!("Registering custom protocol: {}", scheme);

    let state = &mut *(app as *mut AppState);

    // Store the protocol handler
    state.custom_protocols.insert(
        scheme.clone(),
        ProtocolHandler {
            callback,
            user_data,
        },
    );

    log::debug!("Protocol '{}' registered successfully", scheme);
    WryResult::ok()
}
