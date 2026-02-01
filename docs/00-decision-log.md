# tauri-csharp Decision Log

This document captures the decision-making process that led to this project.

## Problem Statement

The .NET ecosystem lacks a modern, maintained solution for building desktop applications with web frontends. The options are:

| Option | Problem |
|--------|---------|
| **Electron** | Bundles entire Chromium (~150MB+), massive overhead |
| **Photino** | Dormant (no updates in 12+ months), missing features, no mobile |
| **MAUI** | Buggy, no official Linux support, not webview-focused |
| **Avalonia** | Solid, but XAML-based—not web frontend |

The web stack (HTML/CSS/JS) has won as the UI lingua franca. XAML has been stagnant for a decade. Developers want web frontends with native backends.

## Options Evaluated

### Option A: Wait for Tauri Multi-Language Support
- **Verdict: Not viable**
- Tauri issue #4670 is marked "priority: low - Accepted but likely won't be worked on"
- No implementation work planned, no resources allocated
- Only workaround is sidecar (separate process), which is architecturally awkward

### Option B: Pure Fork of Photino
- **Pros:** No Rust dependency, full control, simpler build
- **Cons:** Must maintain 5 platform native implementations, mobile is a massive lift from scratch
- **Verdict: Partial—good .NET layer, problematic native layer**

### Option C: Fresh Wry FFI Implementation
- **Pros:** Wry has mobile support, actively maintained by funded Tauri team
- **Cons:** New .NET API design from scratch, lose Photino's good patterns
- **Verdict: Overkill—don't need to rewrite everything**

### Option D: Hybrid (Fork Photino .NET + Replace Native with Wry)
- **Pros:** Reuse Photino's solid .NET layer, get Wry's maintained native layer
- **Cons:** FFI boundary complexity, Rust in build pipeline
- **Verdict: Best balance of reuse and sustainability**

## Decision: Path D (Hybrid)

Fork Photino.NET's managed layer (which is well-designed) and progressively replace the native layer with Wry FFI bindings. This gives us:

1. Photino's fluent .NET API patterns
2. Wry/Tao's actively maintained webview abstraction
3. Mobile support (iOS/Android) via Wry
4. Future Wry improvements flow downstream

## Photino Architecture Analysis

### What Photino Got Right
- Clean two-tier architecture (native C/C++ + .NET bindings)
- Flat C API in Exports.cpp—language agnostic
- Modern P/Invoke with `LibraryImport`
- Fluent .NET API, idiomatic C#
- Event-based extensibility
- Custom scheme handlers for local asset serving

### What's Constraining
- Monolithic native layer (no plugin system)
- Hardcoded 16 custom schemes limit
- String-only IPC (no structured serialization)
- No system tray, menus, global shortcuts
- No mobile support at all
- Platform JS bridges are baked in

## Wry/Tao Value Proposition

**Tao** (event loop, windowing):
- Cross-platform window creation
- System tray, global shortcuts, menus
- Clipboard, drag-and-drop
- Fork of winit, focused on desktop apps

**Wry** (webview abstraction):
- WebView2 (Windows), WebKit (macOS/iOS), WebKitGTK (Linux), Android WebView
- Custom protocol handlers
- IPC bridge
- **Mobile support** (iOS/Android)
- Actively maintained by funded Tauri team

## Implementation Phases

### Phase 1: Fork + Modernize
- Fork Photino.NET, rename to tauri-csharp
- Fix low-hanging fruit (16 scheme limit, IPC improvements)
- Keep using Photino.Native temporarily
- Establish project structure

### Phase 2: Wry FFI Foundation
- Create `wry-ffi` Rust crate
- Implement core: window, webview, IPC, custom protocols
- Build for Windows/Mac/Linux
- Test alongside Photino.Native

### Phase 3: Swap Native Layer
- Modify .NET P/Invoke to target wry-ffi
- Handle API differences
- Deprecate Photino.Native dependency

### Phase 4: Extend
- Add system tray, menus, shortcuts via Tao
- Add mobile targets (iOS, Android)
- Build plugin system

## Licensing

Both Photino and Wry/Tao use Apache 2.0. Requirements:
- Preserve LICENSE file
- Preserve/extend NOTICE file with attribution
- Mark modified files with change notices
- Cannot use "Photino" trademark

## Comparable Projects

**Velox** (https://github.com/ArysSilva/ArysSilva-.NET-WebView-Tutorial-apps): Swift port of Tauri architecture
- Proves the Wry FFI pattern works for non-Rust languages
- Uses same Wry/Tao foundation
- Good reference for FFI surface design

## Date

Decision made: 2026-01-28
