# wry-ffi

C FFI bindings for the Wry WebView library, designed for P/Invoke from .NET.

## Building

```bash
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

### Lifecycle

```c
WryApp wry_app_create();           // Create application
WryResult wry_app_run(WryApp);     // Run event loop (blocks)
void wry_app_quit(WryApp);         // Request quit
void wry_app_destroy(WryApp);      // Free resources
```

### Window Management

```c
WryWindow wry_window_create(WryApp, WryWindowParams*);
void wry_window_destroy(WryWindow);
void wry_window_set_visible(WryWindow, bool);
void wry_window_set_title(WryWindow, char*);
void wry_window_set_size(WryWindow, WrySize);
// ... and more
```

### Webview Operations

```c
WryResult wry_webview_navigate(WryWindow, char* url);
WryResult wry_webview_load_html(WryWindow, char* html);
WryResult wry_webview_evaluate_script(WryWindow, char* js);
WryResult wry_webview_send_message(WryWindow, char* msg);  // Thread-safe
```

### Callbacks

```c
void wry_window_set_message_callback(WryWindow, callback, user_data);
void wry_window_set_closing_callback(WryWindow, callback, user_data);
void wry_window_set_resized_callback(WryWindow, callback, user_data);
// ... and more
```

### Thread Dispatch

```c
void wry_invoke(WryApp, callback, user_data);       // Async
void wry_invoke_sync(WryApp, callback, user_data);  // Blocking
```

## JavaScript Bridge

Every webview has a `window.tauri` object injected:

```javascript
// Send command to backend
window.tauri.invoke('command', { data: 'payload' })
  .then(response => console.log(response))
  .catch(error => console.error(error));

// Listen for events
const unlisten = window.tauri.listen('event-name', payload => {
  console.log('Received:', payload);
});

// Stop listening
unlisten();
```

## Testing

```bash
# Build the C test
cd tests
gcc -o test_basic test_basic.c -L../target/release -lwry_ffi

# Run (Linux)
LD_LIBRARY_PATH=../target/release ./test_basic
```

## Thread Safety

- `wry_app_create`, `wry_app_run` must be called from main thread
- Window operations should be called from main thread
- `wry_webview_send_message` and `wry_invoke` are thread-safe
- Callbacks are invoked on the main/UI thread
