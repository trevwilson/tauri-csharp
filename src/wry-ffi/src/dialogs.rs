//! File and message dialog FFI functions
//!
//! Provides native file open/save dialogs and message/confirmation dialogs.

use std::ffi::CString;
use std::os::raw::c_char;

use rfd::FileDialog;

use crate::helpers::*;
use crate::types::*;

// ============================================================================
// File Dialogs
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_dialog_open(
    options: *const WryDialogOpenOptions,
) -> WryDialogSelection {
    guard_panic_value(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return WryDialogSelection::default();
        };

        let mut dialog = FileDialog::new();
        if let Some(title) = opt_cstring(options.title) {
            dialog = dialog.set_title(&title);
        }
        if let Some(path) = opt_cstring(options.default_path) {
            dialog = dialog.set_directory(std::path::Path::new(&path));
        }

        if options.filter_count > 0 && !options.filters.is_null() && !options.allow_directories {
            let filters =
                unsafe { std::slice::from_raw_parts(options.filters, options.filter_count) };
            dialog = dialog_apply_filters(dialog, filters);
        }

        let selection_paths = if options.allow_directories {
            if options.allow_multiple {
                dialog.pick_folders().unwrap_or_default()
            } else {
                dialog.pick_folder().into_iter().collect()
            }
        } else if options.allow_multiple {
            dialog.pick_files().unwrap_or_default()
        } else {
            dialog.pick_file().into_iter().collect()
        };

        dialog_selection_from_paths(selection_paths)
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_save(
    options: *const WryDialogSaveOptions,
) -> WryDialogSelection {
    guard_panic_value(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return WryDialogSelection::default();
        };

        let mut dialog = FileDialog::new();
        if let Some(title) = opt_cstring(options.title) {
            dialog = dialog.set_title(&title);
        }
        if let Some(path) = opt_cstring(options.default_path) {
            dialog = dialog.set_directory(std::path::Path::new(&path));
        }
        if let Some(name) = opt_cstring(options.default_name) {
            dialog = dialog.set_file_name(&name);
        }

        if options.filter_count > 0 && !options.filters.is_null() {
            let filters =
                unsafe { std::slice::from_raw_parts(options.filters, options.filter_count) };
            dialog = dialog_apply_filters(dialog, filters);
        }

        let selection_paths = dialog.save_file().into_iter().collect();
        dialog_selection_from_paths(selection_paths)
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_selection_free(selection: WryDialogSelection) {
    let _ = guard_panic_bool(|| {
        if selection.count == 0 || selection.paths.is_null() {
            return true;
        }

        unsafe {
            let slice = std::slice::from_raw_parts_mut(selection.paths, selection.count);
            let boxed = Box::from_raw(slice as *mut [*mut c_char]);
            for &ptr in boxed.iter() {
                if !ptr.is_null() {
                    drop(CString::from_raw(ptr));
                }
            }
        }
        true
    });
}

// ============================================================================
// Message Dialogs
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_dialog_message(options: *const WryMessageDialogOptions) -> bool {
    guard_panic_bool(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return false;
        };

        let title = opt_cstring(options.title).unwrap_or_else(|| "Message".to_string());
        let message = opt_cstring(options.message).unwrap_or_default();

        let icon = match options.level {
            WryMessageDialogLevel::Info => tinyfiledialogs::MessageBoxIcon::Info,
            WryMessageDialogLevel::Warning => tinyfiledialogs::MessageBoxIcon::Warning,
            WryMessageDialogLevel::Error => tinyfiledialogs::MessageBoxIcon::Error,
        };

        match options.buttons {
            WryMessageDialogButtons::Ok => {
                tinyfiledialogs::message_box_ok(&title, &message, icon);
                true
            }
            WryMessageDialogButtons::OkCancel => {
                tinyfiledialogs::message_box_ok_cancel(&title, &message, icon, tinyfiledialogs::OkCancel::Ok)
                    == tinyfiledialogs::OkCancel::Ok
            }
            // YesNoCancel falls back to YesNo — tinyfiledialogs has no three-button dialog
            WryMessageDialogButtons::YesNo | WryMessageDialogButtons::YesNoCancel => {
                tinyfiledialogs::message_box_yes_no(&title, &message, icon, tinyfiledialogs::YesNo::Yes)
                    == tinyfiledialogs::YesNo::Yes
            }
        }
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_confirm(options: *const WryConfirmDialogOptions) -> bool {
    guard_panic_bool(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return false;
        };

        let title = opt_cstring(options.title).unwrap_or_else(|| "Confirm".to_string());
        let message = opt_cstring(options.message).unwrap_or_default();

        let icon = match options.level {
            WryMessageDialogLevel::Info => tinyfiledialogs::MessageBoxIcon::Info,
            WryMessageDialogLevel::Warning => tinyfiledialogs::MessageBoxIcon::Warning,
            WryMessageDialogLevel::Error => tinyfiledialogs::MessageBoxIcon::Error,
        };

        tinyfiledialogs::message_box_ok_cancel(&title, &message, icon, tinyfiledialogs::OkCancel::Ok)
            == tinyfiledialogs::OkCancel::Ok
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_ask(options: *const WryAskDialogOptions) -> bool {
    guard_panic_bool(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return false;
        };

        let title = opt_cstring(options.title).unwrap_or_else(|| "Question".to_string());
        let message = opt_cstring(options.message).unwrap_or_default();

        let icon = match options.level {
            WryMessageDialogLevel::Info => tinyfiledialogs::MessageBoxIcon::Info,
            WryMessageDialogLevel::Warning => tinyfiledialogs::MessageBoxIcon::Warning,
            WryMessageDialogLevel::Error => tinyfiledialogs::MessageBoxIcon::Error,
        };

        tinyfiledialogs::message_box_yes_no(&title, &message, icon, tinyfiledialogs::YesNo::Yes)
            == tinyfiledialogs::YesNo::Yes
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_prompt(
    options: *const WryPromptDialogOptions,
) -> WryPromptDialogResult {
    guard_panic_value(|| {
        let Some(options) = (unsafe { options.as_ref() }) else {
            return WryPromptDialogResult::default();
        };

        let title = opt_cstring(options.title).unwrap_or_default();
        let message = opt_cstring(options.message).unwrap_or_default();
        if message.is_empty() {
            return WryPromptDialogResult::default();
        }

        let default_value = opt_cstring(options.default_value);
        let placeholder = opt_cstring(options.placeholder);
        let default_text = default_value.or(placeholder).unwrap_or_default();
        let title_ref = if title.is_empty() {
            "Prompt"
        } else {
            title.as_str()
        };

        let input = tinyfiledialogs::input_box(title_ref, &message, default_text.as_str());
        prompt_result_from_string(input)
    })
}

#[no_mangle]
pub extern "C" fn wry_dialog_prompt_result_free(result: WryPromptDialogResult) {
    let _ = guard_panic_bool(|| {
        if !result.value.is_null() {
            unsafe {
                drop(CString::from_raw(result.value));
            }
        }
        true
    });
}
