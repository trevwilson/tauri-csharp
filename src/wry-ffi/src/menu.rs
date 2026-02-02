//! Menu bar and menu item FFI functions (macOS only)
//!
//! Provides native menu bar support on macOS.

use std::os::raw::c_char;
use std::ptr;

#[cfg(target_os = "macos")]
use crate::helpers::*;
use crate::types::*;

// ============================================================================
// Menu Bar
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_new() -> *mut WryMenuBarHandle {
    use muda::Menu;
    use std::ffi::CString;

    guard_panic(|| {
        let menu = Menu::new();
        let identifier = CString::new(menu.id().as_ref()).expect("menu id contains null byte");
        Box::into_raw(Box::new(WryMenuBarHandle {
            menu,
            submenus: Vec::new(),
            identifier,
        }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_new_with_id(id: *const c_char) -> *mut WryMenuBarHandle {
    use muda::{Menu, MenuId};
    use std::ffi::CString;

    guard_panic(|| {
        let identifier_string = opt_cstring(id).unwrap_or_default();
        let menu = Menu::with_id(MenuId::new(identifier_string.clone()));
        let identifier = CString::new(identifier_string).expect("menu id contains null byte");
        Box::into_raw(Box::new(WryMenuBarHandle {
            menu,
            submenus: Vec::new(),
            identifier,
        }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_free(menu: *mut WryMenuBarHandle) {
    if !menu.is_null() {
        unsafe { drop(Box::from_raw(menu)) };
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_identifier(menu: *mut WryMenuBarHandle) -> *const c_char {
    let Some(menu) = (unsafe { menu.as_ref() }) else {
        return ptr::null();
    };
    menu.identifier.as_ptr()
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_append_submenu(
    menu: *mut WryMenuBarHandle,
    submenu: *mut WrySubmenuHandle,
) -> bool {
    let Some(menu) = (unsafe { menu.as_mut() }) else {
        return false;
    };
    let Some(submenu) = (unsafe { submenu.as_ref() }) else {
        return false;
    };

    let result = {
        let submenu_ref = submenu.submenu.borrow();
        menu.menu.append(&*submenu_ref)
    };

    if result.is_ok() {
        menu.submenus.push(submenu.submenu.clone());
        true
    } else {
        false
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_bar_set_app_menu(menu: *mut WryMenuBarHandle) -> bool {
    let Some(menu) = (unsafe { menu.as_ref() }) else {
        return false;
    };
    menu.menu.init_for_nsapp();
    true
}

// ============================================================================
// Submenu
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_new(
    title: *const c_char,
    enabled: bool,
) -> *mut WrySubmenuHandle {
    use muda::Submenu;
    use std::cell::RefCell;
    use std::ffi::CString;
    use std::rc::Rc;

    guard_panic(|| {
        let title = opt_cstring(title).unwrap_or_default();
        let submenu = Submenu::new(title, enabled);
        let identifier =
            CString::new(submenu.id().as_ref()).expect("submenu id contains null byte");
        Box::into_raw(Box::new(WrySubmenuHandle {
            submenu: Rc::new(RefCell::new(submenu)),
            identifier,
            items: Vec::new(),
        }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_new_with_id(
    id: *const c_char,
    title: *const c_char,
    enabled: bool,
) -> *mut WrySubmenuHandle {
    use muda::{MenuId, Submenu};
    use std::cell::RefCell;
    use std::ffi::CString;
    use std::rc::Rc;

    guard_panic(|| {
        let title = opt_cstring(title).unwrap_or_default();
        let id_string = opt_cstring(id).unwrap_or_default();
        let submenu = Submenu::with_id(MenuId::new(id_string.clone()), title, enabled);
        let identifier = CString::new(id_string).expect("submenu id contains null byte");
        Box::into_raw(Box::new(WrySubmenuHandle {
            submenu: Rc::new(RefCell::new(submenu)),
            identifier,
            items: Vec::new(),
        }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_free(submenu: *mut WrySubmenuHandle) {
    if !submenu.is_null() {
        unsafe { drop(Box::from_raw(submenu)) };
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_identifier(submenu: *mut WrySubmenuHandle) -> *const c_char {
    let Some(submenu) = (unsafe { submenu.as_ref() }) else {
        return ptr::null();
    };
    submenu.identifier.as_ptr()
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_append_item(
    submenu: *mut WrySubmenuHandle,
    item: *mut WryMenuItemHandle,
) -> bool {
    let Some(submenu) = (unsafe { submenu.as_mut() }) else {
        return false;
    };
    let Some(item) = (unsafe { item.as_ref() }) else {
        return false;
    };

    if submenu.submenu.borrow().append(&item.item).is_ok() {
        submenu.items.push(item.item.clone());
        true
    } else {
        false
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_append_separator(
    submenu: *mut WrySubmenuHandle,
    separator: *mut WrySeparatorHandle,
) -> bool {
    let Some(submenu) = (unsafe { submenu.as_mut() }) else {
        return false;
    };
    let Some(separator) = (unsafe { separator.as_ref() }) else {
        return false;
    };
    submenu.submenu.borrow().append(&separator.item).is_ok()
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_submenu_append_check_item(
    submenu: *mut WrySubmenuHandle,
    item: *mut WryCheckMenuItemHandle,
) -> bool {
    let Some(submenu) = (unsafe { submenu.as_mut() }) else {
        return false;
    };
    let Some(item) = (unsafe { item.as_ref() }) else {
        return false;
    };
    submenu.submenu.borrow().append(&item.item).is_ok()
}

// ============================================================================
// Menu Item
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_new(
    id: *const c_char,
    title: *const c_char,
    enabled: bool,
    accelerator: *const c_char,
) -> *mut WryMenuItemHandle {
    use muda::{MenuId, MenuItem};
    use std::ffi::CString;

    guard_panic(|| {
        let title = opt_cstring(title).unwrap_or_default();
        let accelerator = accelerator_from_ptr(accelerator);
        let item = if let Some(id) = opt_cstring(id) {
            MenuItem::with_id(MenuId::new(id.clone()), title, enabled, accelerator)
        } else {
            MenuItem::new(title, enabled, accelerator)
        };
        let identifier = CString::new(item.id().as_ref()).expect("menu item id contains null byte");
        Box::into_raw(Box::new(WryMenuItemHandle { item, identifier }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_free(item: *mut WryMenuItemHandle) {
    if !item.is_null() {
        unsafe { drop(Box::from_raw(item)) };
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_enabled(
    item: *mut WryMenuItemHandle,
    enabled: bool,
) -> bool {
    let Some(item) = (unsafe { item.as_mut() }) else {
        return false;
    };
    item.item.set_enabled(enabled);
    true
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_is_enabled(item: *mut WryMenuItemHandle) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_ref() }) else {
            return false;
        };
        item.item.is_enabled()
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_text(item: *mut WryMenuItemHandle) -> *const c_char {
    guard_panic_value(|| {
        let Some(item) = (unsafe { item.as_ref() }) else {
            return ptr::null();
        };
        write_string_to_buffer(&TITLE_BUFFER, item.item.text())
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_text(
    item: *mut WryMenuItemHandle,
    title: *const c_char,
) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_mut() }) else {
            return false;
        };
        let text = opt_cstring(title).unwrap_or_default();
        item.item.set_text(text);
        true
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_accelerator(
    item: *mut WryMenuItemHandle,
    accelerator: *const c_char,
) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_mut() }) else {
            return false;
        };
        item.item
            .set_accelerator(accelerator_from_ptr(accelerator))
            .is_ok()
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_menu_item_identifier(item: *mut WryMenuItemHandle) -> *const c_char {
    let Some(item) = (unsafe { item.as_ref() }) else {
        return ptr::null();
    };
    item.identifier.as_ptr()
}

// ============================================================================
// Separator
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_separator_new() -> *mut WrySeparatorHandle {
    use muda::PredefinedMenuItem;
    use std::ffi::CString;

    guard_panic(|| {
        let item = PredefinedMenuItem::separator();
        let identifier = CString::new(item.id().as_ref()).expect("separator id contains null byte");
        Box::into_raw(Box::new(WrySeparatorHandle { item, identifier }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_separator_free(separator: *mut WrySeparatorHandle) {
    if !separator.is_null() {
        unsafe { drop(Box::from_raw(separator)) };
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_separator_identifier(separator: *mut WrySeparatorHandle) -> *const c_char {
    let Some(separator) = (unsafe { separator.as_ref() }) else {
        return ptr::null();
    };
    separator.identifier.as_ptr()
}

// ============================================================================
// Check Menu Item
// ============================================================================

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_new(
    id: *const c_char,
    title: *const c_char,
    enabled: bool,
    checked: bool,
    accelerator: *const c_char,
) -> *mut WryCheckMenuItemHandle {
    use muda::{CheckMenuItem, MenuId};
    use std::ffi::CString;

    guard_panic(|| {
        let title = opt_cstring(title).unwrap_or_default();
        let accelerator = accelerator_from_ptr(accelerator);
        let item = if let Some(id) = opt_cstring(id) {
            CheckMenuItem::with_id(MenuId::new(id.clone()), title, enabled, checked, accelerator)
        } else {
            CheckMenuItem::new(title, enabled, checked, accelerator)
        };
        let identifier = CString::new(item.id().as_ref()).expect("check menu item id contains null byte");
        Box::into_raw(Box::new(WryCheckMenuItemHandle { item, identifier }))
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_free(item: *mut WryCheckMenuItemHandle) {
    if !item.is_null() {
        unsafe { drop(Box::from_raw(item)) };
    }
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_is_checked(item: *mut WryCheckMenuItemHandle) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_ref() }) else {
            return false;
        };
        item.item.is_checked()
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_checked(
    item: *mut WryCheckMenuItemHandle,
    checked: bool,
) -> bool {
    let Some(item) = (unsafe { item.as_mut() }) else {
        return false;
    };
    item.item.set_checked(checked);
    true
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_is_enabled(item: *mut WryCheckMenuItemHandle) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_ref() }) else {
            return false;
        };
        item.item.is_enabled()
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_enabled(
    item: *mut WryCheckMenuItemHandle,
    enabled: bool,
) -> bool {
    let Some(item) = (unsafe { item.as_mut() }) else {
        return false;
    };
    item.item.set_enabled(enabled);
    true
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_text(item: *mut WryCheckMenuItemHandle) -> *const c_char {
    guard_panic_value(|| {
        let Some(item) = (unsafe { item.as_ref() }) else {
            return ptr::null();
        };
        write_string_to_buffer(&TITLE_BUFFER, item.item.text())
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_text(
    item: *mut WryCheckMenuItemHandle,
    title: *const c_char,
) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_mut() }) else {
            return false;
        };
        let text = opt_cstring(title).unwrap_or_default();
        item.item.set_text(text);
        true
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_accelerator(
    item: *mut WryCheckMenuItemHandle,
    accelerator: *const c_char,
) -> bool {
    guard_panic_bool(|| {
        let Some(item) = (unsafe { item.as_mut() }) else {
            return false;
        };
        item.item
            .set_accelerator(accelerator_from_ptr(accelerator))
            .is_ok()
    })
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_identifier(item: *mut WryCheckMenuItemHandle) -> *const c_char {
    let Some(item) = (unsafe { item.as_ref() }) else {
        return ptr::null();
    };
    item.identifier.as_ptr()
}

// ============================================================================
// Non-macOS Stubs
// ============================================================================

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_new() -> *mut WryMenuBarHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_new_with_id(_id: *const c_char) -> *mut WryMenuBarHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_free(_menu: *mut WryMenuBarHandle) {}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_identifier(_menu: *mut WryMenuBarHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_append_submenu(_menu: *mut WryMenuBarHandle, _submenu: *mut WrySubmenuHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_bar_set_app_menu(_menu: *mut WryMenuBarHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_new(_title: *const c_char, _enabled: bool) -> *mut WrySubmenuHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_new_with_id(_id: *const c_char, _title: *const c_char, _enabled: bool) -> *mut WrySubmenuHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_free(_submenu: *mut WrySubmenuHandle) {}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_identifier(_submenu: *mut WrySubmenuHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_append_item(_submenu: *mut WrySubmenuHandle, _item: *mut WryMenuItemHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_append_separator(_submenu: *mut WrySubmenuHandle, _separator: *mut WrySeparatorHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_submenu_append_check_item(_submenu: *mut WrySubmenuHandle, _item: *mut WryCheckMenuItemHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_new(_id: *const c_char, _title: *const c_char, _enabled: bool, _accelerator: *const c_char) -> *mut WryMenuItemHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_free(_item: *mut WryMenuItemHandle) {}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_enabled(_item: *mut WryMenuItemHandle, _enabled: bool) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_is_enabled(_item: *mut WryMenuItemHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_text(_item: *mut WryMenuItemHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_text(_item: *mut WryMenuItemHandle, _title: *const c_char) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_set_accelerator(_item: *mut WryMenuItemHandle, _accelerator: *const c_char) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_menu_item_identifier(_item: *mut WryMenuItemHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_separator_new() -> *mut WrySeparatorHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_separator_free(_separator: *mut WrySeparatorHandle) {}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_separator_identifier(_separator: *mut WrySeparatorHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_new(_id: *const c_char, _title: *const c_char, _enabled: bool, _checked: bool, _accelerator: *const c_char) -> *mut WryCheckMenuItemHandle { ptr::null_mut() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_free(_item: *mut WryCheckMenuItemHandle) {}

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_is_checked(_item: *mut WryCheckMenuItemHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_checked(_item: *mut WryCheckMenuItemHandle, _checked: bool) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_is_enabled(_item: *mut WryCheckMenuItemHandle) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_enabled(_item: *mut WryCheckMenuItemHandle, _enabled: bool) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_text(_item: *mut WryCheckMenuItemHandle) -> *const c_char { ptr::null() }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_text(_item: *mut WryCheckMenuItemHandle, _title: *const c_char) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_set_accelerator(_item: *mut WryCheckMenuItemHandle, _accelerator: *const c_char) -> bool { false }

#[cfg(not(target_os = "macos"))]
#[no_mangle]
pub extern "C" fn wry_check_menu_item_identifier(_item: *mut WryCheckMenuItemHandle) -> *const c_char { ptr::null() }
