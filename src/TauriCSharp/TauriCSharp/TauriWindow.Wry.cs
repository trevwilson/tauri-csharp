// TauriWindow implementation using wry-ffi backend
// This partial class contains the wry-ffi specific implementation

using System.Runtime.InteropServices;
using TauriCSharp.Handles;
using TauriCSharp.Interop;

namespace TauriCSharp;

public partial class TauriWindow : IDisposable
{
    // ========================================================================
    // wry-ffi handles and state
    // ========================================================================

    /// <summary>
    /// Static app handle shared across all windows (there's one event loop).
    /// </summary>
    private static WryAppHandle? _wryApp;
    private static readonly object _wryAppLock = new();

    /// <summary>
    /// Window handle for this instance.
    /// </summary>
    private IntPtr _wryWindow = IntPtr.Zero;

    /// <summary>
    /// Callback registry for pinning delegates to prevent GC collection.
    /// </summary>
    private static readonly WryCallbackRegistry _callbackRegistry = new();

    /// <summary>
    /// Indicates if we're already disposed.
    /// </summary>
    private bool _disposed;

    // ========================================================================
    // Callback delegates - stored as fields to prevent GC collection
    // ========================================================================

    private WebMessageCallbackNative? _messageCallback;
    private WindowClosingCallbackNative? _closingCallback;
    private WindowResizedCallbackNative? _resizedCallback;
    private WindowMovedCallbackNative? _movedCallback;
    private WindowFocusCallbackNative? _focusCallback;
    private NavigationCallbackNative? _navigationCallback;

    // ========================================================================
    // wry-ffi specific implementation methods
    // ========================================================================

    /// <summary>
    /// Ensures the wry app is created (singleton).
    /// </summary>
    private static WryAppHandle EnsureWryApp()
    {
        if (_wryApp != null && !_wryApp.IsInvalid)
            return _wryApp;

        lock (_wryAppLock)
        {
            if (_wryApp != null && !_wryApp.IsInvalid)
                return _wryApp;

            _wryApp = WryAppHandle.Create();
            return _wryApp;
        }
    }

    /// <summary>
    /// Converts TauriNativeParameters to WryWindowParams.
    /// </summary>
    private WryWindowParams ConvertToWryParams()
    {
        var p = _startupParameters;

        // Allocate strings - they must stay alive for the duration of window creation
        using var title = new MarshalledUtf8String(p.Title);
        using var url = new MarshalledUtf8String(p.StartUrl);
        using var html = new MarshalledUtf8String(p.StartString);
        using var userAgent = new MarshalledUtf8String(p.UserAgent);
        using var dataDir = new MarshalledUtf8String(p.TemporaryFilesPath);

        return new WryWindowParams
        {
            Title = title.Pointer,
            Url = url.Pointer,
            Html = html.Pointer,
            UserAgent = userAgent.Pointer,
            DataDirectory = dataDir.Pointer,
            X = p.Left,
            Y = p.Top,
            Width = p.UseOsDefaultSize ? 800 : (uint)Math.Max(0, p.Width),
            Height = p.UseOsDefaultSize ? 600 : (uint)Math.Max(0, p.Height),
            MinWidth = (uint)Math.Max(0, p.MinWidth),
            MinHeight = (uint)Math.Max(0, p.MinHeight),
            MaxWidth = p.MaxWidth == int.MaxValue ? 0 : (uint)Math.Max(0, p.MaxWidth),
            MaxHeight = p.MaxHeight == int.MaxValue ? 0 : (uint)Math.Max(0, p.MaxHeight),
            Resizable = p.Resizable,
            Fullscreen = p.FullScreen,
            Maximized = p.Maximized,
            Minimized = p.Minimized,
            Visible = true,
            Transparent = p.Transparent,
            Decorations = !p.Chromeless,
            AlwaysOnTop = p.Topmost,
            DevtoolsEnabled = p.DevToolsEnabled,
            AutoplayEnabled = p.MediaAutoplayEnabled,
        };
    }

    /// <summary>
    /// Creates the wry window and registers callbacks.
    /// </summary>
    private void CreateWryWindow()
    {
        var app = EnsureWryApp();

        // Build params - note we need to keep strings alive during creation
        var wryParams = BuildWryParams();

        _wryWindow = WryInterop.WindowCreate(app.DangerousGetRawHandle(), in wryParams);
        if (_wryWindow == IntPtr.Zero)
        {
            var error = Marshal.PtrToStringUTF8(WryInterop.GetLastError());
            throw new TauriInitializationException($"Failed to create window: {error}");
        }

        // Register callbacks after window creation
        RegisterCallbacks();

        // Store as _nativeInstance for backward compatibility
        _nativeInstance = _wryWindow;
    }

    /// <summary>
    /// Builds WryWindowParams with proper string lifetime management.
    /// </summary>
    private WryWindowParams BuildWryParams()
    {
        var p = _startupParameters;

        var wryParams = WryWindowParams.CreateDefault();

        // Set numeric values
        wryParams.X = p.UseOsDefaultLocation ? 0 : p.Left;
        wryParams.Y = p.UseOsDefaultLocation ? 0 : p.Top;
        wryParams.Width = p.UseOsDefaultSize ? 800 : (uint)Math.Max(1, p.Width);
        wryParams.Height = p.UseOsDefaultSize ? 600 : (uint)Math.Max(1, p.Height);
        wryParams.MinWidth = (uint)Math.Max(0, p.MinWidth);
        wryParams.MinHeight = (uint)Math.Max(0, p.MinHeight);
        wryParams.MaxWidth = p.MaxWidth == int.MaxValue ? 0 : (uint)Math.Max(0, p.MaxWidth);
        wryParams.MaxHeight = p.MaxHeight == int.MaxValue ? 0 : (uint)Math.Max(0, p.MaxHeight);

        // Set flags
        wryParams.Resizable = p.Resizable;
        wryParams.Fullscreen = p.FullScreen;
        wryParams.Maximized = p.Maximized;
        wryParams.Minimized = p.Minimized;
        wryParams.Visible = true;
        wryParams.Transparent = p.Transparent;
        wryParams.Decorations = !p.Chromeless;
        wryParams.AlwaysOnTop = p.Topmost;
        wryParams.DevtoolsEnabled = p.DevToolsEnabled;
        wryParams.AutoplayEnabled = p.MediaAutoplayEnabled;

        // Strings need special handling - allocated and passed as IntPtr
        // Note: These pointers are only valid during the WindowCreate call
        // The native side copies them immediately
        if (!string.IsNullOrEmpty(p.Title))
            wryParams.Title = Marshal.StringToCoTaskMemUTF8(p.Title);
        if (!string.IsNullOrEmpty(p.StartUrl))
            wryParams.Url = Marshal.StringToCoTaskMemUTF8(p.StartUrl);
        if (!string.IsNullOrEmpty(p.StartString))
            wryParams.Html = Marshal.StringToCoTaskMemUTF8(p.StartString);
        if (!string.IsNullOrEmpty(p.UserAgent))
            wryParams.UserAgent = Marshal.StringToCoTaskMemUTF8(p.UserAgent);
        if (!string.IsNullOrEmpty(p.TemporaryFilesPath))
            wryParams.DataDirectory = Marshal.StringToCoTaskMemUTF8(p.TemporaryFilesPath);

        return wryParams;
    }

    /// <summary>
    /// Frees string pointers in WryWindowParams after window creation.
    /// </summary>
    private static void FreeWryParams(ref WryWindowParams p)
    {
        if (p.Title != IntPtr.Zero) Marshal.FreeCoTaskMem(p.Title);
        if (p.Url != IntPtr.Zero) Marshal.FreeCoTaskMem(p.Url);
        if (p.Html != IntPtr.Zero) Marshal.FreeCoTaskMem(p.Html);
        if (p.UserAgent != IntPtr.Zero) Marshal.FreeCoTaskMem(p.UserAgent);
        if (p.DataDirectory != IntPtr.Zero) Marshal.FreeCoTaskMem(p.DataDirectory);
    }

    /// <summary>
    /// Registers all callbacks with the native window.
    /// Callbacks are pinned in the registry to prevent GC collection.
    /// </summary>
    private void RegisterCallbacks()
    {
        // Message callback
        _messageCallback = OnWryMessageReceived;
        _callbackRegistry.Register(_wryWindow, _messageCallback);
        WryInterop.WindowSetMessageCallback(_wryWindow, _messageCallback, IntPtr.Zero);

        // Closing callback
        _closingCallback = OnWryClosing;
        _callbackRegistry.Register(_wryWindow, _closingCallback);
        WryInterop.WindowSetClosingCallback(_wryWindow, _closingCallback, IntPtr.Zero);

        // Resized callback
        _resizedCallback = OnWryResized;
        _callbackRegistry.Register(_wryWindow, _resizedCallback);
        WryInterop.WindowSetResizedCallback(_wryWindow, _resizedCallback, IntPtr.Zero);

        // Moved callback
        _movedCallback = OnWryMoved;
        _callbackRegistry.Register(_wryWindow, _movedCallback);
        WryInterop.WindowSetMovedCallback(_wryWindow, _movedCallback, IntPtr.Zero);

        // Focus callback
        _focusCallback = OnWryFocusChanged;
        _callbackRegistry.Register(_wryWindow, _focusCallback);
        WryInterop.WindowSetFocusCallback(_wryWindow, _focusCallback, IntPtr.Zero);

        // Navigation callback
        _navigationCallback = OnWryNavigationStarting;
        _callbackRegistry.Register(_wryWindow, _navigationCallback);
        WryInterop.WindowSetNavigationCallback(_wryWindow, _navigationCallback, IntPtr.Zero);
    }

    // ========================================================================
    // Native callback handlers
    // ========================================================================

    private void OnWryMessageReceived(IntPtr window, IntPtr messagePtr, IntPtr userData)
    {
        var message = messagePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(messagePtr) : null;
        if (message != null)
        {
            OnWebMessageReceived(message);
        }
    }

    private bool OnWryClosing(IntPtr window, IntPtr userData)
    {
        // OnWindowClosing returns 1 (true) if close should be prevented
        var preventClose = OnWindowClosing();
        return preventClose == 0; // Return true to allow close
    }

    private void OnWryResized(IntPtr window, uint width, uint height, IntPtr userData)
    {
        OnSizeChanged((int)width, (int)height);
    }

    private void OnWryMoved(IntPtr window, int x, int y, IntPtr userData)
    {
        OnLocationChanged(x, y);
    }

    private void OnWryFocusChanged(IntPtr window, bool focused, IntPtr userData)
    {
        if (focused)
            OnFocusIn();
        else
            OnFocusOut();
    }

    private bool OnWryNavigationStarting(IntPtr window, IntPtr urlPtr, IntPtr userData)
    {
        var url = urlPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(urlPtr) : null;
        if (url == null)
            return true; // Allow navigation if URL is null

        return OnNavigationStarting(url);
    }

    // ========================================================================
    // Invoke implementation using wry_invoke_sync
    // ========================================================================

    /// <summary>
    /// Dispatches an Action to the UI thread using wry_invoke_sync.
    /// This version properly pins the delegate for the duration of the call.
    /// </summary>
    private TauriWindow InvokeWry(Action workItem)
    {
        if (_wryApp == null || _wryApp.IsInvalid)
        {
            // No app yet, just execute directly
            workItem();
            return this;
        }

        if (Environment.CurrentManagedThreadId == _managedThreadId)
        {
            // Already on UI thread
            workItem();
        }
        else
        {
            // Need to dispatch to UI thread
            InvokeCallbackNative callback = _ => workItem();
            using var pinned = new PinnedDelegate(callback);
            WryInterop.InvokeSync(_wryApp.DangerousGetRawHandle(), callback, IntPtr.Zero);
        }
        return this;
    }

    // ========================================================================
    // IDisposable implementation
    // ========================================================================

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected disposal method.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Managed cleanup
            // Unregister callbacks
            if (_wryWindow != IntPtr.Zero)
            {
                _callbackRegistry.Unregister(_wryWindow);
            }
        }

        // Unmanaged cleanup
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowDestroy(_wryWindow);
            _wryWindow = IntPtr.Zero;
            _nativeInstance = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~TauriWindow()
    {
        Dispose(disposing: false);
    }
}
