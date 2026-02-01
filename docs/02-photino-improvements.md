# Photino.NET Improvements

This document specifies improvements to make to the forked Photino.NET layer before and during the Wry FFI transition.

## Implementation Status

| Item | Status | Commit |
|------|--------|--------|
| 1.1 Custom scheme limit | ⚠️ Partial | Improved error message; full removal requires Wry |
| 1.2 Structured IPC | ✓ Done | `TauriMessage`, `TauriIpc` in `Ipc/` folder |
| 1.3 Error handling | ✓ Done | `TauriException.cs` |
| 1.4 Config builder | ❌ Deferred | Will implement during Wry swap |

---

## Phase 1: Quick Wins (Before Wry Swap)

### 1.1 Remove Hardcoded Custom Scheme Limit ⚠️ PARTIAL

**Current State:**
```csharp
// Hardcoded array of 16 in TauriNativeParameters (Photino.Native limitation)
[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 16)]
internal string[] CustomSchemeNames;
```

**Status:** Cannot fully remove without modifying Photino.Native. Improvements made:
- Better error message explaining the workaround (register after init)
- TODO marker for removal when switching to wry-ffi
- Schemes can be added after window creation via `Photino_AddCustomSchemeName`

**Full removal:** Deferred to Phase 3 (Wry swap)

**Files Modified:**
- `TauriNativeParameters.cs` - Added TODO comment
- `TauriNetDelegates.cs` - Improved error message with workaround explanation

### 1.2 Structured IPC Messages ✓ IMPLEMENTED

**Implementation:** `src/TauriCSharp/TauriCSharp/Ipc/`

```csharp
// TauriMessage - structured message with correlation
public class TauriMessage
{
    public string Type { get; set; }
    public string? Id { get; set; }           // Correlation ID
    public JsonElement? Payload { get; set; }
    public bool IsResponse { get; set; }
    public string? Error { get; set; }
}

// TauriIpc - request/response with handlers
var ipc = window.CreateIpc();
ipc.On("command", msg => handler(msg));
var response = await ipc.RequestAsync<T>("command", payload);

// Extension methods
window.SendMessage("type", payload);
```

**Features:**
- JSON serialization via System.Text.Json
- Request/response correlation via ID
- Timeout handling for requests
- Async/await support
- Raw string API preserved for backwards compatibility

### 1.3 Improve Error Handling ✓ IMPLEMENTED

**Implementation:** `src/TauriCSharp/TauriCSharp/TauriException.cs`

```csharp
public class TauriException : Exception { }

public class TauriInitializationException : TauriException
{
    public IReadOnlyList<string> ValidationErrors { get; }
}

public class TauriSchemeException : TauriException
{
    public string? Scheme { get; }
    public string? Url { get; }
}

public class TauriIpcException : TauriException
{
    public string? CorrelationId { get; }
}

public class TauriPlatformException : TauriException
{
    public string Platform { get; }
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

| File | Purpose | Status |
|------|---------|--------|
| `TauriApp.cs` | Application lifecycle, multi-window | Phase 3 |
| `TauriWindowBuilder.cs` | Configuration builder | Phase 2 |
| `Ipc/TauriMessage.cs` | Typed IPC messages | ✓ Created |
| `Ipc/TauriIpc.cs` | Request/response IPC manager | ✓ Created |
| `Ipc/TauriWindowIpcExtensions.cs` | Extension methods | ✓ Created |
| `TauriException.cs` | Structured errors | ✓ Created |
| `CommandRegistry.cs` | Command registration for IPC | Phase 3 |
| `Plugins/ITauriPlugin.cs` | Plugin interface | Phase 3 |
| `Plugins/DialogPlugin.cs` | File dialog implementation | Phase 3 |
| `Plugins/NotificationPlugin.cs` | Notification implementation | Phase 3 |
| `Plugins/ShellPlugin.cs` | Shell operations | Phase 3 |
| `Plugins/ClipboardPlugin.cs` | Clipboard operations | Phase 3 |

---

## Breaking Changes

Moving from Photino to tauri-csharp will have breaking changes:

1. **Namespace:** `Photino.NET` → `TauriCSharp`
2. **Class names:** `PhotinoWindow` → `TauriWindow`
3. **Custom scheme limit:** Removed (non-breaking, but API may change)
4. **Some method signatures:** Async variants, typed messages

Migration guide will be provided.
