# Photino.NET Improvements

This document specifies improvements to make to the forked Photino.NET layer before and during the Wry FFI transition.

## Phase 1: Quick Wins (Before Wry Swap)

### 1.1 Remove Hardcoded Custom Scheme Limit

**Current State:**
```csharp
// Hardcoded array of 16 in PhotinoNativeParameters
private fixed byte _customSchemeNames[16 * 256];
```

**Change:**
- Use dynamic collection for scheme registration
- Pass schemes via separate initialization call or pointer to array
- No artificial limit

**Files:**
- `PhotinoNativeParameters.cs`
- `PhotinoWindow.NET.cs` (RegisterCustomSchemeHandler)

### 1.2 Structured IPC Messages

**Current State:**
```csharp
// String-only message passing
public event EventHandler<string> WebMessageReceived;
public PhotinoWindow SendWebMessage(string message);
```

**Change:**
Add typed message layer on top:

```csharp
public class TauriMessage<T>
{
    public string Command { get; set; }
    public T Payload { get; set; }
    public string RequestId { get; set; }  // For request/response correlation
}

// Type-safe invoke from JS
window.RegisterCommand<RequestType, ResponseType>("commandName", handler);

// JS side: window.tauri.invoke("commandName", payload) -> Promise<response>
```

**Implementation:**
- JSON serialization via System.Text.Json
- Request/response correlation via ID
- Async/await support
- Keep raw string API for backwards compatibility

### 1.3 Improve Error Handling

**Current State:**
- Errors from native layer often silently fail or throw generic exceptions
- No structured error types

**Change:**
```csharp
public class TauriException : Exception
{
    public TauriErrorCode Code { get; }
    public string NativeMessage { get; }
}

public enum TauriErrorCode
{
    WindowCreationFailed,
    WebviewInitFailed,
    NavigationFailed,
    CustomSchemeError,
    IpcError,
    // ...
}
```

### 1.4 Configuration Builder Pattern

**Current State:**
- Mix of constructor parameters and fluent setters
- Some settings only work before window creation

**Change:**
- Clear separation: `TauriWindowBuilder` for pre-creation config
- `TauriWindow` for runtime operations
- Immutable configuration object

```csharp
var config = new TauriWindowConfig
{
    Title = "My App",
    Size = (1200, 800),
    MinSize = (400, 300),
    DevToolsEnabled = true,
    // ...
};

var window = TauriWindow.Create(config);
```

---

## Phase 2: API Modernization (During Wry Swap)

### 2.1 Async-First API

**Current State:**
```csharp
// Blocking operations
window.Load("https://example.com");
window.WaitForClose();
```

**Change:**
```csharp
await window.LoadAsync("https://example.com");
await window.WaitForCloseAsync(cancellationToken);

// Non-blocking by default
window.Load(uri);  // Returns immediately, fires event on completion
```

### 2.2 Event System Overhaul

**Current State:**
- Mix of events and fluent handler registration
- Inconsistent naming

**Change:**
```csharp
// Consistent event naming
window.Created += OnCreated;
window.Closing += OnClosing;
window.Closed += OnClosed;
window.Resized += OnResized;
window.Moved += OnMoved;
window.Focused += OnFocused;
window.WebMessageReceived += OnWebMessage;
window.NavigationStarting += OnNavStarting;
window.NavigationCompleted += OnNavCompleted;

// Keep fluent API as convenience
window.OnCreated(handler).OnClosing(handler);
```

### 2.3 Resource Management

**Current State:**
- Manual disposal, easy to leak native resources

**Change:**
```csharp
public class TauriWindow : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        // Clean shutdown sequence
        await CloseAsync();
        // Release native handle
    }
}

// Usage
await using var window = TauriWindow.Create(config);
await window.RunAsync();
```

---

## Phase 3: New Features (Post Wry Swap)

### 3.1 Multi-Window Support

```csharp
var app = TauriApp.Create();

var mainWindow = app.CreateWindow(mainConfig);
var settingsWindow = app.CreateWindow(settingsConfig);

// Window relationships
settingsWindow.SetParent(mainWindow);
settingsWindow.SetModal(true);

await app.RunAsync();
```

### 3.2 System Tray

```csharp
var tray = app.CreateSystemTray(new TrayConfig
{
    Icon = "icon.png",
    Tooltip = "My App",
    Menu = new TrayMenu()
        .AddItem("Open", () => mainWindow.Show())
        .AddSeparator()
        .AddItem("Quit", () => app.Quit())
});
```

### 3.3 Global Shortcuts

```csharp
app.RegisterGlobalShortcut("Ctrl+Shift+P", () =>
{
    mainWindow.Show();
    mainWindow.Focus();
});
```

### 3.4 Native Menus

```csharp
var menu = new Menu()
    .AddSubmenu("File", new Menu()
        .AddItem("New", "Ctrl+N", OnNew)
        .AddItem("Open", "Ctrl+O", OnOpen)
        .AddSeparator()
        .AddItem("Exit", OnExit))
    .AddSubmenu("Edit", new Menu()
        .AddItem("Undo", "Ctrl+Z", OnUndo)
        .AddItem("Redo", "Ctrl+Y", OnRedo));

window.SetMenu(menu);
```

### 3.5 Plugin System

```csharp
public interface ITauriPlugin
{
    string Name { get; }
    void Initialize(TauriApp app);
    void RegisterCommands(CommandRegistry registry);
}

// Built-in plugins
app.UsePlugin<DialogPlugin>();      // File dialogs
app.UsePlugin<NotificationPlugin>(); // System notifications
app.UsePlugin<ShellPlugin>();        // Open URLs, run commands
app.UsePlugin<ClipboardPlugin>();    // Clipboard access

// Custom plugins
app.UsePlugin<MyCustomPlugin>();
```

---

## Files to Modify/Create

### Existing Files (Modify)

| File | Changes |
|------|---------|
| `PhotinoWindow.NET.cs` | Rename to `TauriWindow.cs`, refactor API |
| `PhotinoNativeParameters.cs` | Remove limits, rename to `WindowParams.cs` |
| `PhotinoDllImports.cs` | Update to target wry-ffi |
| `PhotinoDialogEnums.cs` | Keep, rename namespace |

### New Files (Create)

| File | Purpose |
|------|---------|
| `TauriApp.cs` | Application lifecycle, multi-window |
| `TauriWindowBuilder.cs` | Configuration builder |
| `TauriMessage.cs` | Typed IPC messages |
| `TauriException.cs` | Structured errors |
| `CommandRegistry.cs` | Command registration for IPC |
| `Plugins/ITauriPlugin.cs` | Plugin interface |
| `Plugins/DialogPlugin.cs` | File dialog implementation |
| `Plugins/NotificationPlugin.cs` | Notification implementation |
| `Plugins/ShellPlugin.cs` | Shell operations |
| `Plugins/ClipboardPlugin.cs` | Clipboard operations |

---

## Breaking Changes

Moving from Photino to tauri-csharp will have breaking changes:

1. **Namespace:** `Photino.NET` → `TauriCSharp`
2. **Class names:** `PhotinoWindow` → `TauriWindow`
3. **Custom scheme limit:** Removed (non-breaking, but API may change)
4. **Some method signatures:** Async variants, typed messages

Migration guide will be provided.
