# TauriCSharp

Cross-platform desktop applications with web UI, powered by Wry.

## Overview

TauriCSharp enables .NET developers to build desktop applications using web technologies (HTML, CSS, JavaScript) for the UI layer while leveraging C# for backend logic.

**Status:** Work in progress. Currently uses Photino.Native as the webview backend, with plans to migrate to Wry FFI.

## Features

- Cross-platform: Windows, macOS, Linux
- Lightweight: Uses native OS webviews (WebView2, WebKit, WebKitGTK)
- Fluent API for window configuration
- Custom URL scheme handlers
- Bidirectional JavaScript ↔ C# communication
- File dialogs, notifications

## Roadmap

See `/docs/05-roadmap.md` for the full implementation plan.

- **Phase 1:** Fork and modernize Photino.NET ← *current*
- **Phase 2:** Build Wry FFI layer
- **Phase 3:** Swap native backend
- **Phase 4:** Extended features (system tray, menus, shortcuts)
- **Phase 5:** Mobile support (iOS, Android)

## Attribution

This project is derived from [Photino.NET](https://github.com/tryphotino/photino.NET) and uses [Wry](https://github.com/tauri-apps/wry) / [Tao](https://github.com/tauri-apps/tao) from the Tauri project.

Licensed under Apache 2.0. See LICENSE and NOTICE files.
