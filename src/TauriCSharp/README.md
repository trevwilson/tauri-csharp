# TauriCSharp

Cross-platform desktop applications with web UI, powered by Wry.

## Overview

TauriCSharp enables .NET developers to build desktop applications using web technologies (HTML, CSS, JavaScript) for the UI layer while leveraging C# for backend logic.

**Status:** Active development. Core functionality working on Linux (WSL2 tested), Windows and macOS support in progress.

## Features

- Cross-platform: Windows, macOS, Linux
- Lightweight: Uses native OS webviews (WebView2, WebKit, WebKitGTK)
- Fluent API for window configuration
- Custom URL scheme handlers (app://, etc.)
- Bidirectional JavaScript ↔ C# communication
- File dialogs (open, save)
- Message dialogs (confirm, ask, prompt)

## Quick Start

```csharp
using TauriCSharp;

var window = new TauriWindow()
    .SetTitle("My App")
    .SetSize(1024, 768)
    .Load("app://localhost/index.html")
    .RegisterCustomSchemeHandler("app", ServeStaticFiles);

window.WaitForClose();
```

## Dialogs API

```csharp
// File dialogs
string[] paths = Dialogs.OpenFile(title: "Open", allowMultiple: true);
string? path = Dialogs.SaveFile(title: "Save", defaultName: "file.txt");

// Message dialogs
bool confirmed = Dialogs.Confirm("Title", "Proceed?");
bool yes = Dialogs.Ask("Title", "Question?");
string? input = Dialogs.Prompt("Title", "Enter value:");
```

## Architecture

```
Your App (.NET)
    ↓
TauriCSharp (C# library)
    ↓ P/Invoke
wry-ffi (Rust FFI layer)
    ↓
Wry + Tao (Rust crates)
    ↓
Native Webview (WebView2 / WebKit / WebKitGTK)
```

## Roadmap

See `/docs/05-roadmap.md` for the full implementation plan.

- **Phase 1-2:** Foundation & Wry FFI ✅
- **Phase 3:** C# Bindings & Validation ✅
- **Phase 4:** Extended features (dialogs ✅, tray, menus) 🔄
- **Phase 5:** CI/CD & NuGet publishing
- **Phase 6:** Mobile support (iOS, Android)

## Attribution

This project is derived from [Photino.NET](https://github.com/tryphotino/photino.NET) and uses [Wry](https://github.com/tauri-apps/wry) / [Tao](https://github.com/tauri-apps/tao) from the Tauri project.

Licensed under Apache 2.0. See LICENSE and NOTICE files.
