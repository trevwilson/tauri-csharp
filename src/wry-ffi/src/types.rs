//! FFI type definitions for wry-ffi
//!
//! All types are C-compatible and use explicit memory layout.
//! Based on Velox's runtime-wry-ffi types with Wry prefix.

use std::ffi::c_char;
use std::os::raw::c_void;
use std::ptr;

// ============================================================================
// Handle Types
// ============================================================================

/// Opaque handle to the event loop
pub struct WryEventLoop {
    pub event_loop: tao::event_loop::EventLoop<WryUserEvent>,
}

/// Opaque handle to the event loop proxy
pub struct WryEventLoopProxyHandle {
    pub proxy: tao::event_loop::EventLoopProxy<WryUserEvent>,
}

/// Opaque handle to a window
pub struct WryWindowHandle {
    pub window: tao::window::Window,
    pub identifier: std::ffi::CString,
}

/// Opaque handle to a webview
pub struct WryWebviewHandle {
    pub webview: wry::WebView,
}

// ============================================================================
// User Events
// ============================================================================

#[derive(Debug, Clone)]
pub enum WryUserEvent {
    Exit,
    Custom(String),
    #[cfg(target_os = "macos")]
    Menu(String),
    #[cfg(target_os = "macos")]
    Tray(WryTrayEvent),
}

// ============================================================================
// Basic Types
// ============================================================================

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct WryColor {
    pub red: u8,
    pub green: u8,
    pub blue: u8,
    pub alpha: u8,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct WryPoint {
    pub x: f64,
    pub y: f64,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct WrySize {
    pub width: f64,
    pub height: f64,
}

// ============================================================================
// Enums
// ============================================================================

#[repr(C)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum WryWindowTheme {
    Unspecified = 0,
    Light = 1,
    Dark = 2,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum WryActivationPolicy {
    Regular = 0,
    Accessory = 1,
    Prohibited = 2,
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum WryEventLoopControlFlow {
    Poll = 0,
    Wait = 1,
    Exit = 2,
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum WryUserAttentionType {
    Informational = 0,
    Critical = 1,
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum WryResizeDirection {
    East = 0,
    North = 1,
    NorthEast = 2,
    NorthWest = 3,
    South = 4,
    SouthEast = 5,
    SouthWest = 6,
    West = 7,
}

// ============================================================================
// Config Structs
// ============================================================================

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryWindowConfig {
    pub width: u32,
    pub height: u32,
    pub title: *const c_char,
}

/// Callback for IPC messages from JavaScript
pub type WryIpcHandler = Option<
    unsafe extern "C" fn(
        url: *const c_char,
        message: *const c_char,
        user_data: *mut c_void,
    ),
>;

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct WryWebviewConfig {
    pub url: *const c_char,
    pub custom_protocols: WryCustomProtocolList,
    pub devtools: bool,
    /// If true, create as a child webview with bounds
    pub is_child: bool,
    /// X position for child webview (logical pixels)
    pub x: f64,
    /// Y position for child webview (logical pixels)
    pub y: f64,
    /// Width for child webview (logical pixels)
    pub width: f64,
    /// Height for child webview (logical pixels)
    pub height: f64,
    /// IPC handler callback
    pub ipc_handler: WryIpcHandler,
    /// User data for IPC handler
    pub ipc_user_data: *mut c_void,
}

impl Default for WryWebviewConfig {
    fn default() -> Self {
        Self {
            url: ptr::null(),
            custom_protocols: WryCustomProtocolList {
                protocols: ptr::null(),
                count: 0,
            },
            devtools: cfg!(debug_assertions),
            is_child: false,
            x: 0.0,
            y: 0.0,
            width: 0.0,
            height: 0.0,
            ipc_handler: None,
            ipc_user_data: ptr::null_mut(),
        }
    }
}

// ============================================================================
// Custom Protocol Types
// ============================================================================

pub type WryCustomProtocolHandler = Option<
    unsafe extern "C" fn(
        request: *const WryCustomProtocolRequest,
        response: *mut WryCustomProtocolResponse,
        user_data: *mut c_void,
    ) -> bool,
>;

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolDefinition {
    pub scheme: *const c_char,
    pub handler: WryCustomProtocolHandler,
    pub user_data: *mut c_void,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolList {
    pub protocols: *const WryCustomProtocolDefinition,
    pub count: usize,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolHeader {
    pub name: *const c_char,
    pub value: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolHeaderList {
    pub headers: *const WryCustomProtocolHeader,
    pub count: usize,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolBuffer {
    pub ptr: *const u8,
    pub len: usize,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolRequest {
    pub url: *const c_char,
    pub method: *const c_char,
    pub headers: WryCustomProtocolHeaderList,
    pub body: WryCustomProtocolBuffer,
    pub webview_id: *const c_char,
}

pub type WryCustomProtocolResponseFree = Option<unsafe extern "C" fn(user_data: *mut c_void)>;

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryCustomProtocolResponse {
    pub status: u16,
    pub headers: WryCustomProtocolHeaderList,
    pub body: WryCustomProtocolBuffer,
    pub mime_type: *const c_char,
    pub free: WryCustomProtocolResponseFree,
    pub user_data: *mut c_void,
}

// ============================================================================
// Dialog Types
// ============================================================================

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryDialogFilter {
    pub label: *const c_char,
    pub extensions: *const *const c_char,
    pub extension_count: usize,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryDialogOpenOptions {
    pub title: *const c_char,
    pub default_path: *const c_char,
    pub filters: *const WryDialogFilter,
    pub filter_count: usize,
    pub allow_directories: bool,
    pub allow_multiple: bool,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryDialogSaveOptions {
    pub title: *const c_char,
    pub default_path: *const c_char,
    pub default_name: *const c_char,
    pub filters: *const WryDialogFilter,
    pub filter_count: usize,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryDialogSelection {
    pub paths: *mut *mut c_char,
    pub count: usize,
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum WryMessageDialogLevel {
    Info = 0,
    Warning = 1,
    Error = 2,
}

impl Default for WryMessageDialogLevel {
    fn default() -> Self {
        Self::Info
    }
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum WryMessageDialogButtons {
    Ok = 0,
    OkCancel = 1,
    YesNo = 2,
    YesNoCancel = 3,
}

impl Default for WryMessageDialogButtons {
    fn default() -> Self {
        Self::Ok
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryMessageDialogOptions {
    pub title: *const c_char,
    pub message: *const c_char,
    pub level: WryMessageDialogLevel,
    pub buttons: WryMessageDialogButtons,
    pub ok_label: *const c_char,
    pub cancel_label: *const c_char,
    pub yes_label: *const c_char,
    pub no_label: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryConfirmDialogOptions {
    pub title: *const c_char,
    pub message: *const c_char,
    pub level: WryMessageDialogLevel,
    pub ok_label: *const c_char,
    pub cancel_label: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryAskDialogOptions {
    pub title: *const c_char,
    pub message: *const c_char,
    pub level: WryMessageDialogLevel,
    pub yes_label: *const c_char,
    pub no_label: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryPromptDialogOptions {
    pub title: *const c_char,
    pub message: *const c_char,
    pub placeholder: *const c_char,
    pub default_value: *const c_char,
    pub ok_label: *const c_char,
    pub cancel_label: *const c_char,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct WryPromptDialogResult {
    pub value: *mut c_char,
    pub accepted: bool,
}

// ============================================================================
// Tray Types
// ============================================================================

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct WryTrayConfig {
    pub identifier: *const c_char,
    pub title: *const c_char,
    pub tooltip: *const c_char,
    pub visible: bool,
    pub show_menu_on_left_click: bool,
}

impl Default for WryTrayConfig {
    fn default() -> Self {
        Self {
            identifier: ptr::null(),
            title: ptr::null(),
            tooltip: ptr::null(),
            visible: true,
            show_menu_on_left_click: true,
        }
    }
}

#[cfg(target_os = "macos")]
#[derive(Debug, Clone)]
pub struct WryTrayEvent {
    pub identifier: String,
    pub kind: WryTrayEventKind,
    pub position: Option<(f64, f64)>,
    pub rect: Option<WryTrayRect>,
    pub button: Option<String>,
    pub button_state: Option<String>,
}

#[cfg(target_os = "macos")]
#[derive(Debug, Clone, Copy)]
pub enum WryTrayEventKind {
    Click,
    DoubleClick,
    Enter,
    Move,
    Leave,
}

#[cfg(target_os = "macos")]
#[derive(Debug, Clone, Copy)]
pub struct WryTrayRect {
    pub origin_x: f64,
    pub origin_y: f64,
    pub width: f64,
    pub height: f64,
}

#[cfg(target_os = "macos")]
impl From<tray_icon::Rect> for WryTrayRect {
    fn from(rect: tray_icon::Rect) -> Self {
        Self {
            origin_x: rect.position.x,
            origin_y: rect.position.y,
            width: rect.size.width as f64,
            height: rect.size.height as f64,
        }
    }
}

#[cfg(target_os = "macos")]
impl From<tray_icon::TrayIconEvent> for WryTrayEvent {
    fn from(event: tray_icon::TrayIconEvent) -> Self {
        use tray_icon::TrayIconEvent;
        match event {
            TrayIconEvent::Click {
                id,
                position,
                rect,
                button,
                button_state,
            } => Self {
                identifier: id.as_ref().to_string(),
                kind: WryTrayEventKind::Click,
                position: Some((position.x, position.y)),
                rect: Some(rect.into()),
                button: Some(match button {
                    tray_icon::MouseButton::Left => "left".to_string(),
                    tray_icon::MouseButton::Right => "right".to_string(),
                    tray_icon::MouseButton::Middle => "middle".to_string(),
                }),
                button_state: Some(match button_state {
                    tray_icon::MouseButtonState::Up => "up".to_string(),
                    tray_icon::MouseButtonState::Down => "down".to_string(),
                }),
            },
            TrayIconEvent::DoubleClick {
                id,
                position,
                rect,
                button,
            } => Self {
                identifier: id.as_ref().to_string(),
                kind: WryTrayEventKind::DoubleClick,
                position: Some((position.x, position.y)),
                rect: Some(rect.into()),
                button: Some(match button {
                    tray_icon::MouseButton::Left => "left".to_string(),
                    tray_icon::MouseButton::Right => "right".to_string(),
                    tray_icon::MouseButton::Middle => "middle".to_string(),
                }),
                button_state: None,
            },
            TrayIconEvent::Enter { id, position, rect } => Self {
                identifier: id.as_ref().to_string(),
                kind: WryTrayEventKind::Enter,
                position: Some((position.x, position.y)),
                rect: Some(rect.into()),
                button: None,
                button_state: None,
            },
            TrayIconEvent::Move { id, position, rect } => Self {
                identifier: id.as_ref().to_string(),
                kind: WryTrayEventKind::Move,
                position: Some((position.x, position.y)),
                rect: Some(rect.into()),
                button: None,
                button_state: None,
            },
            TrayIconEvent::Leave { id, position, rect } => Self {
                identifier: id.as_ref().to_string(),
                kind: WryTrayEventKind::Leave,
                position: Some((position.x, position.y)),
                rect: Some(rect.into()),
                button: None,
                button_state: None,
            },
            other => Self {
                identifier: other.id().as_ref().to_string(),
                kind: WryTrayEventKind::Move,
                position: None,
                rect: None,
                button: None,
                button_state: None,
            },
        }
    }
}

// ============================================================================
// Menu Handle Types (platform-specific)
// ============================================================================

#[cfg(target_os = "macos")]
pub struct WryMenuBarHandle {
    pub menu: muda::Menu,
    pub submenus: Vec<std::rc::Rc<std::cell::RefCell<muda::Submenu>>>,
    pub identifier: std::ffi::CString,
}

#[cfg(target_os = "macos")]
pub struct WrySubmenuHandle {
    pub submenu: std::rc::Rc<std::cell::RefCell<muda::Submenu>>,
    pub identifier: std::ffi::CString,
    pub items: Vec<muda::MenuItem>,
}

#[cfg(target_os = "macos")]
pub struct WryMenuItemHandle {
    pub item: muda::MenuItem,
    pub identifier: std::ffi::CString,
}

#[cfg(target_os = "macos")]
pub struct WryCheckMenuItemHandle {
    pub item: muda::CheckMenuItem,
    pub identifier: std::ffi::CString,
}

#[cfg(target_os = "macos")]
pub struct WrySeparatorHandle {
    pub item: muda::PredefinedMenuItem,
    pub identifier: std::ffi::CString,
}

#[cfg(not(target_os = "macos"))]
pub struct WryMenuBarHandle {
    _private: (),
}

#[cfg(not(target_os = "macos"))]
pub struct WrySubmenuHandle {
    _private: (),
}

#[cfg(not(target_os = "macos"))]
pub struct WryMenuItemHandle {
    _private: (),
}

#[cfg(not(target_os = "macos"))]
pub struct WryCheckMenuItemHandle {
    _private: (),
}

#[cfg(not(target_os = "macos"))]
pub struct WrySeparatorHandle {
    _private: (),
}

// ============================================================================
// Tray Handle Types (platform-specific)
// ============================================================================

#[cfg(target_os = "macos")]
pub struct WryTrayHandle {
    pub tray: tray_icon::TrayIcon,
    pub menu: Option<tray_icon::menu::Menu>,
    pub identifier: std::ffi::CString,
}

#[cfg(not(target_os = "macos"))]
pub struct WryTrayHandle {
    _private: (),
}

// ============================================================================
// Callback Types
// ============================================================================

pub type WryEventLoopCallback = Option<
    extern "C" fn(
        event_description: *const c_char,
        user_data: *mut c_void,
    ) -> WryEventLoopControlFlow,
>;
