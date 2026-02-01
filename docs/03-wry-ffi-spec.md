# wry-ffi Specification

This document specifies the Rust FFI crate that exposes Wry/Tao functionality to C (and thus to .NET via P/Invoke).

## Design Principles

1. **Flat C API** — No Rust types exposed, only C-compatible types
2. **Opaque handles** — Return pointers to Rust objects, caller treats as opaque
3. **Explicit memory management** — Caller responsible for calling destroy functions
4. **Error codes** — Return status codes, provide error message retrieval
5. **Callback-based async** — Use function pointers for events and async operations
6. **Thread safety** — Document thread requirements, provide thread-safe where possible

## Type Definitions

### Opaque Handles

```rust
/// Opaque handle to the application/event loop
pub type WryApp = *mut c_void;

/// Opaque handle to a window with webview
pub type WryWindow = *mut c_void;

/// Opaque handle to system tray
pub type WryTray = *mut c_void;
```

### Structs

```rust
#[repr(C)]
pub struct WryWindowParams {
    // Strings (UTF-8, null-terminated)
    pub title: *const c_char,
    pub url: *const c_char,              // Initial URL or NULL
    pub html: *const c_char,             // Initial HTML or NULL
    pub user_agent: *const c_char,       // Custom user agent or NULL
    pub data_directory: *const c_char,   // Webview data dir or NULL

    // Dimensions
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub min_width: u32,
    pub min_height: u32,
    pub max_width: u32,                  // 0 = no max
    pub max_height: u32,                 // 0 = no max

    // Flags
    pub resizable: bool,
    pub fullscreen: bool,
    pub maximized: bool,
    pub minimized: bool,
    pub visible: bool,
    pub transparent: bool,
    pub decorations: bool,               // Window chrome
    pub always_on_top: bool,
    pub devtools_enabled: bool,
    pub autoplay_enabled: bool,

    // Callbacks (set after creation via wry_window_set_*)
}

#[repr(C)]
pub struct WrySize {
    pub width: u32,
    pub height: u32,
}

#[repr(C)]
pub struct WryPosition {
    pub x: i32,
    pub y: i32,
}

#[repr(C)]
pub struct WryResult {
    pub success: bool,
    pub error_code: i32,
    pub error_message: *const c_char,    // Valid until next call
}
```

### Callback Types

```rust
/// Called when webview sends a message to backend
pub type WebMessageCallback = extern "C" fn(
    window: WryWindow,
    message: *const c_char,
    user_data: *mut c_void
);

/// Called when custom protocol request is made
/// Returns: pointer to response data, sets out params
pub type CustomProtocolCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    out_data: *mut *const u8,
    out_len: *mut usize,
    out_mime_type: *mut *const c_char,
    user_data: *mut c_void
) -> bool;

/// Called when window is closing (return false to prevent)
pub type WindowClosingCallback = extern "C" fn(
    window: WryWindow,
    user_data: *mut c_void
) -> bool;

/// Called when window is resized
pub type WindowResizedCallback = extern "C" fn(
    window: WryWindow,
    width: u32,
    height: u32,
    user_data: *mut c_void
);

/// Called when window is moved
pub type WindowMovedCallback = extern "C" fn(
    window: WryWindow,
    x: i32,
    y: i32,
    user_data: *mut c_void
);

/// Called when window focus changes
pub type WindowFocusCallback = extern "C" fn(
    window: WryWindow,
    focused: bool,
    user_data: *mut c_void
);

/// Called when navigation starts
pub type NavigationCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    user_data: *mut c_void
) -> bool;  // Return false to cancel navigation
```

## API Functions

### Application Lifecycle

```rust
/// Initialize the application. Must be called first, on main thread.
/// Returns: App handle or NULL on failure
#[no_mangle]
pub extern "C" fn wry_app_create() -> WryApp;

/// Run the event loop. Blocks until all windows closed or wry_app_quit called.
/// Must be called on main thread.
#[no_mangle]
pub extern "C" fn wry_app_run(app: WryApp) -> WryResult;

/// Request app to quit
#[no_mangle]
pub extern "C" fn wry_app_quit(app: WryApp);

/// Destroy app and free resources
#[no_mangle]
pub extern "C" fn wry_app_destroy(app: WryApp);
```

### Window Management

```rust
/// Create a new window with webview
#[no_mangle]
pub extern "C" fn wry_window_create(
    app: WryApp,
    params: *const WryWindowParams
) -> WryWindow;

/// Destroy window and free resources
#[no_mangle]
pub extern "C" fn wry_window_destroy(window: WryWindow);

/// Show/hide window
#[no_mangle]
pub extern "C" fn wry_window_set_visible(window: WryWindow, visible: bool);

/// Get window visibility
#[no_mangle]
pub extern "C" fn wry_window_is_visible(window: WryWindow) -> bool;

/// Set window title
#[no_mangle]
pub extern "C" fn wry_window_set_title(window: WryWindow, title: *const c_char);

/// Get window title (caller must free with wry_string_free)
#[no_mangle]
pub extern "C" fn wry_window_get_title(window: WryWindow) -> *mut c_char;

/// Set window size
#[no_mangle]
pub extern "C" fn wry_window_set_size(window: WryWindow, size: WrySize);

/// Get window size
#[no_mangle]
pub extern "C" fn wry_window_get_size(window: WryWindow) -> WrySize;

/// Set window position
#[no_mangle]
pub extern "C" fn wry_window_set_position(window: WryWindow, pos: WryPosition);

/// Get window position
#[no_mangle]
pub extern "C" fn wry_window_get_position(window: WryWindow) -> WryPosition;

/// Set window state
#[no_mangle]
pub extern "C" fn wry_window_minimize(window: WryWindow);

#[no_mangle]
pub extern "C" fn wry_window_maximize(window: WryWindow);

#[no_mangle]
pub extern "C" fn wry_window_unmaximize(window: WryWindow);

#[no_mangle]
pub extern "C" fn wry_window_set_fullscreen(window: WryWindow, fullscreen: bool);

#[no_mangle]
pub extern "C" fn wry_window_focus(window: WryWindow);

#[no_mangle]
pub extern "C" fn wry_window_close(window: WryWindow);
```

### Webview Operations

```rust
/// Navigate to URL
#[no_mangle]
pub extern "C" fn wry_webview_navigate(window: WryWindow, url: *const c_char) -> WryResult;

/// Load HTML content directly
#[no_mangle]
pub extern "C" fn wry_webview_load_html(window: WryWindow, html: *const c_char) -> WryResult;

/// Execute JavaScript in webview context
#[no_mangle]
pub extern "C" fn wry_webview_evaluate_script(
    window: WryWindow,
    script: *const c_char
) -> WryResult;

/// Send message to JavaScript (calls registered handler)
#[no_mangle]
pub extern "C" fn wry_webview_send_message(
    window: WryWindow,
    message: *const c_char
) -> WryResult;

/// Open devtools (if enabled)
#[no_mangle]
pub extern "C" fn wry_webview_open_devtools(window: WryWindow);

/// Close devtools
#[no_mangle]
pub extern "C" fn wry_webview_close_devtools(window: WryWindow);

/// Set zoom level (1.0 = 100%)
#[no_mangle]
pub extern "C" fn wry_webview_set_zoom(window: WryWindow, zoom: f64);

/// Get current URL (caller must free with wry_string_free)
#[no_mangle]
pub extern "C" fn wry_webview_get_url(window: WryWindow) -> *mut c_char;
```

### Custom Protocol Registration

```rust
/// Register custom protocol handler (e.g., "app" for app://...)
/// Must be called before window creation
#[no_mangle]
pub extern "C" fn wry_register_protocol(
    app: WryApp,
    scheme: *const c_char,
    callback: CustomProtocolCallback,
    user_data: *mut c_void
) -> WryResult;
```

### Event Callbacks

```rust
/// Set callback for web messages
#[no_mangle]
pub extern "C" fn wry_window_set_message_callback(
    window: WryWindow,
    callback: WebMessageCallback,
    user_data: *mut c_void
);

/// Set callback for window closing
#[no_mangle]
pub extern "C" fn wry_window_set_closing_callback(
    window: WryWindow,
    callback: WindowClosingCallback,
    user_data: *mut c_void
);

/// Set callback for window resize
#[no_mangle]
pub extern "C" fn wry_window_set_resized_callback(
    window: WryWindow,
    callback: WindowResizedCallback,
    user_data: *mut c_void
);

/// Set callback for window move
#[no_mangle]
pub extern "C" fn wry_window_set_moved_callback(
    window: WryWindow,
    callback: WindowMovedCallback,
    user_data: *mut c_void
);

/// Set callback for focus change
#[no_mangle]
pub extern "C" fn wry_window_set_focus_callback(
    window: WryWindow,
    callback: WindowFocusCallback,
    user_data: *mut c_void
);

/// Set callback for navigation (can cancel)
#[no_mangle]
pub extern "C" fn wry_window_set_navigation_callback(
    window: WryWindow,
    callback: NavigationCallback,
    user_data: *mut c_void
);
```

### System Tray (Phase 3+)

```rust
#[repr(C)]
pub struct WryTrayParams {
    pub icon_path: *const c_char,        // Path to icon file
    pub icon_data: *const u8,            // Or raw icon data
    pub icon_data_len: usize,
    pub tooltip: *const c_char,
}

#[no_mangle]
pub extern "C" fn wry_tray_create(
    app: WryApp,
    params: *const WryTrayParams
) -> WryTray;

#[no_mangle]
pub extern "C" fn wry_tray_destroy(tray: WryTray);

#[no_mangle]
pub extern "C" fn wry_tray_set_tooltip(tray: WryTray, tooltip: *const c_char);

// Menu building for tray - TBD
```

### Utility Functions

```rust
/// Free a string returned by wry_* functions
#[no_mangle]
pub extern "C" fn wry_string_free(s: *mut c_char);

/// Get last error message (valid until next wry_* call)
#[no_mangle]
pub extern "C" fn wry_get_last_error() -> *const c_char;

/// Get version string
#[no_mangle]
pub extern "C" fn wry_version() -> *const c_char;
```

## JavaScript Bridge

The FFI layer should inject a JavaScript bridge object into every webview:

```javascript
window.tauri = {
    // Send message to backend, returns promise for response
    invoke: function(command, payload) {
        return new Promise((resolve, reject) => {
            const id = window.__tauriNextId++;
            window.__tauriPending[id] = { resolve, reject };
            window.__tauriPostMessage(JSON.stringify({
                id: id,
                command: command,
                payload: payload
            }));
        });
    },

    // Listen for events from backend
    listen: function(event, callback) {
        if (!window.__tauriListeners[event]) {
            window.__tauriListeners[event] = [];
        }
        window.__tauriListeners[event].push(callback);
        return () => {
            window.__tauriListeners[event] =
                window.__tauriListeners[event].filter(cb => cb !== callback);
        };
    },

    // Internal: receive message from backend
    __receive: function(message) {
        const msg = JSON.parse(message);
        if (msg.responseId !== undefined) {
            // Response to invoke()
            const pending = window.__tauriPending[msg.responseId];
            if (pending) {
                delete window.__tauriPending[msg.responseId];
                if (msg.error) {
                    pending.reject(new Error(msg.error));
                } else {
                    pending.resolve(msg.payload);
                }
            }
        } else if (msg.event) {
            // Event from backend
            const listeners = window.__tauriListeners[msg.event] || [];
            listeners.forEach(cb => cb(msg.payload));
        }
    }
};

window.__tauriNextId = 1;
window.__tauriPending = {};
window.__tauriListeners = {};
```

## Build Outputs

```
wry-ffi/
├── Cargo.toml
├── src/
│   ├── lib.rs          # Main entry, re-exports
│   ├── app.rs          # WryApp implementation
│   ├── window.rs       # WryWindow implementation
│   ├── webview.rs      # Webview operations
│   ├── protocol.rs     # Custom protocol handling
│   ├── tray.rs         # System tray
│   ├── bridge.rs       # JavaScript bridge injection
│   └── error.rs        # Error handling
└── target/
    └── release/
        ├── wry_ffi.dll          # Windows
        ├── libwry_ffi.dylib     # macOS
        └── libwry_ffi.so        # Linux
```

## Thread Safety Notes

1. **wry_app_create** and **wry_app_run** must be called from the main thread
2. **Window operations** should be called from the thread that created the window
3. **Callbacks** will be invoked on the main/UI thread
4. **wry_webview_send_message** is thread-safe (queues to UI thread)

## Dialogs (Parity with Photino)

```rust
#[repr(C)]
pub struct WryFileFilter {
    pub name: *const c_char,       // e.g., "Images"
    pub pattern: *const c_char,    // e.g., "*.png;*.jpg"
}

#[repr(C)]
pub struct WryFileDialogResult {
    pub paths: *mut *mut c_char,   // Array of paths
    pub count: usize,              // Number of paths
}

/// Show file open dialog
#[no_mangle]
pub extern "C" fn wry_dialog_open_file(
    window: WryWindow,
    title: *const c_char,
    default_path: *const c_char,
    filters: *const WryFileFilter,
    filter_count: usize,
    multi_select: bool,
) -> WryFileDialogResult;

/// Show folder open dialog
#[no_mangle]
pub extern "C" fn wry_dialog_open_folder(
    window: WryWindow,
    title: *const c_char,
    default_path: *const c_char,
    multi_select: bool,
) -> WryFileDialogResult;

/// Show file save dialog
#[no_mangle]
pub extern "C" fn wry_dialog_save_file(
    window: WryWindow,
    title: *const c_char,
    default_path: *const c_char,
    filters: *const WryFileFilter,
    filter_count: usize,
) -> *mut c_char;  // Single path or NULL

/// Free file dialog result
#[no_mangle]
pub extern "C" fn wry_dialog_result_free(result: WryFileDialogResult);

#[repr(C)]
pub enum WryMessageBoxButtons {
    Ok = 0,
    OkCancel = 1,
    YesNo = 2,
    YesNoCancel = 3,
}

#[repr(C)]
pub enum WryMessageBoxIcon {
    Info = 0,
    Warning = 1,
    Error = 2,
    Question = 3,
}

#[repr(C)]
pub enum WryMessageBoxResult {
    Ok = 0,
    Cancel = 1,
    Yes = 2,
    No = 3,
}

/// Show message box dialog
#[no_mangle]
pub extern "C" fn wry_dialog_message(
    window: WryWindow,
    title: *const c_char,
    message: *const c_char,
    buttons: WryMessageBoxButtons,
    icon: WryMessageBoxIcon,
) -> WryMessageBoxResult;
```

## Notifications

```rust
/// Show system notification (toast on Windows, native on macOS/Linux)
#[no_mangle]
pub extern "C" fn wry_notification_show(
    app: WryApp,
    title: *const c_char,
    body: *const c_char,
    icon_path: *const c_char,  // Optional, can be NULL
) -> WryResult;
```

## Monitor/Display Information

```rust
#[repr(C)]
pub struct WryMonitor {
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub work_x: i32,           // Usable area (excluding taskbar, etc.)
    pub work_y: i32,
    pub work_width: u32,
    pub work_height: u32,
    pub scale_factor: f64,     // DPI scale (1.0 = 100%)
    pub is_primary: bool,
}

#[repr(C)]
pub struct WryMonitorList {
    pub monitors: *mut WryMonitor,
    pub count: usize,
}

/// Get all monitors
#[no_mangle]
pub extern "C" fn wry_get_monitors(app: WryApp) -> WryMonitorList;

/// Free monitor list
#[no_mangle]
pub extern "C" fn wry_monitors_free(list: WryMonitorList);

/// Get primary monitor
#[no_mangle]
pub extern "C" fn wry_get_primary_monitor(app: WryApp) -> WryMonitor;

/// Get DPI/scale factor for window's current monitor
#[no_mangle]
pub extern "C" fn wry_window_get_scale_factor(window: WryWindow) -> f64;
```

## Window Icon

```rust
/// Set window icon from file path
#[no_mangle]
pub extern "C" fn wry_window_set_icon_file(window: WryWindow, path: *const c_char) -> WryResult;

/// Set window icon from raw RGBA data
#[no_mangle]
pub extern "C" fn wry_window_set_icon_rgba(
    window: WryWindow,
    data: *const u8,
    width: u32,
    height: u32,
) -> WryResult;
```

## Thread Dispatch

```rust
/// Callback type for invoke
pub type InvokeCallback = extern "C" fn(user_data: *mut c_void);

/// Execute callback on UI thread (thread-safe, can be called from any thread)
#[no_mangle]
pub extern "C" fn wry_invoke(
    app: WryApp,
    callback: InvokeCallback,
    user_data: *mut c_void,
);

/// Execute callback on UI thread and wait for completion
#[no_mangle]
pub extern "C" fn wry_invoke_sync(
    app: WryApp,
    callback: InvokeCallback,
    user_data: *mut c_void,
);
```

## Additional Webview Features (from Wry)

### Initialization Scripts

```rust
/// Add JavaScript that runs on every page load (before page scripts)
/// Must be called before window creation or will apply to next navigation
#[no_mangle]
pub extern "C" fn wry_webview_add_init_script(
    window: WryWindow,
    script: *const c_char,
) -> WryResult;
```

### Additional Callbacks

```rust
/// Called when page starts loading
pub type PageLoadCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    user_data: *mut c_void
);

/// Called when document title changes
pub type TitleChangedCallback = extern "C" fn(
    window: WryWindow,
    title: *const c_char,
    user_data: *mut c_void
);

/// Called when download starts, return false to cancel
pub type DownloadStartedCallback = extern "C" fn(
    window: WryWindow,
    url: *const c_char,
    suggested_filename: *const c_char,
    user_data: *mut c_void
) -> bool;

/// Called when download completes
pub type DownloadCompletedCallback = extern "C" fn(
    window: WryWindow,
    path: *const c_char,
    success: bool,
    user_data: *mut c_void
);

/// Called on drag/drop events
#[repr(C)]
pub enum WryDragDropEvent {
    Enter = 0,
    Over = 1,
    Drop = 2,
    Leave = 3,
}

pub type DragDropCallback = extern "C" fn(
    window: WryWindow,
    event: WryDragDropEvent,
    paths: *const *const c_char,
    path_count: usize,
    x: i32,
    y: i32,
    user_data: *mut c_void
) -> bool;  // Return false to reject drop

#[no_mangle]
pub extern "C" fn wry_window_set_page_load_callback(
    window: WryWindow,
    callback: PageLoadCallback,
    user_data: *mut c_void
);

#[no_mangle]
pub extern "C" fn wry_window_set_title_changed_callback(
    window: WryWindow,
    callback: TitleChangedCallback,
    user_data: *mut c_void
);

#[no_mangle]
pub extern "C" fn wry_window_set_download_started_callback(
    window: WryWindow,
    callback: DownloadStartedCallback,
    user_data: *mut c_void
);

#[no_mangle]
pub extern "C" fn wry_window_set_download_completed_callback(
    window: WryWindow,
    callback: DownloadCompletedCallback,
    user_data: *mut c_void
);

#[no_mangle]
pub extern "C" fn wry_window_set_drag_drop_callback(
    window: WryWindow,
    callback: DragDropCallback,
    user_data: *mut c_void
);
```

### Browser Settings

```rust
/// Enable/disable context menu (right-click menu)
#[no_mangle]
pub extern "C" fn wry_webview_set_context_menu_enabled(window: WryWindow, enabled: bool);

/// Enable/disable clipboard access from JavaScript
#[no_mangle]
pub extern "C" fn wry_webview_set_clipboard_enabled(window: WryWindow, enabled: bool);

/// Clear browser data (cookies, cache, storage)
#[no_mangle]
pub extern "C" fn wry_webview_clear_data(window: WryWindow) -> WryResult;

/// Set incognito/private mode (must be set before window creation)
/// Note: This is set in WryWindowParams, cannot be changed after creation
```

## Platform Capabilities

```rust
#[repr(C)]
pub struct WryCapabilities {
    pub has_system_tray: bool,
    pub has_notifications: bool,
    pub has_transparent_windows: bool,
    pub has_drag_drop: bool,
    pub has_devtools: bool,
    pub webview_version: *const c_char,  // e.g., "WebView2 119.0" or "WebKit 605.1"
    pub platform: *const c_char,         // "windows", "macos", "linux"
}

/// Query platform capabilities
#[no_mangle]
pub extern "C" fn wry_get_capabilities() -> WryCapabilities;
```

## Extended WryWindowParams

The `WryWindowParams` struct should be extended:

```rust
#[repr(C)]
pub struct WryWindowParams {
    // ... existing fields ...

    // Additional fields:
    pub icon_path: *const c_char,        // Window icon file path
    pub incognito: bool,                 // Private/incognito mode
    pub context_menu_enabled: bool,      // Enable right-click menu
    pub clipboard_enabled: bool,         // Allow JS clipboard access
    pub hotkeys_zoom_enabled: bool,      // Ctrl+/- for zoom
    pub accept_first_mouse: bool,        // macOS: click-through when unfocused
    pub focused: bool,                   // Start with focus

    // Background color (alternative to transparent)
    pub background_r: u8,
    pub background_g: u8,
    pub background_b: u8,
    pub background_a: u8,                // 0 = use default, 255 = opaque
}
```

## Error Handling

Every function that can fail returns `WryResult` or provides error via `wry_get_last_error()`:

```rust
#[repr(C)]
pub enum WryErrorCode {
    Success = 0,
    InvalidHandle = 1,
    WindowCreationFailed = 2,
    WebviewCreationFailed = 3,
    NavigationFailed = 4,
    ScriptError = 5,
    ProtocolError = 6,
    InvalidParameter = 7,
    NotSupported = 8,
    DialogCancelled = 9,
    NotificationFailed = 10,
    IconLoadFailed = 11,
    Unknown = 255,
}
```

## Implementation Priority

### Phase 2 (Core - Must Have)
- Application lifecycle
- Window create/destroy/basic operations
- Webview navigate/load_html/evaluate_script/send_message
- Custom protocol registration
- Basic callbacks (message, closing, resize, move, focus)
- JavaScript bridge injection

### Phase 3 (Extended Features)
- System tray
- Native menus
- Dialogs (file/folder/message)
- Monitor information
- Window icon
- Thread dispatch (invoke)

### Phase 4 (Advanced)
- Notifications
- Drag/drop handling
- Download handlers
- Additional webview settings
- Platform capabilities API
