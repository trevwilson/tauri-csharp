# tauri-csharp Roadmap

## Phase 1: Foundation (Weeks 1-2)

### Goals
- Fork Photino.NET, establish project structure
- Fix immediate issues
- Validate build pipeline

### Tasks

- [ ] **1.1 Project Setup**
  - [ ] Fork Photino.NET repository
  - [ ] Rename namespaces: `Photino.NET` → `TauriCSharp`
  - [ ] Rename classes: `PhotinoWindow` → `TauriWindow`
  - [ ] Update project files, package metadata
  - [ ] Set up CI/CD (GitHub Actions)
  - [ ] Create NOTICE file with Photino attribution

- [ ] **1.2 Quick Wins**
  - [ ] Remove 16 custom scheme limit
  - [ ] Add structured IPC messages (JSON serialization)
  - [ ] Improve error handling (TauriException)
  - [ ] Add request/response correlation for IPC

- [ ] **1.3 Documentation**
  - [ ] Migration guide from Photino
  - [ ] Basic API documentation
  - [ ] Example applications

### Deliverable
Working NuGet package that's API-compatible with Photino but renamed and with improvements.

---

## Phase 2: Wry FFI Layer (Weeks 3-5)

### Goals
- Create Rust FFI crate exposing Wry/Tao
- Build for Windows, macOS, Linux
- Validate interop works

### Tasks

- [ ] **2.1 Rust Crate Setup**
  - [ ] Create `wry-ffi` crate
  - [ ] Add Wry and Tao dependencies
  - [ ] Configure cdylib output
  - [ ] Set up cross-compilation (cross-rs or manual)

- [ ] **2.2 Core FFI Implementation**
  - [ ] App lifecycle (create, run, quit, destroy)
  - [ ] Window management (create, destroy, show/hide, resize, move)
  - [ ] Webview operations (navigate, load_html, evaluate_script)
  - [ ] IPC (send_message, receive callback)
  - [ ] Custom protocol registration

- [ ] **2.3 JavaScript Bridge**
  - [ ] Implement `window.tauri.invoke()` pattern
  - [ ] Request/response correlation
  - [ ] Event emission from backend

- [ ] **2.4 Build Pipeline**
  - [ ] GitHub Actions for Windows x64
  - [ ] GitHub Actions for macOS x64 + arm64
  - [ ] GitHub Actions for Linux x64
  - [ ] Package native binaries for NuGet

### Deliverable
`wry_ffi.dll/.dylib/.so` binaries that can be called from C#.

---

## Phase 3: Native Layer Swap (Weeks 6-7)

### Goals
- Replace Photino.Native with wry-ffi
- Maintain API compatibility
- Ship unified package

### Tasks

- [ ] **3.1 P/Invoke Bindings**
  - [ ] Create `WryInterop.cs` with LibraryImport declarations
  - [ ] Implement struct marshaling
  - [ ] Implement callback marshaling
  - [ ] Native library resolver for multi-platform

- [ ] **3.2 TauriWindow Refactor**
  - [ ] Replace Photino P/Invoke calls with Wry calls
  - [ ] Adapt for any API differences
  - [ ] Handle callback registration and prevent GC

- [ ] **3.3 Testing**
  - [ ] Unit tests for P/Invoke bindings
  - [ ] Integration tests on each platform
  - [ ] Manual smoke tests

- [ ] **3.4 NuGet Package**
  - [ ] Include native binaries for all platforms
  - [ ] Runtime identifier selection
  - [ ] Transitive dependency management

### Deliverable
`TauriCSharp` NuGet package using Wry backend, working on Windows/macOS/Linux.

---

## Phase 4: Extended Features (Weeks 8-12)

### Goals
- Add features Photino lacked
- Improve developer experience
- Build plugin system

### Tasks

- [ ] **4.1 System Tray**
  - [ ] Add Tao system tray to wry-ffi
  - [ ] C# API for tray creation and management
  - [ ] Menu building API

- [ ] **4.2 Native Menus**
  - [ ] Window menu bar support
  - [ ] Context menus
  - [ ] Keyboard shortcuts

- [ ] **4.3 Global Shortcuts**
  - [ ] Register system-wide hotkeys
  - [ ] Callback when hotkey triggered

- [ ] **4.4 Plugin System**
  - [ ] Define `ITauriPlugin` interface
  - [ ] Plugin registration in TauriApp
  - [ ] Command routing to plugins
  - [ ] Built-in plugins:
    - [ ] DialogPlugin (file open/save)
    - [ ] NotificationPlugin
    - [ ] ShellPlugin (open URLs, run commands)
    - [ ] ClipboardPlugin

- [ ] **4.5 Multi-Window**
  - [ ] TauriApp managing multiple windows
  - [ ] Window relationships (parent, modal)
  - [ ] Cross-window communication

### Deliverable
Feature-rich desktop framework competitive with Tauri (minus mobile).

---

## Phase 5: Mobile Support (Weeks 13-20)

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

## Phase 6: Blazor Integration (Optional, Weeks 21-24)

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

| Dependency | Version | License |
|------------|---------|---------|
| Wry | Latest | Apache 2.0 |
| Tao | Latest | Apache 2.0 |
| .NET | 8.0+ | MIT |
| Rust | 1.70+ | MIT/Apache 2.0 |

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Wry API changes | Pin versions, follow Tauri release notes |
| FFI complexity | Start simple, extensive testing |
| Mobile is harder than expected | Phase 5 is optional, desktop-first is viable product |
| Limited maintainer bandwidth | Open source early, attract contributors |
