//! System tray FFI functions (macOS only)
//!
//! Provides system tray icon support on macOS.

use std::os::raw::c_char;
use std::ptr;

#[cfg(target_os = "macos")]
use crate::helpers::*;
use crate::types::*;

// ============================================================================
// Tray Icon
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_new(config: *const WryTrayConfig) -> *mut WryTrayHandle {
    use std::ffi::CString;
    use tray_icon::TrayIconBuilder;

    guard_panic(|| {
        let cfg = unsafe { config.as_ref() }.copied().unwrap_or_default();
        let identifier = opt_cstring(cfg.identifier);
        let title = opt_cstring(cfg.title);
        let tooltip = opt_cstring(cfg.tooltip);

        let mut builder = TrayIconBuilder::new();
        if let Some(ref id) = identifier {
            builder = builder.with_id(id.clone());
        }
        if let Some(ref title) = title {
            builder = builder.with_title(title.clone());
        }
        if let Some(ref tooltip) = tooltip {
            builder = builder.with_tooltip(tooltip.clone());
        }
        builder = builder.with_menu_on_left_click(cfg.show_menu_on_left_click);

        let tray = match builder.build() {
            Ok(tray) => tray,
            Err(_) => return ptr::null_mut(),
        };

        if !cfg.visible {
            let _ = tray.set_visible(false);
        }

        tray.set_show_menu_on_left_click(cfg.show_menu_on_left_click);

        let identifier = CString::new(tray.id().as_ref())
            .unwrap_or_else(|_| CString::new("wry-tray").expect("static string has no nulls"));

        Box::into_raw(Box::new(WryTrayHandle {
            tray,
            menu: None,
            identifier,
        }))
    })
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_new(_config: *const WryTrayConfig) -> *mut WryTrayHandle {
    ptr::null_mut()
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_free(tray: *mut WryTrayHandle) {
    if !tray.is_null() {
        unsafe { drop(Box::from_raw(tray)) };
    }
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_free(_tray: *mut WryTrayHandle) {}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_identifier(tray: *mut WryTrayHandle) -> *const c_char {
    let Some(tray) = (unsafe { tray.as_ref() }) else {
        return ptr::null();
    };
    tray.identifier.as_ptr()
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_identifier(_tray: *mut WryTrayHandle) -> *const c_char {
    ptr::null()
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_set_title(tray: *mut WryTrayHandle, title: *const c_char) -> bool {
    let Some(tray) = (unsafe { tray.as_mut() }) else {
        return false;
    };
    let result_title = opt_cstring(title);
    tray.tray.set_title(result_title.as_deref());
    true
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_set_title(_tray: *mut WryTrayHandle, _title: *const c_char) -> bool {
    false
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_set_tooltip(
    tray: *mut WryTrayHandle,
    tooltip: *const c_char,
) -> bool {
    let Some(tray) = (unsafe { tray.as_mut() }) else {
        return false;
    };
    let tooltip = opt_cstring(tooltip);
    tray.tray.set_tooltip(tooltip.as_deref()).is_ok()
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_set_tooltip(
    _tray: *mut WryTrayHandle,
    _tooltip: *const c_char,
) -> bool {
    false
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_set_visible(tray: *mut WryTrayHandle, visible: bool) -> bool {
    let Some(tray) = (unsafe { tray.as_mut() }) else {
        return false;
    };
    tray.tray.set_visible(visible).is_ok()
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_set_visible(_tray: *mut WryTrayHandle, _visible: bool) -> bool {
    false
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_set_show_menu_on_left_click(
    tray: *mut WryTrayHandle,
    enable: bool,
) -> bool {
    let Some(tray) = (unsafe { tray.as_mut() }) else {
        return false;
    };
    tray.tray.set_show_menu_on_left_click(enable);
    true
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_set_show_menu_on_left_click(
    _tray: *mut WryTrayHandle,
    _enable: bool,
) -> bool {
    false
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_tray_set_menu(
    tray: *mut WryTrayHandle,
    menu: *mut WryMenuBarHandle,
) -> bool {
    let Some(tray) = (unsafe { tray.as_mut() }) else {
        return false;
    };

    if menu.is_null() {
        tray.tray
            .set_menu(None::<Box<dyn tray_icon::menu::ContextMenu>>);
        tray.menu = None;
        return true;
    }

    let Some(menu_handle) = (unsafe { menu.as_ref() }) else {
        return false;
    };

    let cloned_menu = menu_handle.menu.clone();
    tray.tray.set_menu(Some(
        Box::new(cloned_menu.clone()) as Box<dyn tray_icon::menu::ContextMenu>
    ));
    tray.menu = Some(cloned_menu);
    true
}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_tray_set_menu(
    _tray: *mut WryTrayHandle,
    _menu: *mut WryMenuBarHandle,
) -> bool {
    false
}
