//! Thread dispatch utilities
//!
//! Provides thread-safe invocation of callbacks on the UI thread.

use std::os::raw::c_void;
use std::sync::{Arc, Condvar, Mutex};

use crate::app::{AppState, UserEvent};
use crate::error::set_last_error;
use crate::types::{InvokeCallback, WryApp};

// ============================================================================
// FFI Functions
// ============================================================================

/// Execute callback on UI thread (thread-safe, can be called from any thread)
///
/// The callback will be queued and executed on the main/UI thread during
/// the next event loop iteration.
#[no_mangle]
pub unsafe extern "C" fn wry_invoke(
    app: WryApp,
    callback: InvokeCallback,
    user_data: *mut c_void,
) {
    if app.is_null() {
        set_last_error("Null app handle");
        return;
    }

    let state = &*(app as *const AppState);
    log::debug!("wry_invoke: queueing callback for UI thread");

    // Wrap the callback and user_data in a Send closure
    // Safety: user_data is managed by the caller who ensures validity
    let user_data_ptr = user_data as usize;
    let boxed_callback: Box<dyn FnOnce() + Send> = Box::new(move || {
        callback(user_data_ptr as *mut c_void);
    });

    if let Err(e) = state.event_loop_proxy.send_event(UserEvent::InvokeCallback(boxed_callback)) {
        log::error!("Failed to send invoke event: {:?}", e);
        set_last_error("Failed to queue callback - event loop may not be running");
    }
}

/// Execute callback on UI thread and wait for completion
///
/// This function blocks until the callback has been executed on the UI thread.
/// **Warning**: Do not call this from the UI thread as it will deadlock.
#[no_mangle]
pub unsafe extern "C" fn wry_invoke_sync(
    app: WryApp,
    callback: InvokeCallback,
    user_data: *mut c_void,
) {
    if app.is_null() {
        set_last_error("Null app handle");
        return;
    }

    let state = &*(app as *const AppState);
    log::debug!("wry_invoke_sync: queueing callback for UI thread (blocking)");

    // Create synchronization primitives
    let done = Arc::new((Mutex::new(false), Condvar::new()));
    let done_clone = done.clone();

    // Wrap the callback with synchronization
    let user_data_ptr = user_data as usize;
    let boxed_callback: Box<dyn FnOnce() + Send> = Box::new(move || {
        // Execute the actual callback
        callback(user_data_ptr as *mut c_void);

        // Signal completion
        let (lock, cvar) = &*done_clone;
        let mut completed = lock.lock().unwrap();
        *completed = true;
        cvar.notify_one();
    });

    // Send the event
    if let Err(e) = state.event_loop_proxy.send_event(UserEvent::InvokeCallback(boxed_callback)) {
        log::error!("Failed to send invoke_sync event: {:?}", e);
        set_last_error("Failed to queue callback - event loop may not be running");
        return;
    }

    // Wait for completion
    let (lock, cvar) = &*done;
    let mut completed = lock.lock().unwrap();
    while !*completed {
        completed = cvar.wait(completed).unwrap();
    }

    log::debug!("wry_invoke_sync: callback completed");
}
