//! Application and event loop management
//!
//! FFI functions for creating and running the event loop.

use std::ffi::CString;
use std::os::raw::{c_char, c_void};
use std::ptr;

use tao::event::Event;
use tao::event_loop::{ControlFlow, EventLoopBuilder};
use tao::platform::run_return::EventLoopExtRunReturn;

#[cfg(target_os = "macos")]
use muda::MenuEvent;
#[cfg(target_os = "macos")]
use tao::platform::macos::EventLoopWindowTargetExtMacOS;
#[cfg(target_os = "macos")]
use tray_icon::TrayIconEvent;

use crate::events::serialize_event;
use crate::helpers::*;
use crate::types::*;

// ============================================================================
// ABI Version
// ============================================================================

const WRY_FFI_ABI_VERSION: u32 = 2;

#[no_mangle]
pub extern "C" fn wry_ffi_abi_version() -> u32 {
    WRY_FFI_ABI_VERSION
}

#[no_mangle]
pub extern "C" fn wry_library_name() -> *const c_char {
    cached_cstring(&LIBRARY_NAME, || "WryFFI".to_string())
}

#[no_mangle]
pub extern "C" fn wry_crate_version() -> *const c_char {
    cached_cstring(&RUNTIME_VERSION, || env!("CARGO_PKG_VERSION").to_string())
}

#[no_mangle]
pub extern "C" fn wry_webview_version() -> *const c_char {
    cached_cstring(&WEBVIEW_VERSION, || {
        wry::webview_version().unwrap_or_default()
    })
}

// ============================================================================
// Event Loop Creation
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_event_loop_new() -> *mut WryEventLoop {
    let event_loop = EventLoopBuilder::<WryUserEvent>::with_user_event().build();

    #[cfg(target_os = "macos")]
    {
        let proxy = event_loop.create_proxy();
        MenuEvent::set_event_handler(Some(move |event: MenuEvent| {
            let _ = proxy.send_event(WryUserEvent::Menu(event.id().as_ref().to_string()));
        }));

        let tray_proxy = event_loop.create_proxy();
        TrayIconEvent::set_event_handler(Some(move |event: TrayIconEvent| {
            let _ = tray_proxy.send_event(WryUserEvent::Tray(event.into()));
        }));
    }

    Box::into_raw(Box::new(WryEventLoop { event_loop }))
}

#[no_mangle]
pub extern "C" fn wry_event_loop_free(event_loop: *mut WryEventLoop) {
    if !event_loop.is_null() {
        unsafe { drop(Box::from_raw(event_loop)) };
        #[cfg(target_os = "macos")]
        MenuEvent::set_event_handler::<fn(MenuEvent)>(None);
        #[cfg(target_os = "macos")]
        TrayIconEvent::set_event_handler::<fn(TrayIconEvent)>(None);
    }
}

// ============================================================================
// Event Loop Proxy
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_event_loop_create_proxy(
    event_loop: *mut WryEventLoop,
) -> *mut WryEventLoopProxyHandle {
    if event_loop.is_null() {
        return ptr::null_mut();
    }

    let event_loop = unsafe { &mut *event_loop };
    let proxy = event_loop.event_loop.create_proxy();
    Box::into_raw(Box::new(WryEventLoopProxyHandle { proxy }))
}

#[no_mangle]
pub extern "C" fn wry_event_loop_proxy_request_exit(
    proxy: *mut WryEventLoopProxyHandle,
) -> bool {
    if proxy.is_null() {
        return false;
    }

    let proxy = unsafe { &mut *proxy };
    proxy.proxy.send_event(WryUserEvent::Exit).is_ok()
}

#[no_mangle]
pub extern "C" fn wry_event_loop_proxy_send_user_event(
    proxy: *mut WryEventLoopProxyHandle,
    payload: *const c_char,
) -> bool {
    if proxy.is_null() {
        return false;
    }

    let proxy = unsafe { &mut *proxy };
    let message = opt_cstring(payload).unwrap_or_default();
    proxy
        .proxy
        .send_event(WryUserEvent::Custom(message))
        .is_ok()
}

#[no_mangle]
pub extern "C" fn wry_event_loop_proxy_free(proxy: *mut WryEventLoopProxyHandle) {
    if !proxy.is_null() {
        unsafe { drop(Box::from_raw(proxy)) };
    }
}

// ============================================================================
// macOS-specific Event Loop Functions
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_event_loop_set_activation_policy(
    event_loop: *mut WryEventLoop,
    policy: WryActivationPolicy,
) -> bool {
    #[cfg(target_os = "macos")]
    {
        if event_loop.is_null() {
            return false;
        }

        let event_loop = unsafe { &mut *event_loop };
        event_loop
            .event_loop
            .set_activation_policy_at_runtime(activation_policy_from_ffi(policy));
        true
    }

    #[cfg(not(target_os = "macos"))]
    {
        let _ = (event_loop, policy);
        false
    }
}

#[no_mangle]
pub extern "C" fn wry_event_loop_set_dock_visibility(
    event_loop: *mut WryEventLoop,
    visible: bool,
) -> bool {
    #[cfg(target_os = "macos")]
    {
        if event_loop.is_null() {
            return false;
        }

        let event_loop = unsafe { &mut *event_loop };
        event_loop.event_loop.set_dock_visibility(visible);
        true
    }

    #[cfg(not(target_os = "macos"))]
    {
        let _ = (event_loop, visible);
        false
    }
}

#[no_mangle]
pub extern "C" fn wry_event_loop_hide_application(event_loop: *mut WryEventLoop) -> bool {
    #[cfg(target_os = "macos")]
    {
        if event_loop.is_null() {
            return false;
        }

        let event_loop = unsafe { &mut *event_loop };
        event_loop.event_loop.hide_application();
        true
    }

    #[cfg(not(target_os = "macos"))]
    {
        let _ = event_loop;
        false
    }
}

#[no_mangle]
pub extern "C" fn wry_event_loop_show_application(event_loop: *mut WryEventLoop) -> bool {
    #[cfg(target_os = "macos")]
    {
        if event_loop.is_null() {
            return false;
        }

        let event_loop = unsafe { &mut *event_loop };
        event_loop.event_loop.show_application();
        true
    }

    #[cfg(not(target_os = "macos"))]
    {
        let _ = event_loop;
        false
    }
}

// ============================================================================
// Event Loop Pump (Main Loop)
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_event_loop_pump(
    event_loop: *mut WryEventLoop,
    callback: WryEventLoopCallback,
    user_data: *mut c_void,
) {
    if event_loop.is_null() {
        return;
    }

    let event_loop = unsafe { &mut *event_loop };
    event_loop
        .event_loop
        .run_return(|event, _target, control_flow| {
            if let Some(cb) = callback {
                let description = serialize_event(&event);
                if let Ok(c_description) = CString::new(description) {
                    let desired_flow = cb(c_description.as_ptr(), user_data);
                    match desired_flow {
                        WryEventLoopControlFlow::Poll => *control_flow = ControlFlow::Poll,
                        WryEventLoopControlFlow::Wait => *control_flow = ControlFlow::Wait,
                        WryEventLoopControlFlow::Exit => *control_flow = ControlFlow::Exit,
                    }
                } else {
                    *control_flow = ControlFlow::Exit;
                }

                // Poll for global hotkey events and deliver them through the callback
                while let Some(shortcut_id) = crate::shortcuts::poll_hotkey_event() {
                    let hotkey_json = format!(
                        r#"{{"type":"global-shortcut","id":{}}}"#,
                        shortcut_id
                    );
                    if let Ok(c_hotkey) = CString::new(hotkey_json) {
                        let flow = cb(c_hotkey.as_ptr(), user_data);
                        if matches!(flow, WryEventLoopControlFlow::Exit) {
                            *control_flow = ControlFlow::Exit;
                            return;
                        }
                    }
                }
            } else {
                *control_flow = ControlFlow::Exit;
            }

            if matches!(event, Event::UserEvent(WryUserEvent::Exit)) {
                *control_flow = ControlFlow::Exit;
            }

            if matches!(event, Event::LoopDestroyed) {
                *control_flow = ControlFlow::Exit;
            }
        });
}

// ============================================================================
// Testing Helpers (macOS local-dev only)
// ============================================================================

#[cfg(all(target_os = "macos", feature = "local-dev"))]
#[no_mangle]
pub extern "C" fn wry_app_state_force_launched() {
    tao::platform::macos::force_app_state_launched_for_testing();
}

#[cfg(all(target_os = "macos", not(feature = "local-dev")))]
#[no_mangle]
pub extern "C" fn wry_app_state_force_launched() {
    // No-op when using crates.io tao (velox-testing feature not available)
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_app_state_force_launched() {
    // No-op on non-macOS platforms
}
