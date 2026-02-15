# TestApp

Interactive test application for TauriCSharp. Supports both manual and automated testing.

## Running Tests

```bash
# Manual mode (interactive — click buttons to test features)
dotnet run --project src/TauriCSharp/TauriCSharp.TestApp/

# Automated mode (self-driving via IPC)
dotnet run --project src/TauriCSharp/TauriCSharp.TestApp/ -- --autorun

# Headless CI (requires xvfb on Linux)
xvfb-run dotnet run --project src/TauriCSharp/TauriCSharp.TestApp/ -- --autorun
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All automated tests passed |
| 1 | One or more tests failed |
| 2 | Timeout (30 seconds without completion) |

## Autorun Architecture

The `--autorun` flag injects a JS bootstrap via `ExecuteScript` that polls until `window.ipc.postMessage` and `sendMessage()` are available, then sends `autorun-start` through the real IPC path. Each test command is sent via `sendMessage('command')` followed by a delayed `sendMessage('autorun-next')` to advance the sequence. This exercises the full IPC pipeline:

```
C# ExecuteScript("sendMessage('...')")
  → JS sendMessage()
    → window.ipc.postMessage()
      → Rust IPC handler
        → C# OnMessageReceived → HandleMessage()
```

The only thing not exercised is the browser `onclick` event from button clicks, which is standard browser behavior.

## Skipped Tests (require manual testing)

| Test | Reason |
|------|--------|
| dialog-open | Native OS dialog blocks thread, requires user interaction |
| dialog-save | Native OS dialog blocks thread, requires user interaction |
| dialog-confirm | Native OS dialog blocks thread, requires user interaction |
| open-modal-window | Blocks parent window input, no programmatic dismiss |
| navigate | Navigates away from test page, destroys JS context |
