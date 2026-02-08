# tauri-csharp Roadmap

## Phase 1: Foundation ✅ COMPLETE

### Goals
- Fork Photino.NET, establish project structure
- Fix immediate issues

### Tasks

- [x] **1.1 Project Setup**
  - [x] Fork Photino.NET repository
  - [x] Rename namespaces: `Photino.NET` → `TauriCSharp`
  - [x] Rename classes: `PhotinoWindow` → `TauriWindow`
  - [x] Update project files, package metadata
  - [x] Create NOTICE file with Photino attribution

- [x] **1.2 Quick Wins**
  - [x] Remove 16 custom scheme limit (wry-ffi has no limit)
  - [x] Add structured IPC messages (JSON serialization via TauriIpc)
  - [x] Improve error handling (TauriException hierarchy)
  - [x] Add request/response correlation for IPC

### Deliverable
Renamed and improved codebase ready for native layer swap.

---

## Phase 2: Wry FFI Layer ✅ COMPLETE

### Goals
- Create Rust FFI crate exposing Wry/Tao
- Validate interop works on local platform

### Tasks

- [x] **2.1 Rust Crate Setup**
  - [x] Create `wry-ffi` crate
  - [x] Add Wry and Tao dependencies (vendored)
  - [x] Configure cdylib output

- [x] **2.2 Core FFI Implementation**
  - [x] App lifecycle (create, run, quit, destroy)
  - [x] Window management (create, destroy, show/hide, resize, move, focus)
  - [x] Webview operations (navigate, load_html, evaluate_script, send_message)
  - [x] IPC (message callback)
  - [x] Custom protocol registration
  - [x] Window callbacks (closing, resized, moved, focus, navigation)

- [x] **2.3 JavaScript Bridge**
  - [x] Implement `window.tauri.invoke()` pattern
  - [x] Request/response correlation
  - [x] Event emission from backend

### Deliverable
`wry_ffi.dll/.dylib/.so` binaries that can be called from C#.

---

## Phase 3: C# Bindings & Validation ✅ COMPLETE

### Goals
- Replace Photino.Native with wry-ffi
- Maintain API compatibility
- Validate with tests before extending

### Tasks

- [x] **3.1 P/Invoke Bindings**
  - [x] Create `WryInterop.cs` with LibraryImport declarations
  - [x] Implement struct marshaling
  - [x] Implement callback marshaling (WryDelegates.cs)
  - [x] WryCallbackRegistry for GCHandle pinning (prevents delegate GC)

- [x] **3.2 TauriWindow Refactor**
  - [x] Replace all Photino P/Invoke calls with Wry calls
  - [x] Handle callback registration and prevent GC
  - [x] IDisposable implementation with proper cleanup

- [x] **3.3 New API Surface**
  - [x] ExecuteScript/ExecuteScriptAsync - run JavaScript
  - [x] OpenDevTools/CloseDevTools - runtime devtools control
  - [x] Show/Hide/IsVisible - window visibility
  - [x] Focus - bring window to front
  - [x] Restore - restore from maximized/minimized
  - [x] CurrentUrl property - get webview URL
  - [x] NavigationStarting event - can cancel navigation

- [x] **3.4 Testing**
  - [x] Test app with interactive UI (TauriCSharp.TestApp)
  - [x] IPC round-trip test (ping/pong)
  - [x] Custom protocol test (app:// scheme)
  - [x] Window events (resize, move, focus, close)
  - [x] WebView tests (execute script, navigate, devtools)

- [ ] **3.5 Local NuGet Package**
  - [ ] Native library resolver for multi-platform loading
  - [ ] Local `.nupkg` build for testing
  - [ ] Test package install in fresh project

### Deliverable
Validated `TauriCSharp` that works on current platform, ready for feature expansion.

---

## Phase 4: Extended Features 🔄 IN PROGRESS

### Goals
- Add features Photino lacked via additional Rust crates
- Expose Tao's built-in features
- Achieve feature parity with Velox

### Reference
Velox (Swift Tauri port) uses these crates beyond wry/tao:
- `rfd` - Rusty File Dialogs
- `tray-icon` - System tray
- `muda` - Native menus

### Tasks

- [x] **4.1 Dialogs**
  - [x] Add `rfd` and `tinyfiledialogs` dependencies
  - [x] Implement FFI: file open/save dialogs (rfd)
  - [x] Implement FFI: message dialogs (tinyfiledialogs - rfd fails on Linux/WSL2)
  - [x] C# P/Invoke bindings in WryInterop.cs
  - [x] Public `Dialogs` static class with clean API
  - [x] Test coverage in test app

- [ ] **4.2 System Tray (tray-icon crate)** — macOS only
  - [x] Add `tray-icon` dependency to wry-ffi
  - [x] Implement Rust FFI layer
  - [ ] C# P/Invoke bindings
  - [ ] TauriTray class with C# API
  - Note: Returns no-op stubs on Linux/Windows

- [ ] **4.3 Native Menus (muda crate)** — macOS only
  - [x] Add `muda` dependency to wry-ffi
  - [x] Implement Rust FFI layer
  - [ ] C# P/Invoke bindings
  - [ ] TauriMenu builder API
  - Note: Returns no-op stubs on Linux/Windows

- [x] **4.4 Monitor/Display Info (tao built-in)**
  - [x] Rust FFI already existed: `wry_window_current_monitor`, `wry_window_primary_monitor`, `wry_window_available_monitors`, `wry_window_scale_factor`
  - [x] C# P/Invoke bindings (already in WryInterop.cs)
  - [x] Wire `Monitors`, `MainMonitor`, `CurrentMonitor`, `ScreenDpi` properties via JSON parsing
  - [x] Add `Name` property to `Monitor` struct

- [x] **4.5 Window Icon (tao built-in)**
  - [x] Add `image` crate (PNG/ICO/JPEG loading)
  - [x] Implement FFI: `wry_window_set_icon_file`, `wry_window_set_icon_rgba`, `wry_window_clear_icon`
  - [x] C# P/Invoke bindings
  - [x] `TauriWindow.SetIcon()`, `ClearIcon()`, `IconFile` setter

- [x] **4.6 Notifications**
  - [x] Add `notify-rust` crate (D-Bus on Linux, native on Windows/macOS)
  - [x] Implement FFI: `wry_notification_show` with timeout and urgency support
  - [x] C# P/Invoke bindings
  - [x] Public `Notifications.Show()` static API (follows `Dialogs` pattern)
  - Note: Requires notification daemon; fails gracefully on WSL2

- [x] **4.7 Global Shortcuts (global-hotkey crate)**
  - [x] Add `global-hotkey` crate
  - [x] Implement FFI: `wry_shortcut_register`, `wry_shortcut_unregister`, `wry_shortcut_unregister_all`
  - [x] Deliver shortcut events via JSON event loop polling
  - [x] C# P/Invoke bindings
  - [x] Public `GlobalShortcuts` static class with callback dispatch
  - Note: On WSL2/Wayland, only captures keys when X11 apps have focus

- [x] **4.8 Multi-Window**
  - [x] `TauriApp` singleton managing multiple windows via `ConcurrentDictionary`
  - [x] `TauriApp.Run()` for multi-window event loop with per-window event routing
  - [x] Dynamic window creation after `Run()` via `TauriApp.InitializeWindow()`
  - [x] Child window event routing when main window uses `WaitForClose()`
  - [ ] Window relationships (parent, modal)
  - [ ] Cross-window communication

### Deliverable
Feature-rich desktop framework competitive with Tauri and Velox (minus mobile).

---

## Phase 5: CI/CD, Packaging & Documentation

### Goals
- Automated cross-platform builds
- Published NuGet package
- Documentation for adoption

### Tasks

- [ ] **5.1 Cross-Platform Builds**
  - [ ] Set up cross-compilation (cross-rs or manual)
  - [ ] GitHub Actions for Windows x64
  - [ ] GitHub Actions for macOS x64 + arm64
  - [ ] GitHub Actions for Linux x64

- [ ] **5.2 NuGet Publishing**
  - [ ] Package native binaries for all platforms
  - [ ] Runtime identifier (RID) selection
  - [ ] Transitive dependency management
  - [ ] Publish to NuGet.org

- [ ] **5.3 Documentation**
  - [ ] Migration guide from Photino
  - [ ] API documentation
  - [ ] Getting started guide
  - [ ] Example applications

### Deliverable
Published `TauriCSharp` NuGet package with documentation.

---

## Phase 6: Mobile Support

### Goals
- iOS support via Wry
- Android support via Wry
- Unified build experience

### Tasks

- [ ] **5.1 iOS**
  - [ ] Wry iOS build configuration
  - [ ] .NET iOS binding project
  - [ ] Test app deployment

- [ ] **5.2 Android**
  - [ ] Wry Android build configuration (JNI)
  - [ ] .NET Android binding project
  - [ ] Test app deployment

- [ ] **5.3 Unified API**
  - [ ] Platform-agnostic TauriApp
  - [ ] Conditional features (tray not on mobile, etc.)
  - [ ] Documentation for mobile setup

### Deliverable
Cross-platform framework: Windows, macOS, Linux, iOS, Android.

---

## Phase 7: Blazor Integration (Optional)

### Goals
- First-class Blazor support
- Port Photino.Blazor concepts
- Optimize for Blazor workflows

### Tasks

- [ ] **6.1 TauriCSharp.Blazor Package**
  - [ ] BlazorWebViewManager adaptation
  - [ ] Static file serving via custom protocol
  - [ ] JavaScript interop bridge

- [ ] **6.2 Blazor Templates**
  - [ ] `dotnet new tauri-blazor` template
  - [ ] Sample applications

### Deliverable
`TauriCSharp.Blazor` package for Blazor desktop apps.

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Platforms supported | 5 (Win, Mac, Linux, iOS, Android) |
| Bundle size (minimal app) | < 10 MB |
| Startup time | < 500ms |
| API surface coverage vs Tauri | 80%+ |
| NuGet downloads (6 months) | 10,000+ |

---

## Dependencies

### Current (Phase 1-3)

| Dependency | Version | License | Purpose |
|------------|---------|---------|---------|
| Wry | 0.50+ | Apache 2.0 | WebView abstraction |
| Tao | 0.33+ | Apache 2.0 | Windowing, event loop |
| .NET | 8.0+ | MIT | Runtime |
| Rust | 1.70+ | MIT/Apache 2.0 | Build toolchain |

### Phase 4 Additions

| Dependency | Version | License | Purpose |
|------------|---------|---------|---------|
| rfd | 0.14+ | MIT | File dialogs (open/save/folder/message) |
| tray-icon | 0.21+ | Apache 2.0 | System tray |
| muda | 0.17+ | Apache 2.0 | Native menus |
| notify-rust | 4 | MIT/Apache 2.0 | Desktop notifications (D-Bus/native) |
| global-hotkey | 0.6 | Apache 2.0 | System-wide keyboard shortcuts |
| image | 0.25 | MIT | Icon loading (PNG/ICO/JPEG → RGBA) |

Reference: [Velox runtime-wry-ffi](https://github.com/velox-apps/velox) uses same crate stack.

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Wry API changes | Pin versions, follow Tauri release notes |
| FFI complexity | Start simple, extensive testing |
| Mobile is harder than expected | Phase 5 is optional, desktop-first is viable product |
| Limited maintainer bandwidth | Open source early, attract contributors |
