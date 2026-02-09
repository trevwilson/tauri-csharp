# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TauriCSharp is a cross-platform desktop framework enabling .NET developers to build applications with web UI (HTML/CSS/JS) and C# backend. It uses Wry (WebView) and Tao (windowing) from the Tauri project via a Rust FFI layer.

## Mandatory Instructions

You **MUST** use robust, performant, and best-practice implementations without taking shortcuts or easy routes. This is a library intended for broad usage in a variety of applications - minimal approaches, workarounds, and hand-rolled alternatives to standard library features are **NOT** acceptable without explicit instruction or approval.

## Build Commands

```bash
# Build Rust FFI library (required after Rust changes)
# IMPORTANT: cargo must be in PATH — use: PATH="$HOME/.cargo/bin:$PATH"
# The script builds release by default and copies to TestApp bin/Debug/net8.0/
./scripts/build-wry-ffi.sh

# Run the test app
dotnet run --project src/TauriCSharp/TauriCSharp.TestApp/

# Build C# library only
dotnet build src/TauriCSharp/TauriCSharp/
```

## Architecture

```
Your App (.NET)
    ↓
TauriCSharp (C# library)
    ↓ P/Invoke (LibraryImport source generator)
wry-ffi (Rust FFI layer, cdylib)
    ↓
Wry + Tao (vendored Rust crates)
    ↓
Native Webview (WebView2 / WebKit / WebKitGTK)
```

### Key Components

- **`src/TauriCSharp/TauriCSharp/`** - Main .NET library (multi-targets net8.0 + net9.0)
  - `TauriWindow.cs` - Window creation and management (fluent API, partial class)
  - `TauriWindow.Wry.cs` - Event loop, window/webview creation, event dispatch
  - `TauriWindow.WryCompat.cs` - Wry-ffi wrappers and JSON parsing helpers
  - `TauriApp.cs` - Multi-window lifecycle manager (singleton)
  - `Dialogs.cs` - Static file/message dialog API
  - `Notifications.cs` - Static desktop notification API
  - `GlobalShortcuts.cs` - Static global keyboard shortcut API
  - `Interop/WryInterop.cs` - P/Invoke bindings to Rust FFI
  - `Interop/WryTypes.cs` - FFI struct definitions (must match Rust `types.rs` exactly)

- **`src/wry-ffi/`** - Rust FFI crate exposing Wry/Tao to C
  - `src/app.rs` - Event loop creation and pump (includes hotkey polling)
  - `src/window.rs` - Window creation, properties, parent/modal, icons
  - `src/webview.rs` - WebView operations
  - `src/dialogs.rs` - File and message dialogs
  - `src/notifications.rs` - Desktop notifications (notify-rust)
  - `src/shortcuts.rs` - Global shortcuts (global-hotkey)
  - `src/types.rs` - All FFI type definitions
  - `src/helpers.rs` - Shared utilities (guard_panic_bool, opt_cstring, thread-local buffers)
  - `src/events.rs` - Tao event → JSON serialization
  - `src/lib.rs` - Module declarations and re-exports

- **`vendor/`** - Vendored Wry and Tao crates (may have local patches)

### Event Loop Pattern

The Rust FFI uses a pump-based event loop. C# calls `wry_event_loop_pump()` which processes pending events and returns JSON-serialized events via callback. Events include a `window_id` field for routing to the correct window. This allows .NET to control the loop rather than blocking in Rust.

### Multi-Window Architecture

- Static singleton event loop shared across all windows
- **Single-window**: `TauriWindow.WaitForClose()` — runs event loop directly
- **Multi-window**: `TauriApp.Run()` — routes events by `window_id` to registered windows
- **Hybrid**: Main window uses `WaitForClose()`, child windows via `TauriApp` — events for non-matching windows are forwarded through `TauriApp.TryRouteEvent()`
- Parent/modal: `SetParent()` uses platform-specific APIs (transient_for on GTK, owner_window on Windows). `SetModal()` blocks parent input and auto-restores on child close.

### FFI Conventions

- Rust functions: `#[no_mangle] pub extern "C" fn wry_*` with `#[repr(C)]` structs
- C# bindings: `[LibraryImport]` source generator with `[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]`
- `[assembly: DisableRuntimeMarshalling]` — bool is 1-byte blittable in structs, but `[MarshalAs(UnmanagedType.U1)]` still needed on P/Invoke bool params/returns
- Strings: Rust uses `*const c_char` (null-terminated UTF-8), C# marshals via `StringMarshalling.Utf8` or manual `Marshal.StringToCoTaskMemUTF8`
- Thread-local `RefCell<CString>` buffers for returning strings from Rust to C#
- `guard_panic_bool` wraps FFI functions to catch panics at the boundary

### Linux GTK Integration

On Linux, WebKitGTK requires proper GTK widget tree integration:
- Tao window contains a GTK VBox container
- Webview must use `WebViewBuilderExtUnix::build_gtk(vbox)`
- Without this, webview exists but renders as blank white box
- This is handled in `src/wry-ffi/src/webview.rs`

## Platform Notes

### WSL2 (Linux on Windows)

- App runs on **native Wayland** via WSLg by default
- Do NOT set `GDK_BACKEND=x11` - causes cursor invisibility issues
- File dialogs use rfd (GTK native file chooser)
- Message dialogs use tinyfiledialogs (zenity/kdialog) because rfd's xdg-desktop-portal fails silently on WSL2
- Desktop notifications require a D-Bus notification daemon — typically not available on WSL2, fails gracefully
- Global shortcuts use X11 XGrabKey — only captures when X11 apps have focus on Wayland

### Feature Backends

| Feature | Backend | Platform Notes |
|---------|---------|----------------|
| File open/save | rfd → GTK FileChooser | Cross-platform |
| Message/Confirm | tinyfiledialogs → zenity | Cross-platform |
| Notifications | notify-rust → D-Bus | No daemon on WSL2 |
| Global shortcuts | global-hotkey → X11 | Limited on Wayland |
| Window icons | image crate (PNG/ICO/JPEG) | Cross-platform |
| Menus | muda | macOS only |
| System tray | tray-icon | macOS only |

## Documentation

- `docs/05-roadmap.md` - Project phases and task tracking
- `docs/01-architecture-overview.md` - Detailed architecture

## Current Branch

`velox-ffi-adoption` - Phases 1-4 complete. Next: Phase 5 (CI/CD, NuGet packaging, documentation)
