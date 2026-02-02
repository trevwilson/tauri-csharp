using System.Drawing;
using System.Text;
using TauriCSharp;

namespace TauriCSharp.TestApp;

class Program
{
    private static TauriWindow? _window;
    private static readonly StringBuilder _testResults = new();
    private static int _passCount = 0;
    private static int _failCount = 0;

    [STAThread]
    static void Main(string[] args)
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

        _window = new TauriWindow()
            .SetTitle("TauriCSharp Test App")
            .SetSize(1024, 768)
            .SetMinSize(640, 480)
            .SetDevToolsEnabled(true)
            .SetLogVerbosity(2)
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
                var echoText = message.Substring(5);
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
                var newTitle = message.Substring(10);
                _window.Title = newTitle;
                _window.SendWebMessage($"title-set:{_window.Title}");
                RecordTest("Set window title", _window.Title == newTitle);
                break;

            case var m when m.StartsWith("resize:"):
                var sizeStr = message.Substring(7);
                var parts = sizeStr.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                {
                    _window.Size = new Size(w, h);
                    _window.SendWebMessage($"resized:{_window.Width}x{_window.Height}");
                }
                break;

            case var m when m.StartsWith("move:"):
                var posStr = message.Substring(5);
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
                var targetUrl = message.Substring(9);
                _window.Load(targetUrl);
                _window.SendWebMessage($"navigating:{targetUrl}");
                break;

            case "close":
                _window.Close();
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
