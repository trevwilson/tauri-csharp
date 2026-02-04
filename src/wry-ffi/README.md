# wry-ffi

C FFI bindings for the Wry WebView library, designed for P/Invoke from .NET.

Based on [Velox's runtime-wry-ffi](https://github.com/nicholasgrose/velox) with adaptations for TauriCSharp.

## Building

```bash
# From repo root
./scripts/build-wry-ffi.sh

# Or manually
cd src/wry-ffi
cargo build --release
```

This produces:
- Linux: `target/release/libwry_ffi.so`
- macOS: `target/release/libwry_ffi.dylib`
- Windows: `target/release/wry_ffi.dll`

### Linux Dependencies

Install WebKitGTK development libraries:

```bash
# Ubuntu/Debian
sudo apt install libwebkit2gtk-4.1-dev libgtk-3-dev

# Fedora
sudo dnf install gtk3-devel webkit2gtk4.1-devel

# Arch
sudo pacman -S webkit2gtk-4.1
```

## API Overview

### Event Loop

```c
// Create event loop (must be on main thread)
WryEventLoopHandle wry_event_loop_new();

// Create proxy for cross-thread communication
WryEventLoopProxyHandle wry_event_loop_create_proxy(WryEventLoopHandle);

// Pump events with callback (non-blocking iteration)
void wry_event_loop_pump(WryEventLoopHandle, callback, user_data);

// Free resources
void wry_event_loop_free(WryEventLoopHandle);
void wry_event_loop_proxy_free(WryEventLoopProxyHandle);
```

### Window Management

```c
// Create window from config struct
WryWindowHandle wry_window_build(WryEventLoopHandle, WryWindowConfig*);

// Window properties
void wry_window_set_title(WryWindowHandle, char*);
void wry_window_set_visible(WryWindowHandle, bool);
void wry_window_set_size(WryWindowHandle, uint32_t width, uint32_t height);
void wry_window_set_position(WryWindowHandle, int32_t x, int32_t y);
void wry_window_set_minimized(WryWindowHandle, bool);
void wry_window_set_maximized(WryWindowHandle, bool);

// Cleanup
void wry_window_free(WryWindowHandle);
```

### Webview Operations

```c
// Build webview attached to window
WryWebviewHandle wry_webview_build(WryWindowHandle, WryWebviewConfig*);

// Navigation
bool wry_webview_navigate(WryWebviewHandle, char* url);
bool wry_webview_load_html(WryWebviewHandle, char* html);

// JavaScript
bool wry_webview_evaluate_script(WryWebviewHandle, char* js);

// DevTools
void wry_webview_open_devtools(WryWebviewHandle);
void wry_webview_close_devtools(WryWebviewHandle);

// Cleanup
void wry_webview_free(WryWebviewHandle);
```

### Dialogs

```c
// File dialogs (via rfd)
WryDialogSelection wry_dialog_open(WryDialogOpenOptions*);
WryDialogSelection wry_dialog_save(WryDialogSaveOptions*);
void wry_dialog_selection_free(WryDialogSelection);

// Message dialogs (via tinyfiledialogs)
bool wry_dialog_message(WryMessageDialogOptions*);
bool wry_dialog_confirm(WryConfirmDialogOptions*);
bool wry_dialog_ask(WryAskDialogOptions*);
WryPromptDialogResult wry_dialog_prompt(WryPromptDialogOptions*);
```

### Menus (macOS only)

```c
WryMenuBarHandle wry_menu_bar_new();
WrySubmenuHandle wry_submenu_new(char* title, bool enabled);
WryMenuItemHandle wry_menu_item_new(char* id, char* title, bool enabled, char* accelerator);
// ... returns no-op stubs on Linux/Windows
```

### System Tray (macOS only)

```c
WryTrayHandle wry_tray_new(WryTrayConfig*);
void wry_tray_set_title(WryTrayHandle, char* title);
void wry_tray_set_tooltip(WryTrayHandle, char* tooltip);
// ... returns no-op stubs on Linux/Windows
```

## JavaScript Bridge

Webviews have `window.ipc` injected:

```javascript
// Send message to backend
window.ipc.postMessage("your message here");
```

The backend receives messages via the IPC handler callback registered during webview creation.

## Platform Notes

### Linux (GTK)

- WebKitGTK must be built into the GTK widget tree
- Use `wry_webview_build_gtk(vbox)` via `WebViewBuilderExtUnix::build_gtk()`
- Without proper GTK integration, webview renders as blank white box
- Tested on WSL2 with WSLg (native Wayland)

### WSL2 Specifics

- Runs on native Wayland via WSLg by default
- Do NOT set `GDK_BACKEND=x11` - causes cursor invisibility issues
- Message dialogs use tinyfiledialogs (zenity/kdialog) because rfd's xdg-desktop-portal fails silently

## Thread Safety

- Event loop must be created/run on main thread
- Window operations should be called from main thread
- `wry_event_loop_proxy_*` functions are thread-safe
- Callbacks are invoked on the main/UI thread
