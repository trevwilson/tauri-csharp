//! wry-ffi: C FFI bindings for Wry WebView library
//!
//! This crate provides a flat C API for creating windows with webviews using
//! Wry (WebView) and Tao (windowing). It is designed for P/Invoke from .NET.
//!
//! # Design Principles
//!
//! 1. **Flat C API** — No Rust types exposed, only C-compatible types
//! 2. **Opaque handles** — Return pointers to Rust objects, caller treats as opaque
//! 3. **Explicit memory management** — Caller responsible for calling destroy functions
//! 4. **Error codes** — Return status codes, provide error message retrieval
//! 5. **Callback-based async** — Use function pointers for events
//! 6. **Thread safety** — Document thread requirements, provide thread-safe where possible
//!
//! # Thread Safety
//!
//! - `wry_app_create` and `wry_app_run` must be called from the main thread
//! - Window operations should be called from the main thread
//! - `wry_webview_send_message` and `wry_invoke` are thread-safe
//! - Callbacks are invoked on the main/UI thread

#![allow(clippy::missing_safety_doc)]

pub mod app;
pub mod bridge;
pub mod callbacks;
pub mod dispatch;
pub mod error;
pub mod protocol;
pub mod string;
pub mod types;
pub mod webview;
pub mod window;
pub mod window_ops;

// Re-export public FFI functions
pub use app::{
    wry_app_create, wry_app_destroy, wry_app_quit, wry_app_run, wry_get_last_error, wry_version,
};
pub use callbacks::{
    wry_window_set_closing_callback, wry_window_set_focus_callback,
    wry_window_set_message_callback, wry_window_set_moved_callback,
    wry_window_set_navigation_callback, wry_window_set_resized_callback,
};
pub use dispatch::{wry_invoke, wry_invoke_sync};
pub use protocol::wry_register_protocol;
pub use string::wry_string_free;
pub use types::*;
pub use webview::{
    wry_webview_close_devtools, wry_webview_evaluate_script, wry_webview_get_url,
    wry_webview_load_html, wry_webview_navigate, wry_webview_open_devtools,
    wry_webview_send_message, wry_webview_set_zoom,
};
pub use window::{wry_window_create, wry_window_destroy};
pub use window_ops::{
    wry_window_close, wry_window_focus, wry_window_get_position, wry_window_get_size,
    wry_window_get_title, wry_window_is_visible, wry_window_maximize, wry_window_minimize,
    wry_window_set_fullscreen, wry_window_set_position, wry_window_set_size,
    wry_window_set_title, wry_window_set_visible, wry_window_unmaximize,
};
