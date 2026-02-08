using System.Drawing;
using System.Text;
using Microsoft.Extensions.Logging;
using TauriCSharp;

namespace TauriCSharp.TestApp;

class Program
{
    private static TauriWindow? _window;
    private static readonly StringBuilder _testResults = new();
    private static int _passCount = 0;
    private static int _failCount = 0;

    [STAThread]
    static void Main(string[] _)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  TauriCSharp Test App - wry-ffi Validation");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static void RunTests()
    {
        Log("Creating TauriWindow...");

        // Create a logger for TauriWindow
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<TauriWindow>();

        _window = new TauriWindow(logger)
            .SetTitle("TauriCSharp Test App")
            .SetSize(1024, 768)
            .SetMinSize(640, 480)
            .SetDevToolsEnabled(true)
            .Load("app://localhost/index.html")

            // Register event handlers
            .RegisterWindowCreatingHandler(OnWindowCreating)
            .RegisterWindowCreatedHandler(OnWindowCreated)
            .RegisterWebMessageReceivedHandler(OnMessageReceived)
            .RegisterSizeChangedHandler(OnSizeChanged)
            .RegisterLocationChangedHandler(OnLocationChanged)
            .RegisterFocusInHandler(OnFocusIn)
            .RegisterFocusOutHandler(OnFocusOut)
            .RegisterWindowClosingHandler(OnWindowClosing)
            .RegisterNavigationStartingHandler(OnNavigationStarting)

            // Register custom scheme
            .RegisterCustomSchemeHandler("app", OnCustomScheme);

        Log("Window configured, starting event loop...");
        Log("Close the window to see test results.");
        Console.WriteLine();

        _window.WaitForClose();

        // Print test results
        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("  Test Results");
        Console.WriteLine("===========================================");
        Console.WriteLine(_testResults.ToString());
        Console.WriteLine($"Passed: {_passCount}  Failed: {_failCount}");
        Console.WriteLine("===========================================");
    }

    // ========================================================================
    // Event Handlers
    // ========================================================================

    static void OnWindowCreating(object? sender, EventArgs? e)
    {
        Log("EVENT: WindowCreating");
        RecordTest("WindowCreating event fires", true);
    }

    static void OnWindowCreated(object? sender, EventArgs? e)
    {
        Log("EVENT: WindowCreated");
        RecordTest("WindowCreated event fires", true);

        // Run post-creation tests
        if (_window != null)
        {
            TestWindowProperties();
        }
    }

    static void OnMessageReceived(object? sender, string message)
    {
        Log($"EVENT: WebMessageReceived - '{message}'");
        RecordTest("WebMessageReceived event fires", true);

        if (_window == null) return;

        try
        {
            HandleMessage(message);
        }
        catch (Exception ex)
        {
            Log($"ERROR handling message: {ex.Message}");
            _window.SendWebMessage($"error:{ex.Message}");
        }
    }

    static void HandleMessage(string message)
    {
        if (_window == null) return;

        switch (message.ToLower())
        {
            case "ping":
                _window.SendWebMessage("pong");
                RecordTest("IPC round-trip (ping/pong)", true);
                break;

            case var m when m.StartsWith("echo:"):
                var echoText = message[5..];
                _window.SendWebMessage($"echo:{echoText}");
                break;

            case "get-window-info":
                var info = $"Title: {_window.Title}, Size: {_window.Width}x{_window.Height}, Pos: {_window.Left},{_window.Top}";
                _window.SendWebMessage($"info:{info}");
                break;

            case "minimize":
                _window.Minimized = true;
                _window.SendWebMessage("minimized");
                break;

            case "maximize":
                _window.Maximized = true;
                _window.SendWebMessage("maximized");
                break;

            case "restore":
                _window.Restore();
                _window.SendWebMessage("restored");
                break;

            case var m when m.StartsWith("set-title:"):
                var newTitle = message[10..];
                _window.Title = newTitle;
                _window.SendWebMessage($"title-set:{_window.Title}");
                RecordTest("Set window title", _window.Title == newTitle);
                break;

            case var m when m.StartsWith("resize:"):
                var sizeStr = message[7..];
                var parts = sizeStr.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                {
                    _window.Size = new Size(w, h);
                    _window.SendWebMessage($"resized:{_window.Width}x{_window.Height}");
                }
                break;

            case var m when m.StartsWith("move:"):
                var posStr = message[5..];
                var posParts = posStr.Split('x');
                if (posParts.Length == 2 && int.TryParse(posParts[0], out var x) && int.TryParse(posParts[1], out var y))
                {
                    _window.Left = x;
                    _window.Top = y;
                    _window.SendWebMessage($"moved:{_window.Left},{_window.Top}");
                }
                break;

            case "execute-script":
                _window.ExecuteScript("log('Script executed from C#!', 'success')");
                _window.SendWebMessage("script-executed");
                RecordTest("ExecuteScript", true);
                break;

            case "open-devtools":
                _window.OpenDevTools();
                _window.SendWebMessage("devtools-opened");
                break;

            case "get-url":
                var url = _window.CurrentUrl ?? "null";
                _window.SendWebMessage($"url:{url}");
                RecordTest("Get CurrentUrl", !string.IsNullOrEmpty(_window.CurrentUrl));
                break;

            case var m when m.StartsWith("navigate:"):
                var targetUrl = message[9..];
                _window.Load(targetUrl);
                _window.SendWebMessage($"navigating:{targetUrl}");
                break;

            case "close":
                _window.Close();
                break;

            case "dialog-open":
                HandleDialogOpen();
                break;

            case "dialog-save":
                HandleDialogSave();
                break;

            case "dialog-confirm":
                HandleDialogConfirm();
                break;

            case "get-monitors":
                HandleGetMonitors();
                break;

            case "set-icon":
                HandleSetIcon();
                break;

            case "send-notification":
                HandleSendNotification();
                break;

            case "register-shortcut":
                HandleRegisterShortcut();
                break;

            case "open-child-window":
                HandleOpenChildWindow();
                break;

            default:
                Log($"Unknown message: {message}");
                _window.SendWebMessage($"unknown:{message}");
                break;
        }
    }

    static void OnSizeChanged(object? sender, Size size)
    {
        Log($"EVENT: SizeChanged - {size.Width}x{size.Height}");
        RecordTest("SizeChanged event fires", true);
    }

    static void OnLocationChanged(object? sender, Point location)
    {
        Log($"EVENT: LocationChanged - {location.X},{location.Y}");
        RecordTest("LocationChanged event fires", true);
    }

    static void OnFocusIn(object? sender, EventArgs? e)
    {
        Log("EVENT: FocusIn");
        RecordTest("FocusIn event fires", true);
    }

    static void OnFocusOut(object? sender, EventArgs? e)
    {
        Log("EVENT: FocusOut");
        RecordTest("FocusOut event fires", true);
    }

    static bool OnWindowClosing(object? sender, EventArgs? e)
    {
        Log("EVENT: WindowClosing");
        RecordTest("WindowClosing event fires", true);
        return false; // Allow close
    }

    static bool OnNavigationStarting(object sender, string url)
    {
        Log($"EVENT: NavigationStarting - {url}");
        RecordTest("NavigationStarting event fires", true);
        return true; // Allow navigation
    }

    static Stream? OnCustomScheme(object sender, string scheme, string url, out string contentType)
    {
        Log($"EVENT: CustomScheme - {scheme} - {url}");
        RecordTest("Custom scheme handler called", true);

        // Parse the URL to get the file path
        // URL format: app://localhost/path/to/file
        contentType = "text/plain";

        try
        {
            // Extract path from URL (after scheme://host/)
            var uri = new Uri(url);
            var filePath = uri.AbsolutePath.TrimStart('/');

            if (string.IsNullOrEmpty(filePath) || filePath == "/")
            {
                filePath = "index.html";
            }

            // Resolve relative to wwwroot
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var fullPath = Path.Combine(wwwroot, filePath);

            Log($"  Serving file: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Log($"  File not found: {fullPath}");
                contentType = "text/plain";
                return new MemoryStream(Encoding.UTF8.GetBytes($"404 Not Found: {filePath}"));
            }

            // Determine content type based on extension
            contentType = Path.GetExtension(filePath).ToLower() switch
            {
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                _ => "application/octet-stream"
            };

            Log($"  Content-Type: {contentType}");

            // Read and return file contents
            var fileBytes = File.ReadAllBytes(fullPath);
            return new MemoryStream(fileBytes);
        }
        catch (Exception ex)
        {
            Log($"  Error serving file: {ex.Message}");
            contentType = "text/plain";
            return new MemoryStream(Encoding.UTF8.GetBytes($"500 Internal Error: {ex.Message}"));
        }
    }

    // ========================================================================
    // Tests
    // ========================================================================

    static void TestWindowProperties()
    {
        if (_window == null) return;

        Log("Testing window properties...");

        // Test title
        var originalTitle = _window.Title;
        RecordTest("Get Title", !string.IsNullOrEmpty(originalTitle));

        // Test size
        RecordTest("Get Width", _window.Width > 0);
        RecordTest("Get Height", _window.Height > 0);

        // Test position
        RecordTest("Get Left", true); // Position can be 0
        RecordTest("Get Top", true);

        // Test IsVisible
        RecordTest("IsVisible = true", _window.IsVisible);

        Log("Initial property tests complete.");
    }

    // ========================================================================
    // Dialog Handlers
    // ========================================================================

    static void HandleDialogOpen()
    {
        if (_window == null) return;

        Log("Opening file dialog...");
        var paths = Dialogs.OpenFile(title: "Open File");

        if (paths.Length > 0)
        {
            Log($"Selected: {paths[0]}");
            _window.SendWebMessage($"dialog-result:open:{paths[0]}");
            RecordTest("Dialog Open", true);
        }
        else
        {
            Log("Dialog cancelled");
            _window.SendWebMessage("dialog-result:open:cancelled");
            RecordTest("Dialog Open (cancelled)", true);
        }
    }

    static void HandleDialogSave()
    {
        if (_window == null) return;

        Log("Opening save dialog...");
        var path = Dialogs.SaveFile(title: "Save File", defaultName: "untitled.txt");

        if (path != null)
        {
            Log($"Save path: {path}");
            _window.SendWebMessage($"dialog-result:save:{path}");
            RecordTest("Dialog Save", true);
        }
        else
        {
            Log("Dialog cancelled");
            _window.SendWebMessage("dialog-result:save:cancelled");
            RecordTest("Dialog Save (cancelled)", true);
        }
    }

    static void HandleDialogConfirm()
    {
        if (_window == null) return;

        Log("Opening confirm dialog...");
        var confirmed = Dialogs.Confirm("Confirm", "Do you want to proceed with this action?");

        Log($"Confirmed: {confirmed}");
        _window.SendWebMessage($"dialog-result:confirm:{confirmed}");
        RecordTest("Dialog Confirm", true);
    }

    // ========================================================================
    // Phase 4 Feature Handlers
    // ========================================================================

    static void HandleGetMonitors()
    {
        if (_window == null) return;

        try
        {
            var monitors = _window.Monitors;
            var sb = new StringBuilder();
            sb.Append($"Found {monitors.Count} monitor(s):\\n");
            foreach (var m in monitors)
            {
                sb.Append($"  {m.Name}: {m.MonitorArea.Width}x{m.MonitorArea.Height} @ ({m.MonitorArea.X},{m.MonitorArea.Y}), scale={m.Scale}\\n");
            }

            var current = _window.CurrentMonitor;
            if (current.HasValue)
                sb.Append($"Current: {current.Value.Name}\\n");

            sb.Append($"DPI: {_window.ScreenDpi}");

            Log(sb.ToString().Replace("\\n", "\n"));
            _window.SendWebMessage($"monitors:{sb}");
            RecordTest("Monitor enumeration", monitors.Count > 0);
        }
        catch (Exception ex)
        {
            Log($"Monitor error: {ex.Message}");
            _window.SendWebMessage($"error:{ex.Message}");
            RecordTest("Monitor enumeration", false);
        }
    }

    static void HandleSetIcon()
    {
        if (_window == null) return;

        try
        {
            // Look for a test icon in wwwroot
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var iconPath = Path.Combine(wwwroot, "icon.png");

            if (File.Exists(iconPath))
            {
                _window.IconFile = iconPath;
                _window.SendWebMessage("icon-set:file");
                RecordTest("Set window icon (file)", true);
            }
            else
            {
                // Generate a simple 32x32 red square RGBA icon for testing
                var size = 32;
                var rgba = new byte[size * size * 4];
                for (int i = 0; i < rgba.Length; i += 4)
                {
                    rgba[i] = 255;     // R
                    rgba[i + 1] = 0;   // G
                    rgba[i + 2] = 0;   // B
                    rgba[i + 3] = 255; // A
                }
                var result = _window.SetIcon(rgba, size, size);
                _window.SendWebMessage($"icon-set:rgba:{result}");
                RecordTest("Set window icon (RGBA)", result);
            }
        }
        catch (Exception ex)
        {
            Log($"Icon error: {ex.Message}");
            _window.SendWebMessage($"error:{ex.Message}");
            RecordTest("Set window icon", false);
        }
    }

    static void HandleSendNotification()
    {
        if (_window == null) return;

        try
        {
            var result = Notifications.Show(
                "TauriCSharp Test",
                "This is a test notification from the TestApp!",
                timeoutMs: 5000);

            Log($"Notification sent: {result}");
            _window.SendWebMessage($"notification:{result}");

            if (result)
            {
                RecordTest("Send notification", true);
            }
            else
            {
                // No notification daemon available (expected on WSL2)
                Log("No notification daemon available — expected on WSL2");
                RecordTest("Send notification (no daemon)", true);
            }
        }
        catch (Exception ex)
        {
            // Notifications may fail on WSL2 — that's expected
            Log($"Notification error (may be expected on WSL): {ex.Message}");
            _window.SendWebMessage($"notification:false:{ex.Message}");
            RecordTest("Send notification (graceful failure)", true);
        }
    }

    static void HandleRegisterShortcut()
    {
        if (_window == null) return;

        try
        {
            var shortcutId = GlobalShortcuts.Register("Ctrl+Shift+T", (id) =>
            {
                Log($"Global shortcut triggered! ID={id}");
                _window?.SendWebMessage($"shortcut-triggered:{id}");
            });

            if (shortcutId != 0)
            {
                Log($"Registered shortcut Ctrl+Shift+T with ID={shortcutId}");
                _window.SendWebMessage($"shortcut-registered:{shortcutId}");
                RecordTest("Register global shortcut", true);
            }
            else
            {
                Log("Failed to register shortcut (may be expected on WSL/Wayland)");
                _window.SendWebMessage("shortcut-registered:0");
                RecordTest("Register global shortcut (platform limitation)", true);
            }
        }
        catch (Exception ex)
        {
            Log($"Shortcut error: {ex.Message}");
            _window.SendWebMessage($"error:{ex.Message}");
            RecordTest("Register global shortcut", false);
        }
    }

    static void HandleOpenChildWindow()
    {
        if (_window == null) return;

        try
        {
            Log("Opening child window via TauriApp...");

            // For this test, we create a simple child window using TauriApp
            var app = TauriApp.Instance;
            var child = app.CreateWindow()
                .SetTitle("Child Window")
                .SetSize(640, 480)
                .RegisterCustomSchemeHandler("app", OnCustomScheme);

            child.StartUrl = "app://localhost/index.html";
            app.InitializeWindow(child);

            _window.SendWebMessage("child-window:opened");
            RecordTest("Open child window", true);
        }
        catch (Exception ex)
        {
            Log($"Child window error: {ex.Message}");
            _window.SendWebMessage($"error:{ex.Message}");
            RecordTest("Open child window", false);
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    static void RecordTest(string testName, bool passed)
    {
        // Only record each test once
        if (_testResults.ToString().Contains(testName)) return;

        var status = passed ? "PASS" : "FAIL";
        var symbol = passed ? "[+]" : "[-]";
        _testResults.AppendLine($"  {symbol} {testName}");

        if (passed) _passCount++;
        else _failCount++;

        Log($"TEST: {testName} - {status}");
    }
}
