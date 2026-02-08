//! Window creation and management FFI functions
//!
//! Functions for creating windows and manipulating their properties.

use std::ffi::CString;
use std::os::raw::c_char;
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::ptr;

use tao::dpi::{LogicalPosition, LogicalSize, Size};
use tao::window::{Fullscreen, Icon, WindowBuilder as TaoWindowBuilder};

use crate::helpers::*;
use crate::types::*;

// ============================================================================
// Window Creation/Destruction
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_build(
    event_loop: *mut WryEventLoop,
    config: *const WryWindowConfig,
) -> *mut WryWindowHandle {
    if event_loop.is_null() {
        return ptr::null_mut();
    }

    let event_loop = unsafe { &mut *event_loop };
    let cfg = unsafe { config.as_ref().copied().unwrap_or_default() };

    let build_result = catch_unwind(AssertUnwindSafe(|| {
        let mut builder = TaoWindowBuilder::new();

        if let Some(title) = opt_cstring(cfg.title) {
            builder = builder.with_title(title);
        }

        if cfg.width > 0 && cfg.height > 0 {
            builder =
                builder.with_inner_size(LogicalSize::new(cfg.width as f64, cfg.height as f64));
        }

        builder.build(&event_loop.event_loop)
    }));

    match build_result {
        Ok(Ok(window)) => {
            let id_string = format!("{:?}", window.id());
            let identifier = CString::new(id_string).unwrap_or_else(|_| {
                CString::new("wry-window").expect("static string has no nulls")
            });
            Box::into_raw(Box::new(WryWindowHandle { window, identifier }))
        }
        _ => ptr::null_mut(),
    }
}

#[no_mangle]
pub extern "C" fn wry_window_free(window: *mut WryWindowHandle) {
    if !window.is_null() {
        unsafe { drop(Box::from_raw(window)) };
    }
}

#[no_mangle]
pub extern "C" fn wry_window_identifier(window: *mut WryWindowHandle) -> *const c_char {
    if window.is_null() {
        return ptr::null();
    }

    unsafe { &*window }.identifier.as_ptr()
}

// ============================================================================
// Window Properties - Setters
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_set_title(
    window: *mut WryWindowHandle,
    title: *const c_char,
) -> bool {
    let Some(title) = opt_cstring(title) else {
        return false;
    };
    with_window(window, |w| {
        w.set_title(&title);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_fullscreen(
    window: *mut WryWindowHandle,
    fullscreen: bool,
) -> bool {
    with_window(window, |w| {
        if fullscreen {
            w.set_fullscreen(Some(Fullscreen::Borderless(None)));
        } else {
            w.set_fullscreen(None);
        }
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_decorations(
    window: *mut WryWindowHandle,
    decorations: bool,
) -> bool {
    with_window(window, |w| {
        w.set_decorations(decorations);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_always_on_bottom(
    window: *mut WryWindowHandle,
    on_bottom: bool,
) -> bool {
    with_window(window, |w| {
        w.set_always_on_bottom(on_bottom);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_visible_on_all_workspaces(
    window: *mut WryWindowHandle,
    visible_on_all_workspaces: bool,
) -> bool {
    with_window(window, |w| {
        w.set_visible_on_all_workspaces(visible_on_all_workspaces);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_content_protected(
    window: *mut WryWindowHandle,
    protected: bool,
) -> bool {
    with_window(window, |w| {
        w.set_content_protection(protected);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_resizable(
    window: *mut WryWindowHandle,
    resizable: bool,
) -> bool {
    with_window(window, |w| {
        w.set_resizable(resizable);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_always_on_top(
    window: *mut WryWindowHandle,
    on_top: bool,
) -> bool {
    with_window(window, |w| {
        w.set_always_on_top(on_top);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_visible(window: *mut WryWindowHandle, visible: bool) -> bool {
    with_window(window, |w| {
        let _ = w.set_visible(visible);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_maximized(
    window: *mut WryWindowHandle,
    maximized: bool,
) -> bool {
    with_window(window, |w| {
        w.set_maximized(maximized);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_minimized(
    window: *mut WryWindowHandle,
    minimized: bool,
) -> bool {
    with_window(window, |w| {
        w.set_minimized(minimized);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_skip_taskbar(
    window: *mut WryWindowHandle,
    skip: bool,
) -> bool {
    with_window(window, |w| {
        #[cfg(target_os = "windows")]
        {
            use tao::platform::windows::WindowExtWindows;
            return w.set_skip_taskbar(skip).is_ok();
        }

        #[cfg(any(
            target_os = "linux",
            target_os = "dragonfly",
            target_os = "freebsd",
            target_os = "netbsd",
            target_os = "openbsd"
        ))]
        {
            use tao::platform::unix::WindowExtUnix;
            return w.set_skip_taskbar(skip).is_ok();
        }

        #[cfg(not(any(
            target_os = "windows",
            target_os = "linux",
            target_os = "dragonfly",
            target_os = "freebsd",
            target_os = "netbsd",
            target_os = "openbsd"
        )))]
        {
            let _ = (w, skip);
            return false;
        }
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_minimizable(
    window: *mut WryWindowHandle,
    minimizable: bool,
) -> bool {
    with_window(window, |w| {
        w.set_minimizable(minimizable);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_maximizable(
    window: *mut WryWindowHandle,
    maximizable: bool,
) -> bool {
    with_window(window, |w| {
        w.set_maximizable(maximizable);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_closable(
    window: *mut WryWindowHandle,
    closable: bool,
) -> bool {
    with_window(window, |w| {
        w.set_closable(closable);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_background_color(
    window: *mut WryWindowHandle,
    color: *const WryColor,
) -> bool {
    let color = opt_color(color);
    with_window(window, |w| {
        w.set_background_color(color);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_theme(
    window: *mut WryWindowHandle,
    theme: WryWindowTheme,
) -> bool {
    let theme = theme_from_ffi(theme);
    with_window(window, |w| {
        w.set_theme(theme);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_focusable(
    window: *mut WryWindowHandle,
    focusable: bool,
) -> bool {
    with_window(window, |w| {
        w.set_focusable(focusable);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_size(
    window: *mut WryWindowHandle,
    width: f64,
    height: f64,
) -> bool {
    with_window(window, |w| {
        w.set_inner_size(LogicalSize::new(width, height));
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_position(
    window: *mut WryWindowHandle,
    x: f64,
    y: f64,
) -> bool {
    with_window(window, |w| {
        w.set_outer_position(LogicalPosition::new(x, y));
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_min_size(
    window: *mut WryWindowHandle,
    width: f64,
    height: f64,
) -> bool {
    with_window(window, |w| {
        let size: Option<Size> = if width > 0.0 && height > 0.0 {
            Some(Size::Logical(LogicalSize::new(width, height)))
        } else {
            None
        };
        w.set_min_inner_size(size);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_max_size(
    window: *mut WryWindowHandle,
    width: f64,
    height: f64,
) -> bool {
    with_window(window, |w| {
        let size: Option<Size> = if width > 0.0 && height > 0.0 {
            Some(Size::Logical(LogicalSize::new(width, height)))
        } else {
            None
        };
        w.set_max_inner_size(size);
        true
    })
    .unwrap_or(false)
}

// ============================================================================
// Window Properties - Getters
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_is_maximized(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_maximized()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_minimized(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_minimized()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_visible(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_visible()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_resizable(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_resizable()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_decorated(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_decorated()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_always_on_top(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_always_on_top()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_minimizable(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_minimizable()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_maximizable(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_maximizable()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_closable(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_closable()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_fullscreen(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.fullscreen().is_some()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_is_focused(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.is_focused()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_scale_factor(
    window: *mut WryWindowHandle,
    scale_factor: *mut f64,
) -> bool {
    if scale_factor.is_null() {
        return false;
    }

    with_window(window, |w| {
        unsafe {
            *scale_factor = w.scale_factor();
        }
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_inner_position(
    window: *mut WryWindowHandle,
    position: *mut WryPoint,
) -> bool {
    if position.is_null() {
        return false;
    }

    with_window(window, |w| match w.inner_position() {
        Ok(pos) => {
            write_position(position, pos.to_logical(w.scale_factor()));
            true
        }
        Err(_) => false,
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_outer_position(
    window: *mut WryWindowHandle,
    position: *mut WryPoint,
) -> bool {
    if position.is_null() {
        return false;
    }

    with_window(window, |w| match w.outer_position() {
        Ok(pos) => {
            write_position(position, pos.to_logical(w.scale_factor()));
            true
        }
        Err(_) => false,
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_inner_size(
    window: *mut WryWindowHandle,
    size: *mut WrySize,
) -> bool {
    if size.is_null() {
        return false;
    }

    with_window(window, |w| {
        let inner = w.inner_size().to_logical::<f64>(w.scale_factor());
        write_size(size, inner);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_outer_size(
    window: *mut WryWindowHandle,
    size: *mut WrySize,
) -> bool {
    if size.is_null() {
        return false;
    }

    with_window(window, |w| {
        let outer = w.outer_size().to_logical::<f64>(w.scale_factor());
        write_size(size, outer);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_title(window: *mut WryWindowHandle) -> *const c_char {
    with_window(window, |w| {
        let title = w.title();
        write_string_to_buffer(&TITLE_BUFFER, title)
    })
    .unwrap_or(ptr::null())
}

#[no_mangle]
pub extern "C" fn wry_window_cursor_position(
    window: *mut WryWindowHandle,
    point: *mut WryPoint,
) -> bool {
    if point.is_null() {
        return false;
    }

    with_window(window, |w| match w.cursor_position() {
        Ok(position) => {
            unsafe {
                (*point).x = position.x;
                (*point).y = position.y;
            }
            true
        }
        Err(_) => false,
    })
    .unwrap_or(false)
}

// ============================================================================
// Monitor Information
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_current_monitor(window: *mut WryWindowHandle) -> *const c_char {
    with_window(window, |w| {
        if let Some(monitor) = w.current_monitor() {
            write_json_to_buffer(&MONITOR_BUFFER, monitor_to_json(&monitor))
        } else {
            ptr::null()
        }
    })
    .unwrap_or(ptr::null())
}

#[no_mangle]
pub extern "C" fn wry_window_primary_monitor(window: *mut WryWindowHandle) -> *const c_char {
    with_window(window, |w| {
        if let Some(monitor) = w.primary_monitor() {
            write_json_to_buffer(&MONITOR_BUFFER, monitor_to_json(&monitor))
        } else {
            ptr::null()
        }
    })
    .unwrap_or(ptr::null())
}

#[no_mangle]
pub extern "C" fn wry_window_available_monitors(window: *mut WryWindowHandle) -> *const c_char {
    with_window(window, |w| {
        let monitors: Vec<_> = w
            .available_monitors()
            .map(|monitor| monitor_to_json(&monitor))
            .collect();
        write_json_to_buffer(&MONITOR_LIST_BUFFER, serde_json::Value::Array(monitors))
    })
    .unwrap_or(ptr::null())
}

#[no_mangle]
pub extern "C" fn wry_window_monitor_from_point(
    window: *mut WryWindowHandle,
    point: WryPoint,
) -> *const c_char {
    with_window(window, |w| {
        if let Some(monitor) = w.monitor_from_point(point.x, point.y) {
            write_json_to_buffer(&MONITOR_BUFFER, monitor_to_json(&monitor))
        } else {
            ptr::null()
        }
    })
    .unwrap_or(ptr::null())
}

// ============================================================================
// Window Actions
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_focus(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| {
        w.set_focus();
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_request_redraw(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| {
        w.request_redraw();
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_request_user_attention(
    window: *mut WryWindowHandle,
    attention_type: WryUserAttentionType,
) -> bool {
    let attention = tao_user_attention_from_ffi(attention_type);
    with_window(window, |w| {
        w.request_user_attention(Some(attention));
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_clear_user_attention(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| {
        w.request_user_attention(None);
        true
    })
    .unwrap_or(false)
}

// ============================================================================
// Cursor Operations
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_set_cursor_grab(window: *mut WryWindowHandle, grab: bool) -> bool {
    with_window(window, |w| w.set_cursor_grab(grab).is_ok()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_cursor_visible(
    window: *mut WryWindowHandle,
    visible: bool,
) -> bool {
    with_window(window, |w| {
        w.set_cursor_visible(visible);
        true
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_cursor_position(
    window: *mut WryWindowHandle,
    x: f64,
    y: f64,
) -> bool {
    with_window(window, |w| {
        w.set_cursor_position(LogicalPosition::new(x, y)).is_ok()
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_set_ignore_cursor_events(
    window: *mut WryWindowHandle,
    ignore: bool,
) -> bool {
    with_window(window, |w| w.set_ignore_cursor_events(ignore).is_ok()).unwrap_or(false)
}

// ============================================================================
// Drag Operations
// ============================================================================

#[no_mangle]
pub extern "C" fn wry_window_start_dragging(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| w.drag_window().is_ok()).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn wry_window_start_resize_dragging(
    window: *mut WryWindowHandle,
    direction: WryResizeDirection,
) -> bool {
    let tao_direction = tao_resize_direction_from_ffi(direction);
    with_window(window, |w| w.drag_resize_window(tao_direction).is_ok()).unwrap_or(false)
}

// ============================================================================
// Window Icon
// ============================================================================

/// Set window icon from raw RGBA pixel data.
#[no_mangle]
pub extern "C" fn wry_window_set_icon_rgba(
    window: *mut WryWindowHandle,
    rgba_data: *const u8,
    rgba_len: usize,
    width: u32,
    height: u32,
) -> bool {
    if rgba_data.is_null() || rgba_len == 0 {
        return false;
    }

    let data = unsafe { std::slice::from_raw_parts(rgba_data, rgba_len) };

    guard_panic_bool(|| {
        let icon = Icon::from_rgba(data.to_vec(), width, height)
            .map_err(|e| log::error!("Failed to create icon from RGBA: {e}"))
            .ok();

        with_window(window, |w| {
            w.set_window_icon(icon);
            true
        })
        .unwrap_or(false)
    })
}

/// Set window icon from an image file (PNG, ICO, JPEG).
#[no_mangle]
pub extern "C" fn wry_window_set_icon_file(
    window: *mut WryWindowHandle,
    path: *const c_char,
) -> bool {
    let Some(path_str) = opt_cstring(path) else {
        return false;
    };

    guard_panic_bool(|| {
        let img = image::open(&path_str)
            .map_err(|e| log::error!("Failed to load icon file '{}': {e}", path_str))
            .ok();
        let Some(img) = img else {
            return false;
        };

        let rgba = img.into_rgba8();
        let (w, h) = rgba.dimensions();
        let icon = Icon::from_rgba(rgba.into_raw(), w, h)
            .map_err(|e| log::error!("Failed to create icon: {e}"))
            .ok();

        with_window(window, |win| {
            win.set_window_icon(icon);
            true
        })
        .unwrap_or(false)
    })
}

/// Clear the window icon.
#[no_mangle]
pub extern "C" fn wry_window_clear_icon(window: *mut WryWindowHandle) -> bool {
    with_window(window, |w| {
        w.set_window_icon(None);
        true
    })
    .unwrap_or(false)
}
