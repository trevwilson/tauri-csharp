//! wry-ffi: C FFI bindings for Wry WebView library
//!
//! This crate provides a flat C API for creating windows with webviews using
//! Wry (WebView) and Tao (windowing). It is designed for P/Invoke from .NET.
//!
//! Based on Velox's runtime-wry-ffi with the following changes:
//! - Renamed velox_* functions to wry_*
//! - Split into modular files for maintainability
//!
//! # Design Principles
//!
//! 1. **Flat C API** - No Rust types exposed, only C-compatible types
//! 2. **Opaque handles** - Return pointers to Rust objects, caller treats as opaque
//! 3. **Explicit memory management** - Caller responsible for calling free functions
//! 4. **Callback-based events** - Events serialized to JSON, passed via callback
//! 5. **Cross-platform** - Menu/tray features are macOS-only with no-op stubs elsewhere
//!
//! # Usage Pattern (C#)
//!
//! ```csharp
//! // 1. Create event loop
//! var eventLoop = wry_event_loop_new();
//! var proxy = wry_event_loop_create_proxy(eventLoop);
//!
//! // 2. Create window
//! var windowConfig = new WryWindowConfig { width = 800, height = 600 };
//! var window = wry_window_build(eventLoop, ref windowConfig);
//!
//! // 3. Create webview
//! var webviewConfig = new WryWebviewConfig { url = "https://example.com" };
//! var webview = wry_webview_build(window, ref webviewConfig);
//!
//! // 4. Run event loop with callback
//! wry_event_loop_pump(eventLoop, eventCallback, userData);
//!
//! // 5. Cleanup
//! wry_webview_free(webview);
//! wry_window_free(window);
//! wry_event_loop_proxy_free(proxy);
//! wry_event_loop_free(eventLoop);
//! ```

#![allow(clippy::missing_safety_doc)]

// ============================================================================
// Modules
// ============================================================================

pub mod types;
pub mod helpers;
pub mod events;
pub mod app;
pub mod window;
pub mod webview;
pub mod dialogs;
pub mod menu;
pub mod tray;

// ============================================================================
// Re-exports: Types
// ============================================================================

pub use types::*;

// ============================================================================
// Re-exports: App/Event Loop
// ============================================================================

pub use app::{
    wry_event_loop_new,
    wry_event_loop_free,
    wry_event_loop_create_proxy,
    wry_event_loop_proxy_request_exit,
    wry_event_loop_proxy_send_user_event,
    wry_event_loop_proxy_free,
    wry_event_loop_set_activation_policy,
    wry_event_loop_set_dock_visibility,
    wry_event_loop_hide_application,
    wry_event_loop_show_application,
    wry_event_loop_pump,
    wry_app_state_force_launched,
    wry_ffi_abi_version,
    wry_library_name,
    wry_crate_version,
    wry_webview_version,
};

// ============================================================================
// Re-exports: Window
// ============================================================================

pub use window::{
    wry_window_build,
    wry_window_free,
    wry_window_identifier,
    wry_window_set_title,
    wry_window_set_fullscreen,
    wry_window_set_decorations,
    wry_window_set_always_on_bottom,
    wry_window_set_visible_on_all_workspaces,
    wry_window_set_content_protected,
    wry_window_set_resizable,
    wry_window_set_always_on_top,
    wry_window_set_visible,
    wry_window_set_maximized,
    wry_window_set_minimized,
    wry_window_set_skip_taskbar,
    wry_window_set_minimizable,
    wry_window_set_maximizable,
    wry_window_set_closable,
    wry_window_set_background_color,
    wry_window_set_theme,
    wry_window_set_focusable,
    wry_window_set_size,
    wry_window_set_position,
    wry_window_set_min_size,
    wry_window_set_max_size,
    wry_window_is_maximized,
    wry_window_is_minimized,
    wry_window_is_visible,
    wry_window_is_resizable,
    wry_window_is_decorated,
    wry_window_is_always_on_top,
    wry_window_is_minimizable,
    wry_window_is_maximizable,
    wry_window_is_closable,
    wry_window_is_fullscreen,
    wry_window_is_focused,
    wry_window_scale_factor,
    wry_window_inner_position,
    wry_window_outer_position,
    wry_window_inner_size,
    wry_window_outer_size,
    wry_window_title,
    wry_window_cursor_position,
    wry_window_current_monitor,
    wry_window_primary_monitor,
    wry_window_available_monitors,
    wry_window_monitor_from_point,
    wry_window_focus,
    wry_window_request_redraw,
    wry_window_request_user_attention,
    wry_window_clear_user_attention,
    wry_window_set_cursor_grab,
    wry_window_set_cursor_visible,
    wry_window_set_cursor_position,
    wry_window_set_ignore_cursor_events,
    wry_window_start_dragging,
    wry_window_start_resize_dragging,
};

// ============================================================================
// Re-exports: Webview
// ============================================================================

pub use webview::{
    wry_webview_build,
    wry_webview_free,
    wry_webview_identifier,
    wry_webview_navigate,
    wry_webview_reload,
    wry_webview_evaluate_script,
    wry_webview_set_zoom,
    wry_webview_show,
    wry_webview_hide,
    wry_webview_set_bounds,
    wry_webview_clear_browsing_data,
};

// ============================================================================
// Re-exports: Dialogs
// ============================================================================

pub use dialogs::{
    wry_dialog_open,
    wry_dialog_save,
    wry_dialog_selection_free,
    wry_dialog_message,
    wry_dialog_confirm,
    wry_dialog_ask,
    wry_dialog_prompt,
    wry_dialog_prompt_result_free,
};

// ============================================================================
// Re-exports: Menu (macOS)
// ============================================================================

pub use menu::{
    wry_menu_bar_new,
    wry_menu_bar_new_with_id,
    wry_menu_bar_free,
    wry_menu_bar_identifier,
    wry_menu_bar_append_submenu,
    wry_menu_bar_set_app_menu,
    wry_submenu_new,
    wry_submenu_new_with_id,
    wry_submenu_free,
    wry_submenu_identifier,
    wry_submenu_append_item,
    wry_submenu_append_separator,
    wry_submenu_append_check_item,
    wry_menu_item_new,
    wry_menu_item_free,
    wry_menu_item_set_enabled,
    wry_menu_item_is_enabled,
    wry_menu_item_text,
    wry_menu_item_set_text,
    wry_menu_item_set_accelerator,
    wry_menu_item_identifier,
    wry_separator_new,
    wry_separator_free,
    wry_separator_identifier,
    wry_check_menu_item_new,
    wry_check_menu_item_free,
    wry_check_menu_item_is_checked,
    wry_check_menu_item_set_checked,
    wry_check_menu_item_is_enabled,
    wry_check_menu_item_set_enabled,
    wry_check_menu_item_text,
    wry_check_menu_item_set_text,
    wry_check_menu_item_set_accelerator,
    wry_check_menu_item_identifier,
};

// ============================================================================
// Re-exports: Tray (macOS)
// ============================================================================

pub use tray::{
    wry_tray_new,
    wry_tray_free,
    wry_tray_identifier,
    wry_tray_set_title,
    wry_tray_set_tooltip,
    wry_tray_set_visible,
    wry_tray_set_show_menu_on_left_click,
    wry_tray_set_menu,
};
