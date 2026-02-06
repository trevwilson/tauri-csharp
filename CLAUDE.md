# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TauriCSharp is a cross-platform desktop framework enabling .NET developers to build applications with web UI (HTML/CSS/JS) and C# backend. It uses Wry (WebView) and Tao (windowing) from the Tauri project via a Rust FFI layer.

## Mandatory Instructions

You **MUST** use robust, performant, and best-practice implementations without taking shortcuts or easy routes. This is a library intended for broad usage in a variety of applications - minimal approaches, workarounds, and hand-rolled alternatives to standard library features are **NOT** acceptable without explicit instruction or approval.

## Build Commands

```bash
# Build Rust FFI library (required after Rust changes)
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
    ↓ P/Invoke
wry-ffi (Rust FFI layer)
    ↓
Wry + Tao (vendored Rust crates)
    ↓
Native Webview (WebView2 / WebKit / WebKitGTK)
```

### Key Components

- **`src/TauriCSharp/TauriCSharp/`** - Main .NET library
  - `TauriWindow.cs` - Window creation and management (fluent API)
  - `Dialogs.cs` - Public dialog API (file open/save, message dialogs)
  - `Interop/WryInterop.cs` - P/Invoke bindings to Rust FFI
  - `Interop/WryTypes.cs` - FFI struct definitions

- **`src/wry-ffi/`** - Rust FFI crate exposing Wry/Tao to C
  - `src/event_loop.rs` - Event loop management
  - `src/window.rs` - Window creation
  - `src/webview.rs` - WebView operations
  - `src/dialogs.rs` - File and message dialogs

- **`vendor/`** - Vendored Wry and Tao crates (may have local patches)

### Event Loop Pattern

The Rust FFI uses a pump-based event loop. C# calls `wry_event_loop_pump()` which processes pending events and returns JSON-serialized events via callback. This allows .NET to control the loop rather than blocking in Rust.

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

### Dialog Backends

| Dialog Type | Backend | Platform |
|-------------|---------|----------|
| File open/save | rfd → GTK FileChooser | Cross-platform |
| Message/Confirm | tinyfiledialogs → zenity | Cross-platform |
| Menus | muda | macOS only |
| System tray | tray-icon | macOS only |

## Documentation

- `docs/05-roadmap.md` - Project phases and task tracking
- `docs/01-architecture-overview.md` - Detailed architecture
- `docs/private/session-handover.md` - Current session state for handoffs

## Current Branch

`velox-ffi-adoption` - Migration to Velox-based wry-ffi (Phases 1-3 complete, Phase 4 in progress)
