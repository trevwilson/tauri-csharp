//! WebView creation and management FFI functions
//!
//! Functions for creating webviews and controlling their behavior.

use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_void};
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::ptr;

use tao::dpi::{LogicalPosition, LogicalSize};
use url::Url;
use wry::http::{
    header::{HeaderName, HeaderValue, CONTENT_TYPE},
    Response as WryHttpResponse, StatusCode,
};
use wry::{Rect, WebViewBuilder};

use crate::helpers::*;
use crate::types::*;

// ============================================================================
// Webview Creation/Destruction
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_build(
    window: *mut WryWindowHandle,
    config: *const WryWebviewConfig,
) -> *mut WryWebviewHandle {
    if window.is_null() {
        return ptr::null_mut();
    }

    let cfg = unsafe { config.as_ref().copied().unwrap_or_default() };
    let url = opt_cstring(cfg.url);

    let ffi_protocols: Vec<(
        String,
        unsafe extern "C" fn(
            *const WryCustomProtocolRequest,
            *mut WryCustomProtocolResponse,
            *mut c_void,
        ) -> bool,
        *mut c_void,
    )> = if cfg.custom_protocols.count > 0 && !cfg.custom_protocols.protocols.is_null() {
        unsafe {
            std::slice::from_raw_parts(cfg.custom_protocols.protocols, cfg.custom_protocols.count)
        }
        .iter()
        .filter_map(|definition| {
            let handler = definition.handler?;
            let scheme = opt_cstring(definition.scheme)?;
            Some((scheme, handler, definition.user_data))
        })
        .collect()
    } else {
        Vec::new()
    };

    with_window(window, |w| {
        let mut builder = WebViewBuilder::new();

        if let Some(url) = url.as_ref() {
            builder = builder.with_url(url.clone());
        }

        builder = builder.with_devtools(cfg.devtools);

        // Set up IPC handler if provided
        if let Some(ipc_handler) = cfg.ipc_handler {
            let user_data = cfg.ipc_user_data;
            builder = builder.with_ipc_handler(move |request| {
                let url_str = request.uri().to_string();
                let body_str = request.body();

                let url_cstring = match CString::new(url_str) {
                    Ok(s) => s,
                    Err(_) => return,
                };
                let body_cstring = match CString::new(body_str.as_str()) {
                    Ok(s) => s,
                    Err(_) => return,
                };

                unsafe {
                    ipc_handler(url_cstring.as_ptr(), body_cstring.as_ptr(), user_data);
                }
            });
        }

        for (scheme, handler, user_data) in ffi_protocols.iter().cloned() {
            builder = builder.with_asynchronous_custom_protocol(
                scheme.clone(),
                move |webview_id, request, responder| {
                    let (parts, body_vec) = request.into_parts();
                    let uri_string = parts.uri.to_string();
                    let method_string = parts.method.as_str().to_string();
                    let headers_map = parts.headers;

                    let url_cstring = match CString::new(uri_string) {
                        Ok(value) => value,
                        Err(_) => {
                            let _ = responder.respond(
                                WryHttpResponse::builder()
                                    .status(StatusCode::BAD_REQUEST)
                                    .body(Vec::new())
                                    .unwrap(),
                            );
                            return;
                        }
                    };

                    let method_cstring = match CString::new(method_string) {
                        Ok(value) => value,
                        Err(_) => {
                            let _ = responder.respond(
                                WryHttpResponse::builder()
                                    .status(StatusCode::BAD_REQUEST)
                                    .body(Vec::new())
                                    .unwrap(),
                            );
                            return;
                        }
                    };

                    let webview_id_string = format!("{webview_id}");
                    let webview_id_cstring = CString::new(webview_id_string)
                        .unwrap_or_else(|_| CString::new("").expect("empty string"));

                    let mut header_storage: Vec<CString> = Vec::new();
                    let mut header_pairs: Vec<WryCustomProtocolHeader> = Vec::new();
                    for (name, value) in headers_map.iter() {
                        let name_str = name.as_str();
                        let value_str = match value.to_str() {
                            Ok(v) => v,
                            Err(_) => continue,
                        };

                        let Ok(name_cstring) = CString::new(name_str) else {
                            continue;
                        };
                        let Ok(value_cstring) = CString::new(value_str) else {
                            continue;
                        };

                        header_pairs.push(WryCustomProtocolHeader {
                            name: name_cstring.as_ptr(),
                            value: value_cstring.as_ptr(),
                        });
                        header_storage.push(name_cstring);
                        header_storage.push(value_cstring);
                    }

                    let headers_list = WryCustomProtocolHeaderList {
                        headers: if header_pairs.is_empty() {
                            ptr::null()
                        } else {
                            header_pairs.as_ptr()
                        },
                        count: header_pairs.len(),
                    };

                    let body_buffer = WryCustomProtocolBuffer {
                        ptr: body_vec.as_ptr(),
                        len: body_vec.len(),
                    };

                    let ffi_request = WryCustomProtocolRequest {
                        url: url_cstring.as_ptr(),
                        method: method_cstring.as_ptr(),
                        headers: headers_list,
                        body: body_buffer,
                        webview_id: webview_id_cstring.as_ptr(),
                    };

                    let mut ffi_response = WryCustomProtocolResponse::default();
                    let handled = match catch_unwind(AssertUnwindSafe(|| unsafe {
                        handler(&ffi_request, &mut ffi_response, user_data)
                    })) {
                        Ok(result) => result,
                        Err(_) => false,
                    };

                    if !handled {
                        let _ = responder.respond(
                            WryHttpResponse::builder()
                                .status(StatusCode::NOT_FOUND)
                                .body(Vec::new())
                                .unwrap(),
                        );
                        return;
                    }

                    let status = if ffi_response.status == 0 {
                        StatusCode::OK
                    } else {
                        StatusCode::from_u16(ffi_response.status).unwrap_or(StatusCode::OK)
                    };

                    let mut builder = WryHttpResponse::builder().status(status);

                    if !ffi_response.mime_type.is_null() {
                        if let Ok(mime) = unsafe { CStr::from_ptr(ffi_response.mime_type) }.to_str()
                        {
                            if let Ok(value) = HeaderValue::from_str(mime) {
                                builder = builder.header(CONTENT_TYPE, value);
                            }
                        }
                    }

                    if ffi_response.headers.count > 0 && !ffi_response.headers.headers.is_null() {
                        let header_slice = unsafe {
                            std::slice::from_raw_parts(
                                ffi_response.headers.headers,
                                ffi_response.headers.count,
                            )
                        };
                        for header in header_slice {
                            if header.name.is_null() || header.value.is_null() {
                                continue;
                            }
                            let Ok(name_str) = unsafe { CStr::from_ptr(header.name) }.to_str()
                            else {
                                continue;
                            };
                            let Ok(value_str) = unsafe { CStr::from_ptr(header.value) }.to_str()
                            else {
                                continue;
                            };
                            let Ok(name) = HeaderName::from_bytes(name_str.as_bytes()) else {
                                continue;
                            };
                            let Ok(value) = HeaderValue::from_str(value_str) else {
                                continue;
                            };
                            builder = builder.header(name, value);
                        }
                    }

                    let body = if ffi_response.body.len > 0 && !ffi_response.body.ptr.is_null() {
                        unsafe {
                            std::slice::from_raw_parts(ffi_response.body.ptr, ffi_response.body.len)
                        }
                        .to_vec()
                    } else {
                        Vec::new()
                    };

                    let response = builder.body(body).unwrap_or_else(|_| {
                        WryHttpResponse::builder()
                            .status(StatusCode::INTERNAL_SERVER_ERROR)
                            .body(Vec::new())
                            .unwrap()
                    });

                    let _ = responder.respond(response);

                    if let Some(free) = ffi_response.free {
                        unsafe { free(ffi_response.user_data) };
                    }
                },
            );
        }

        // Build as child webview if requested, otherwise as full-window webview
        if cfg.is_child {
            let bounds = Rect {
                position: LogicalPosition::new(cfg.x, cfg.y).into(),
                size: LogicalSize::new(cfg.width, cfg.height).into(),
            };
            builder = builder.with_bounds(bounds);
            builder
                .build_as_child(w)
                .ok()
                .map(|webview| Box::into_raw(Box::new(WryWebviewHandle { webview })))
        } else {
            builder
                .build(w)
                .ok()
                .map(|webview| Box::into_raw(Box::new(WryWebviewHandle { webview })))
        }
    })
    .flatten()
    .unwrap_or(ptr::null_mut())
}

#[no_mangle]
pub extern "C" fn wry_webview_free(webview: *mut WryWebviewHandle) {
    if !webview.is_null() {
        unsafe { drop(Box::from_raw(webview)) };
    }
}

#[no_mangle]
pub extern "C" fn wry_webview_identifier(webview: *mut WryWebviewHandle) -> *const c_char {
    with_webview(webview, |view| {
        let id_string = format!("{}", view.id());
        let cstring = CString::new(id_string).unwrap_or_else(|_| CString::new("").unwrap());
        cstring.into_raw() as *const c_char
    })
    .unwrap_or(ptr::null())
}

// ============================================================================
// Navigation
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_navigate(
    webview: *mut WryWebviewHandle,
    url: *const c_char,
) -> bool {
    let Some(url_str) = opt_cstring(url) else {
        return false;
    };
    let Ok(parsed_url) = Url::parse(&url_str) else {
        return false;
    };
    with_webview(webview, |view| view.load_url(parsed_url.as_str()).is_ok()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_webview_reload(webview: *mut WryWebviewHandle) -> bool {
    with_webview(webview, |view| view.reload().is_ok()).unwrap_or(false)
}

// ============================================================================
// Script Execution
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_evaluate_script(
    webview: *mut WryWebviewHandle,
    script: *const c_char,
) -> bool {
    let Some(script) = opt_cstring(script) else {
        return false;
    };
    with_webview(webview, |view| view.evaluate_script(&script).is_ok()).unwrap_or(false)
}

// ============================================================================
// Zoom
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_set_zoom(
    webview: *mut WryWebviewHandle,
    scale_factor: f64,
) -> bool {
    with_webview(webview, |view| view.zoom(scale_factor).is_ok()).unwrap_or(false)
}

// ============================================================================
// Visibility
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_show(webview: *mut WryWebviewHandle) -> bool {
    with_webview(webview, |view| view.set_visible(true).is_ok()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_webview_hide(webview: *mut WryWebviewHandle) -> bool {
    with_webview(webview, |view| view.set_visible(false).is_ok()).unwrap_or(false)
}

// ============================================================================
// Bounds (for child webviews)
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_set_bounds(
    webview: *mut WryWebviewHandle,
    x: f64,
    y: f64,
    width: f64,
    height: f64,
) -> bool {
    with_webview(webview, |view| {
        let bounds = Rect {
            position: LogicalPosition::new(x, y).into(),
            size: LogicalSize::new(width, height).into(),
        };
        view.set_bounds(bounds).is_ok()
    })
    .unwrap_or(false)
}

// ============================================================================
// Browsing Data
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_webview_clear_browsing_data(webview: *mut WryWebviewHandle) -> bool {
    with_webview(webview, |view| view.clear_all_browsing_data().is_ok()).unwrap_or(false)
}
