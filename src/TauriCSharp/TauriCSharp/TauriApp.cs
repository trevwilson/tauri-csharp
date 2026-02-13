using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TauriCSharp.Interop;

namespace TauriCSharp;

/// <summary>
/// Central application lifecycle manager for multi-window TauriCSharp applications.
/// </summary>
/// <remarks>
/// <para>
/// For multi-window applications, use <see cref="TauriApp"/> to manage the event loop
/// and create windows. The single-window <see cref="TauriWindow.WaitForClose"/> pattern
/// continues to work unchanged for simple applications.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var app = TauriApp.Instance;
/// var main = app.CreateWindow().SetTitle("Main").Load("app://localhost/index.html");
/// var settings = app.CreateWindow().SetTitle("Settings").Load("app://localhost/settings.html");
/// app.Run();
/// </code>
/// </para>
/// </remarks>
public class TauriApp : IDisposable
{
    private static readonly Lazy<TauriApp> _instance = new(() => new TauriApp());

    /// <summary>
    /// Gets the singleton TauriApp instance.
    /// </summary>
    public static TauriApp Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, TauriWindow> _windows = new();
    private readonly ConcurrentBag<TauriWindow> _pendingWindows = new();
    private readonly ILogger? _logger;
    private bool _disposed;
    private bool _shouldQuit;

    /// <summary>
    /// Event loop callback - pinned for lifetime of event loop.
    /// </summary>
    private WryEventLoopCallback? _appEventLoopCallback;

    /// <summary>
    /// Creates a new TauriApp instance.
    /// </summary>
    /// <param name="logger">Optional logger for application-level logging.</param>
    private TauriApp(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new window managed by this application.
    /// </summary>
    /// <param name="logger">Optional logger for the window.</param>
    /// <returns>A new, unconfigured TauriWindow. Call fluent methods to configure, then call <see cref="Run"/>.</returns>
    public TauriWindow CreateWindow(ILogger? logger = null)
    {
        var window = new TauriWindow(logger, app: this);
        _pendingWindows.Add(window);
        return window;
    }

    /// <summary>
    /// Runs the application event loop. Blocks until all windows are closed or <see cref="Quit"/> is called.
    /// </summary>
    public void Run()
    {
        // Initialize all pending windows (CreateWindow adds to _pendingWindows,
        // InitializeForApp calls RegisterWindow which moves them to _windows)
        while (_pendingWindows.TryTake(out var window))
        {
            window.InitializeForApp();
        }

        if (_windows.IsEmpty)
            throw new InvalidOperationException("No windows have been initialized. Call CreateWindow() and configure windows before calling Run().");

        var eventLoop = TauriWindow.EnsureEventLoopStatic();
        var callbackRegistry = TauriWindow.CallbackRegistryStatic;

        _appEventLoopCallback = HandleEventLoopEvent;
        callbackRegistry.Register(IntPtr.Zero, _appEventLoopCallback);

        WryInterop.EventLoopPump(eventLoop.DangerousGetRawHandle(), _appEventLoopCallback, IntPtr.Zero);

        callbackRegistry.Unregister(IntPtr.Zero);
    }

    /// <summary>
    /// Requests the application to quit, closing all windows.
    /// </summary>
    public void Quit()
    {
        _shouldQuit = true;
        TauriWindow.EventLoopProxyStatic?.RequestExit();
    }

    /// <summary>
    /// Registers a window with the application.
    /// </summary>
    internal void RegisterWindow(string windowId, TauriWindow window)
    {
        _windows[windowId] = window;
    }

    /// <summary>
    /// Unregisters a window from the application.
    /// </summary>
    internal void UnregisterWindow(string windowId)
    {
        _windows.TryRemove(windowId, out _);
    }

    /// <summary>
    /// Handles events from the event loop, routing to the correct window.
    /// </summary>
    private WryEventLoopControlFlow HandleEventLoopEvent(IntPtr eventJsonPtr, IntPtr userData)
    {
        if (_shouldQuit)
            return WryEventLoopControlFlow.Exit;

        if (eventJsonPtr == IntPtr.Zero)
            return WryEventLoopControlFlow.Wait;

        try
        {
            var json = Marshal.PtrToStringUTF8(eventJsonPtr);
            if (string.IsNullOrEmpty(json))
                return WryEventLoopControlFlow.Wait;

            DispatchAppEvent(json);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling event in TauriApp");
        }

        if (_shouldQuit || _windows.IsEmpty)
            return WryEventLoopControlFlow.Exit;

        return WryEventLoopControlFlow.Wait;
    }

    /// <summary>
    /// Dispatches a JSON event to the appropriate window or handles it at the app level.
    /// </summary>
    private void DispatchAppEvent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var eventType = typeElement.GetString();
            var windowId = root.TryGetProperty("window_id", out var widEl) ? widEl.GetString() : null;

            // Handle global shortcut events at app level
            if (eventType == "global-shortcut")
            {
                if (root.TryGetProperty("id", out var shortcutIdEl))
                {
                    var shortcutId = (uint)shortcutIdEl.GetInt32();
                    GlobalShortcuts.DispatchShortcutEvent(shortcutId);
                }
                return;
            }

            // Route window-specific events
            if (windowId != null && _windows.TryGetValue(windowId, out var window))
            {
                window.DispatchEventFromApp(json);

                // Check if window was destroyed
                if (eventType is "window-destroyed" or "window-close-requested")
                {
                    if (window.ShouldExitFromApp)
                    {
                        UnregisterWindow(windowId);
                    }
                }
            }
            else if (windowId == null)
            {
                // Broadcast to all windows for non-window-specific events
                if (eventType is "loop-destroyed")
                {
                    _shouldQuit = true;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Failed to parse event JSON in TauriApp");
        }
    }

    /// <summary>
    /// Initializes a window at runtime (after Run() has been called).
    /// This allows creating additional windows from IPC handlers.
    /// </summary>
    public void InitializeWindow(TauriWindow window)
    {
        window.InitializeForApp();
    }

    /// <summary>
    /// Routes an event to the correct window when the event loop is driven by a
    /// single-window WaitForClose() but child windows exist via TauriApp.
    /// </summary>
    internal static void TryRouteEvent(string windowId, string json, string? eventType)
    {
        if (!_instance.IsValueCreated) return;
        var app = _instance.Value;

        if (!app._windows.TryGetValue(windowId, out var window))
            return;

        window.DispatchEventFromApp(json);

        // Handle window lifecycle - destroy if close was accepted
        if (eventType is "window-destroyed" or "window-close-requested")
        {
            if (window.ShouldExitFromApp)
            {
                app.UnregisterWindow(windowId);
                window.Dispose();
            }
        }
    }

    // ========================================================================
    // Cross-Window Communication
    // ========================================================================

    /// <summary>
    /// Gets a registered window by its native window identifier.
    /// </summary>
    /// <param name="windowId">The native window identifier string.</param>
    /// <returns>The window if found, null otherwise.</returns>
    public TauriWindow? GetWindow(string windowId)
    {
        _windows.TryGetValue(windowId, out var window);
        return window;
    }

    /// <summary>
    /// Gets all registered windows as a read-only snapshot.
    /// </summary>
    public IReadOnlyDictionary<string, TauriWindow> Windows =>
        new Dictionary<string, TauriWindow>(_windows);

    /// <summary>
    /// Sends a web message to a specific window's webview.
    /// </summary>
    /// <param name="windowId">The target window's native identifier.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>True if the window was found and the message was sent.</returns>
    public bool SendToWindow(string windowId, string message)
    {
        if (_windows.TryGetValue(windowId, out var window))
        {
            window.SendWebMessage(message);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Broadcasts a web message to all registered windows' webviews.
    /// </summary>
    /// <param name="message">The message to send to all windows.</param>
    public void Broadcast(string message)
    {
        foreach (var window in _windows.Values)
        {
            window.SendWebMessage(message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var window in _windows.Values)
        {
            window.Dispose();
        }
        _windows.Clear();

        GC.SuppressFinalize(this);
    }
}
