# tauri-csharp Architecture Overview

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Application                         │
├─────────────────────────────────────────────────────────────┤
│                   tauri-csharp (.NET)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Window API  │  │  IPC Layer  │  │  Plugin System      │  │
│  │ (fluent)    │  │ (typed msg) │  │  (extensible)       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│              P/Invoke Bindings (NativeInterop)              │
├─────────────────────────────────────────────────────────────┤
│                    wry-ffi (Rust)                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Thin FFI layer exposing Wry/Tao functionality      │    │
│  └─────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                    Wry + Tao (Rust)                         │
│  ┌───────────────────┐  ┌─────────────────────────────┐    │
│  │ Tao               │  │ Wry                         │    │
│  │ - Window mgmt     │  │ - Webview abstraction       │    │
│  │ - Event loop      │  │ - Custom protocols          │    │
│  │ - System tray     │  │ - JS bridge                 │    │
│  │ - Menus           │  │ - Navigation                │    │
│  │ - Global shortcuts│  │                             │    │
│  └───────────────────┘  └─────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                   Platform Webviews                         │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐  │
│  │ WebView2 │ │ WebKit   │ │WebKitGTK  │ │Android WebView│  │
│  │ (Windows)│ │(macOS/iOS)│ │ (Linux)   │ │  (Android)   │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

### tauri-csharp (.NET)

The managed layer, forked from Photino.NET with improvements:

| Component | Responsibility |
|-----------|----------------|
| **Window API** | Fluent builder for window configuration, lifecycle management |
| **IPC Layer** | Typed message passing between C# and JavaScript |
| **Plugin System** | Extensibility for dialogs, notifications, shell, etc. |
| **Custom Schemes** | Register handlers for `app://`, `asset://`, etc. |
| **Event System** | Window events, webview events, user-defined events |

### wry-ffi (Rust)

Thin FFI crate exposing Wry/Tao to C:

```rust
// Flat C API, no Rust types exposed
#[no_mangle]
pub extern "C" fn wry_window_create(params: *const WindowParams) -> *mut WryWindow;

#[no_mangle]
pub extern "C" fn wry_window_set_title(window: *mut WryWindow, title: *const c_char);

#[no_mangle]
pub extern "C" fn wry_webview_navigate(window: *mut WryWindow, url: *const c_char);
```

### P/Invoke Bindings

Modern C# interop to wry-ffi:

```csharp
[LibraryImport("wry_ffi", StringMarshalling = StringMarshalling.Utf8)]
internal static partial IntPtr wry_window_create(ref WindowParams parameters);

[LibraryImport("wry_ffi", StringMarshalling = StringMarshalling.Utf8)]
internal static partial void wry_window_set_title(IntPtr window, string title);
```

## Data Flow

### Application Startup

```
1. App creates TauriWindow via fluent API
2. TauriWindow builds WindowParams struct
3. P/Invoke calls wry_window_create()
4. wry-ffi creates Wry WebViewBuilder + Tao Window
5. Native window handle returned to .NET
6. Event loop starts (Tao)
```

### JS → C# Message

```
1. JavaScript: window.tauri.invoke("command", {data})
2. Wry intercepts via IPC handler
3. wry-ffi calls registered callback (function pointer)
4. Callback marshals to .NET delegate
5. C# handler processes message
6. Response sent back via wry_webview_evaluate_script()
```

### C# → JS Message

```
1. C#: window.SendMessage(data)
2. P/Invoke: wry_webview_evaluate_script(js)
3. Wry executes JavaScript in webview context
4. JS receives message via registered listener
```

### Custom Protocol Request

```
1. Webview requests app://assets/index.html
2. Wry intercepts via custom protocol handler
3. wry-ffi calls registered callback with URL
4. .NET handler returns Stream + content-type
5. wry-ffi reads stream, returns bytes to Wry
6. Wry serves response to webview
```

## Threading Model

```
┌─────────────────┐     ┌─────────────────┐
│   .NET Thread   │     │   UI Thread     │
│   (your code)   │     │   (Tao event    │
│                 │     │    loop)        │
└────────┬────────┘     └────────┬────────┘
         │                       │
         │  Invoke on UI thread  │
         │──────────────────────>│
         │                       │
         │  Callback to .NET     │
         │<──────────────────────│
         │                       │
```

- **UI operations** must happen on the Tao event loop thread
- **Callbacks** from native to .NET are invoked on the UI thread
- **Long-running C# work** should be offloaded to background threads
- **SendMessage** can be called from any thread (marshaled internally)

## Platform Targets

| Platform | Webview | Status |
|----------|---------|--------|
| Windows x64 | WebView2 | Phase 1 |
| macOS x64 | WebKit | Phase 1 |
| macOS arm64 | WebKit | Phase 1 |
| Linux x64 | WebKitGTK | Phase 1 |
| Linux arm64 | WebKitGTK | Phase 2 |
| iOS | WKWebView | Phase 4 |
| Android | Android WebView | Phase 4 |

## Build Artifacts

```
tauri-csharp/
├── src/
│   ├── TauriCSharp/              # Main .NET library
│   ├── TauriCSharp.Blazor/       # Blazor integration (optional)
│   └── wry-ffi/                  # Rust FFI crate
├── native/
│   ├── win-x64/wry_ffi.dll
│   ├── osx-x64/libwry_ffi.dylib
│   ├── osx-arm64/libwry_ffi.dylib
│   ├── linux-x64/libwry_ffi.so
│   └── linux-arm64/libwry_ffi.so
└── packages/
    └── TauriCSharp.1.0.0.nupkg   # NuGet package with native libs
```
