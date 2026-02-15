//! Global keyboard shortcut support via global-hotkey
//!
//! Provides FFI functions for registering and unregistering global hotkeys.
//! Hotkey events are delivered through the event loop as JSON events.
//!
//! Note: On WSL2/Wayland, global shortcuts only capture keys when X11 apps
//! have focus, not system-wide. This is a platform limitation.

use std::sync::Mutex;
use std::collections::HashMap;
use std::sync::atomic::{AtomicU32, Ordering};

use global_hotkey::{GlobalHotKeyManager, hotkey::HotKey};

use crate::helpers::*;

// ============================================================================
// Global State
// ============================================================================

static NEXT_ID: AtomicU32 = AtomicU32::new(1);

/// The global hotkey manager - must be created on the main thread.
static MANAGER: Mutex<Option<GlobalHotKeyManager>> = Mutex::new(None);

/// Map from our sequential IDs to the actual HotKey objects.
static REGISTRY: Mutex<Option<HashMap<u32, HotKey>>> = Mutex::new(None);

fn ensure_manager() -> bool {
    let mut mgr = MANAGER.lock().unwrap();
    if mgr.is_none() {
        match GlobalHotKeyManager::new() {
            Ok(m) => {
                *mgr = Some(m);
                let mut reg = REGISTRY.lock().unwrap();
                *reg = Some(HashMap::new());
                true
            }
            Err(e) => {
                log::error!("Failed to create GlobalHotKeyManager: {e}");
                false
            }
        }
    } else {
        true
    }
}

// ============================================================================
// FFI Functions
// ============================================================================

/// Register a global shortcut from an accelerator string (e.g. "CmdOrCtrl+Shift+T").
///
/// Returns a non-zero shortcut ID on success, or 0 on failure.
///
/// Accelerator format follows the global-hotkey crate convention:
/// - Modifiers: Alt, Ctrl, CmdOrCtrl, Meta, Shift, Super
/// - Keys: A-Z, 0-9, F1-F24, Space, Enter, Tab, Escape, etc.
/// - Example: "CmdOrCtrl+Shift+T", "Alt+F4", "Ctrl+A"
#[no_mangle]
pub extern "C" fn wry_shortcut_register(
    accelerator: *const std::os::raw::c_char,
) -> u32 {
    let Some(accel_str) = opt_cstring(accelerator) else {
        return 0;
    };

    guard_panic_value(|| {
        if !ensure_manager() {
            return 0;
        }

        let hotkey: HotKey = match accel_str.parse() {
            Ok(hk) => hk,
            Err(e) => {
                log::error!("Failed to parse accelerator '{}': {e}", accel_str);
                return 0;
            }
        };

        let mgr = MANAGER.lock().unwrap();
        let mgr = mgr.as_ref().unwrap();

        if let Err(e) = mgr.register(hotkey) {
            log::error!("Failed to register shortcut '{}': {e}", accel_str);
            return 0;
        }

        let id = NEXT_ID.fetch_add(1, Ordering::Relaxed);
        let mut reg = REGISTRY.lock().unwrap();
        reg.as_mut().unwrap().insert(id, hotkey);

        log::info!("Registered global shortcut '{}' with id {}", accel_str, id);
        id
    })
}

/// Unregister a global shortcut by ID.
#[no_mangle]
pub extern "C" fn wry_shortcut_unregister(shortcut_id: u32) -> bool {
    guard_panic_bool(|| {
        let mgr = MANAGER.lock().unwrap();
        let mgr = match mgr.as_ref() {
            Some(m) => m,
            None => return false,
        };

        let mut reg = REGISTRY.lock().unwrap();
        let reg = match reg.as_mut() {
            Some(r) => r,
            None => return false,
        };

        if let Some(hotkey) = reg.remove(&shortcut_id) {
            mgr.unregister(hotkey).is_ok()
        } else {
            false
        }
    })
}

/// Unregister all global shortcuts.
#[no_mangle]
pub extern "C" fn wry_shortcut_unregister_all() -> bool {
    guard_panic_bool(|| {
        let mgr = MANAGER.lock().unwrap();
        let mgr = match mgr.as_ref() {
            Some(m) => m,
            None => return false,
        };

        let mut reg = REGISTRY.lock().unwrap();
        let reg = match reg.as_mut() {
            Some(r) => r,
            None => return false,
        };

        let hotkeys: Vec<HotKey> = reg.values().cloned().collect();
        let result = mgr.unregister_all(&hotkeys).is_ok();
        reg.clear();
        result
    })
}

/// Check for pending hotkey events. Returns the shortcut ID if an event is pending,
/// or 0 if no events. Used internally by the event loop.
pub(crate) fn poll_hotkey_event() -> Option<u32> {
    use global_hotkey::GlobalHotKeyEvent;

    if let Ok(event) = GlobalHotKeyEvent::receiver().try_recv() {
        let reg = REGISTRY.lock().unwrap();
        if let Some(reg) = reg.as_ref() {
            for (&id, hotkey) in reg.iter() {
                if hotkey.id() == event.id {
                    return Some(id);
                }
            }
        }
    }
    None
}
