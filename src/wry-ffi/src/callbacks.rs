//! Callback management for window events
//!
//! Stores and invokes C function pointers for various window events.
//! Includes a global registry for IPC message routing.

use std::collections::HashMap;
use std::ffi::CString;
use std::os::raw::c_void;
use std::sync::RwLock;

use once_cell::sync::Lazy;
use tao::window::WindowId;

use crate::types::{
    NavigationCallback, WebMessageCallback, WindowClosingCallback, WindowFocusCallback,
    WindowMovedCallback, WindowResizedCallback, WryWindow,
};

/// Global registry mapping WindowId to callback info for IPC routing
static CALLBACK_REGISTRY: Lazy<RwLock<HashMap<WindowId, CallbackEntry>>> =
    Lazy::new(|| RwLock::new(HashMap::new()));

/// Entry in the callback registry
struct CallbackEntry {
    window_ptr: WryWindow,
    message_callback: Option<(WebMessageCallback, *mut c_void)>,
}

// Safety: callback pointers and user_data are managed by caller
unsafe impl Send for CallbackEntry {}
unsafe impl Sync for CallbackEntry {}

/// Register a window in the callback registry
pub fn register_window(window_id: WindowId, window_ptr: WryWindow) {
    let mut registry = CALLBACK_REGISTRY.write().unwrap();
    registry.insert(window_id, CallbackEntry {
        window_ptr,
        message_callback: None,
    });
}

/// Unregister a window from the callback registry
pub fn unregister_window(window_id: WindowId) {
    let mut registry = CALLBACK_REGISTRY.write().unwrap();
    registry.remove(&window_id);
}

/// Set the message callback in the global registry
pub fn set_global_message_callback(
    window_id: WindowId,
    callback: WebMessageCallback,
    user_data: *mut c_void,
) {
    let mut registry = CALLBACK_REGISTRY.write().unwrap();
    if let Some(entry) = registry.get_mut(&window_id) {
        entry.message_callback = Some((callback, user_data));
    }
}

/// Invoke message callback from the IPC handler
/// This is called from the webview's IPC handler closure
pub fn invoke_message_callback(window_id: WindowId, message: &str) {
    let registry = CALLBACK_REGISTRY.read().unwrap();
    if let Some(entry) = registry.get(&window_id) {
        if let Some((callback, user_data)) = entry.message_callback {
            if let Ok(c_msg) = CString::new(message) {
                callback(entry.window_ptr, c_msg.as_ptr(), user_data);
            }
        }
    }
}

/// Stored callback with its user data
struct StoredCallback<F> {
    callback: F,
    user_data: *mut c_void,
}

// Safety: user_data is managed by the caller who must ensure thread safety
unsafe impl<F: Send> Send for StoredCallback<F> {}
unsafe impl<F: Sync> Sync for StoredCallback<F> {}

/// All callbacks for a window
pub struct WindowCallbacks {
    window_id: WindowId,
    closing: Option<StoredCallback<WindowClosingCallback>>,
    resized: Option<StoredCallback<WindowResizedCallback>>,
    moved: Option<StoredCallback<WindowMovedCallback>>,
    focus: Option<StoredCallback<WindowFocusCallback>>,
    navigation: Option<StoredCallback<NavigationCallback>>,
    /// Cached window pointer for callbacks
    window_ptr: WryWindow,
}

impl WindowCallbacks {
    /// Create new callbacks for a window
    pub fn new(window_id: WindowId) -> Self {
        Self {
            window_id,
            closing: None,
            resized: None,
            moved: None,
            focus: None,
            navigation: None,
            window_ptr: std::ptr::null_mut(),
        }
    }

    /// Set the window pointer used in callbacks
    pub fn set_window_ptr(&mut self, ptr: WryWindow) {
        self.window_ptr = ptr;
        // Also register in global registry
        register_window(self.window_id, ptr);
    }

    // ========================================================================
    // Setters
    // ========================================================================

    pub fn set_message(&mut self, callback: WebMessageCallback, user_data: *mut c_void) {
        // Store in global registry for IPC handler access
        set_global_message_callback(self.window_id, callback, user_data);
    }

    pub fn set_closing(&mut self, callback: WindowClosingCallback, user_data: *mut c_void) {
        self.closing = Some(StoredCallback {
            callback,
            user_data,
        });
    }

    pub fn set_resized(&mut self, callback: WindowResizedCallback, user_data: *mut c_void) {
        self.resized = Some(StoredCallback {
            callback,
            user_data,
        });
    }

    pub fn set_moved(&mut self, callback: WindowMovedCallback, user_data: *mut c_void) {
        self.moved = Some(StoredCallback {
            callback,
            user_data,
        });
    }

    pub fn set_focus(&mut self, callback: WindowFocusCallback, user_data: *mut c_void) {
        self.focus = Some(StoredCallback {
            callback,
            user_data,
        });
    }

    pub fn set_navigation(&mut self, callback: NavigationCallback, user_data: *mut c_void) {
        self.navigation = Some(StoredCallback {
            callback,
            user_data,
        });
    }

    // ========================================================================
    // Callers
    // ========================================================================

    /// Call the closing callback, returns true if close should proceed
    pub fn call_closing(&self) -> bool {
        if let Some(ref cb) = self.closing {
            (cb.callback)(self.window_ptr, cb.user_data)
        } else {
            true // Default: allow close
        }
    }

    /// Call the resized callback
    pub fn call_resized(&self, width: u32, height: u32) {
        if let Some(ref cb) = self.resized {
            (cb.callback)(self.window_ptr, width, height, cb.user_data);
        }
    }

    /// Call the moved callback
    pub fn call_moved(&self, x: i32, y: i32) {
        if let Some(ref cb) = self.moved {
            (cb.callback)(self.window_ptr, x, y, cb.user_data);
        }
    }

    /// Call the focus callback
    pub fn call_focus(&self, focused: bool) {
        if let Some(ref cb) = self.focus {
            (cb.callback)(self.window_ptr, focused, cb.user_data);
        }
    }

    /// Call the navigation callback, returns true if navigation should proceed
    pub fn call_navigation(&self, url: &str) -> bool {
        if let Some(ref cb) = self.navigation {
            if let Ok(c_url) = CString::new(url) {
                (cb.callback)(self.window_ptr, c_url.as_ptr(), cb.user_data)
            } else {
                true
            }
        } else {
            true // Default: allow navigation
        }
    }
}

impl Default for WindowCallbacks {
    fn default() -> Self {
        Self::new(unsafe { std::mem::zeroed() })
    }
}

impl Drop for WindowCallbacks {
    fn drop(&mut self) {
        // Unregister from global registry
        unregister_window(self.window_id);
    }
}

// ============================================================================
// FFI Functions
// ============================================================================

use crate::window::get_window_state_mut;

/// Set callback for web messages
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_message_callback(
    window: WryWindow,
    callback: WebMessageCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_message(callback, user_data);
        log::debug!("Message callback set for window {:?}", state.id);
    }
}

/// Set callback for window closing
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_closing_callback(
    window: WryWindow,
    callback: WindowClosingCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_closing(callback, user_data);
        log::debug!("Closing callback set for window {:?}", state.id);
    }
}

/// Set callback for window resize
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_resized_callback(
    window: WryWindow,
    callback: WindowResizedCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_resized(callback, user_data);
        log::debug!("Resized callback set for window {:?}", state.id);
    }
}

/// Set callback for window move
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_moved_callback(
    window: WryWindow,
    callback: WindowMovedCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_moved(callback, user_data);
        log::debug!("Moved callback set for window {:?}", state.id);
    }
}

/// Set callback for focus change
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_focus_callback(
    window: WryWindow,
    callback: WindowFocusCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_focus(callback, user_data);
        log::debug!("Focus callback set for window {:?}", state.id);
    }
}

/// Set callback for navigation (can cancel)
#[no_mangle]
pub unsafe extern "C" fn wry_window_set_navigation_callback(
    window: WryWindow,
    callback: NavigationCallback,
    user_data: *mut c_void,
) {
    if let Some(state) = get_window_state_mut(window) {
        state.callbacks.set_window_ptr(window);
        state.callbacks.set_navigation(callback, user_data);
        log::debug!("Navigation callback set for window {:?}", state.id);
    }
}
