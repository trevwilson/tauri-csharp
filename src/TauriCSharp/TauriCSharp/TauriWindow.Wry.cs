// TauriWindow implementation using Velox-based wry-ffi backend
// This partial class contains the wry-ffi specific implementation
// Updated for the new separate event loop, window, and webview architecture

using System.Runtime.InteropServices;
using System.Text.Json;
using TauriCSharp.Handles;
using TauriCSharp.Interop;

namespace TauriCSharp;

public partial class TauriWindow : IDisposable
{
    // ========================================================================
    // Velox-based wry-ffi handles and state
    // ========================================================================

    /// <summary>
    /// Static event loop shared across all windows.
    /// </summary>
    private static WryEventLoopHandle? _eventLoop;
    private static WryEventLoopProxyHandle? _eventLoopProxy;
    private static readonly object _eventLoopLock = new();

    /// <summary>
    /// Window handle for this instance.
    /// </summary>
    private IntPtr _wryWindow = IntPtr.Zero;

    /// <summary>
    /// Webview handle for this instance.
    /// </summary>
    private IntPtr _wryWebview = IntPtr.Zero;

    /// <summary>
    /// Window identifier from the native side.
    /// </summary>
    private string? _windowIdentifier;

    /// <summary>
    /// Protocol handler state - stored for the lifetime of the window.
    /// </summary>
    private ProtocolHandlerState? _protocolState;

    /// <summary>
    /// IPC handler state - stored for the lifetime of the window.
    /// </summary>
    private IpcHandlerState? _ipcState;

    /// <summary>
    /// Callback registry for pinning delegates to prevent GC collection.
    /// </summary>
    private static readonly WryCallbackRegistry _callbackRegistry = new();
    internal static WryCallbackRegistry CallbackRegistryStatic => _callbackRegistry;

    /// <summary>
    /// Event loop callback - pinned for lifetime of event loop.
    /// </summary>
    private static WryEventLoopCallback? _eventLoopCallback;

    /// <summary>
    /// Static delegate and function pointer for protocol response cleanup.
    /// Single instance reused across all requests since FreeResponseData is static.
    /// </summary>
    private static readonly WryCustomProtocolResponseFree _freeResponseDataCallback = FreeResponseData;
    private static readonly IntPtr _freeResponseDataPtr = Marshal.GetFunctionPointerForDelegate(_freeResponseDataCallback);

    /// <summary>
    /// The TauriApp that owns this window (null for single-window mode).
    /// </summary>
    private TauriApp? _ownerApp;

    /// <summary>
    /// Indicates if we're already disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Indicates if we should exit the event loop.
    /// </summary>
    private bool _shouldExit;

    // ========================================================================
    // Protocol Handler State
    // ========================================================================

    private sealed class ProtocolHandlerState
    {
        public required WryCustomProtocolHandler NativeCallback;
        public required GCHandle CallbackHandle;
        public required IntPtr SchemePtr;
        public required GCHandle DefinitionsHandle;
        public required IntPtr DefinitionsPtr;

        // Keep managed state alive
        public required WryCustomProtocolDefinition[] Definitions;

        public void Free()
        {
            if (CallbackHandle.IsAllocated) CallbackHandle.Free();
            if (SchemePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(SchemePtr);
            if (DefinitionsHandle.IsAllocated) DefinitionsHandle.Free();
        }
    }

    // ========================================================================
    // IPC Handler State
    // ========================================================================

    private sealed class IpcHandlerState
    {
        public required WryIpcHandler NativeCallback;
        public required GCHandle CallbackHandle;

        public void Free()
        {
            if (CallbackHandle.IsAllocated) CallbackHandle.Free();
        }
    }

    // ========================================================================
    // wry-ffi specific implementation methods
    // ========================================================================

    /// <summary>
    /// Ensures the event loop is created (singleton).
    /// </summary>
    private static WryEventLoopHandle EnsureEventLoop()
    {
        if (_eventLoop != null && !_eventLoop.IsInvalid)
            return _eventLoop;

        lock (_eventLoopLock)
        {
            if (_eventLoop != null && !_eventLoop.IsInvalid)
                return _eventLoop;

            _eventLoop = WryEventLoopHandle.Create();
            _eventLoopProxy = WryEventLoopProxyHandle.Create(_eventLoop);
            return _eventLoop;
        }
    }

    /// <summary>
    /// Static accessor for event loop (used by TauriApp).
    /// </summary>
    internal static WryEventLoopHandle EnsureEventLoopStatic() => EnsureEventLoop();

    /// <summary>
    /// Static accessor for event loop proxy (used by TauriApp).
    /// </summary>
    internal static WryEventLoopProxyHandle? EventLoopProxyStatic => _eventLoopProxy;

    /// <summary>
    /// Creates the window and webview.
    /// </summary>
    private void CreateWryWindow()
    {
        var eventLoop = EnsureEventLoop();

        // Create window config
        var windowConfig = BuildWindowConfig();

        // Create window
        _wryWindow = WryInterop.WindowBuild(eventLoop.DangerousGetRawHandle(), in windowConfig);
        FreeWindowConfig(ref windowConfig);

        if (_wryWindow == IntPtr.Zero)
        {
            throw new TauriInitializationException("Failed to create window");
        }

        // Get window identifier
        var idPtr = WryInterop.WindowIdentifier(_wryWindow);
        if (idPtr != IntPtr.Zero)
        {
            _windowIdentifier = Marshal.PtrToStringUTF8(idPtr);
        }

        // Create webview config (includes protocol handlers)
        var webviewConfig = BuildWebviewConfig();

        // Create webview
        _wryWebview = WryInterop.WebviewBuild(_wryWindow, in webviewConfig);

        // Free URL allocation from webview config
        if (webviewConfig.Url != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(webviewConfig.Url);
        }

        // Don't free webviewConfig protocol data yet - it needs to stay alive
        // The protocol state is stored in _protocolState

        if (_wryWebview == IntPtr.Zero)
        {
            WryInterop.WindowFree(_wryWindow);
            _wryWindow = IntPtr.Zero;
            _protocolState?.Free();
            _ipcState?.Free();
            throw new TauriInitializationException("Failed to create webview");
        }

        // Store as _nativeInstance for backward compatibility
        _nativeInstance = _wryWindow;
    }

    /// <summary>
    /// Builds window config from startup parameters.
    /// </summary>
    private WryWindowConfig BuildWindowConfig()
    {
        var p = _startupParameters;

        var config = WryWindowConfig.CreateDefault();
        config.Width = p.UseOsDefaultSize ? 800 : (uint)Math.Max(1, p.Width);
        config.Height = p.UseOsDefaultSize ? 600 : (uint)Math.Max(1, p.Height);

        if (!string.IsNullOrEmpty(p.Title))
        {
            config.Title = Marshal.StringToCoTaskMemUTF8(p.Title);
        }

        // Parent/modal window support
        if (_parentWindow != null && _parentWindow._wryWindow != IntPtr.Zero)
        {
            config.Parent = _parentWindow._wryWindow;
            config.Modal = _isModal;
        }

        return config;
    }

    /// <summary>
    /// Frees window config strings.
    /// </summary>
    private static void FreeWindowConfig(ref WryWindowConfig config)
    {
        if (config.Title != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(config.Title);
            config.Title = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Builds webview config with protocol handlers.
    /// </summary>
    private WryWebviewConfig BuildWebviewConfig()
    {
        var p = _startupParameters;
        var config = WryWebviewConfig.CreateDefault();

        // Set URL
        if (!string.IsNullOrEmpty(p.StartUrl))
        {
            config.Url = Marshal.StringToCoTaskMemUTF8(p.StartUrl);
        }

        // Set devtools
        config.Devtools = p.DevToolsEnabled;

        // Build protocol handlers if we have custom schemes
        if (CustomSchemes.Count > 0)
        {
            BuildProtocolConfig(ref config);
        }

        // Build IPC handler
        BuildIpcConfig(ref config);

        return config;
    }

    /// <summary>
    /// Builds the IPC handler configuration.
    /// </summary>
    private void BuildIpcConfig(ref WryWebviewConfig config)
    {
        // Create the native IPC callback
        WryIpcHandler nativeCallback = HandleIpcMessage;

        // Pin the callback to prevent GC
        var callbackHandle = GCHandle.Alloc(nativeCallback);
        var callbackPtr = Marshal.GetFunctionPointerForDelegate(nativeCallback);

        // Store state for cleanup
        _ipcState = new IpcHandlerState
        {
            NativeCallback = nativeCallback,
            CallbackHandle = callbackHandle,
        };

        // Set in config
        config.IpcHandler = callbackPtr;
        config.IpcUserData = IntPtr.Zero;
    }

    /// <summary>
    /// Handles an IPC message from the webview.
    /// </summary>
    private void HandleIpcMessage(IntPtr urlPtr, IntPtr messagePtr, IntPtr userData)
    {
        var message = messagePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(messagePtr) : null;
        if (!string.IsNullOrEmpty(message))
        {
            OnWebMessageReceived(message);
        }
    }

    /// <summary>
    /// Builds the protocol handler configuration.
    /// </summary>
    private void BuildProtocolConfig(ref WryWebviewConfig config)
    {
        // For now, we only support a single protocol handler (the first one)
        // In the future we could support multiple
        var firstScheme = CustomSchemes.Keys.First();

        // Create the native callback
        WryCustomProtocolHandler nativeCallback = HandleProtocolRequest;

        // Pin the callback
        var callbackHandle = GCHandle.Alloc(nativeCallback);
        var callbackPtr = Marshal.GetFunctionPointerForDelegate(nativeCallback);

        // Allocate scheme string
        var schemePtr = Marshal.StringToCoTaskMemUTF8(firstScheme);

        // Create definition
        var definitions = new WryCustomProtocolDefinition[]
        {
            new()
            {
                Scheme = schemePtr,
                Handler = callbackPtr,
                UserData = IntPtr.Zero,
            }
        };

        // Pin definitions array
        var definitionsHandle = GCHandle.Alloc(definitions, GCHandleType.Pinned);
        var definitionsPtr = definitionsHandle.AddrOfPinnedObject();

        // Store state for cleanup
        _protocolState = new ProtocolHandlerState
        {
            NativeCallback = nativeCallback,
            CallbackHandle = callbackHandle,
            SchemePtr = schemePtr,
            DefinitionsHandle = definitionsHandle,
            DefinitionsPtr = definitionsPtr,
            Definitions = definitions,
        };

        // Set in config
        config.CustomProtocols = new WryCustomProtocolList
        {
            Protocols = definitionsPtr,
            Count = 1,
        };
    }

    /// <summary>
    /// Handles a protocol request from the webview.
    /// </summary>
    private unsafe bool HandleProtocolRequest(IntPtr requestPtr, IntPtr responsePtr, IntPtr userData)
    {
        try
        {
            // Read request (blittable struct - direct pointer read avoids Marshal runtime marshalling)
            var request = System.Runtime.CompilerServices.Unsafe.Read<WryCustomProtocolRequest>((void*)requestPtr);
            var url = request.Url != IntPtr.Zero ? Marshal.PtrToStringUTF8(request.Url) : null;

            if (string.IsNullOrEmpty(url))
                return false;

            // Parse URL to get scheme
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return false;
            }

            // Find handler
            if (!CustomSchemes.TryGetValue(uri.Scheme, out var handler) || handler == null)
                return false;

            // Call handler
            var stream = handler.Invoke(this, uri.Scheme, url, out var contentType);
            if (stream == null)
                return false;

            // Read stream
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bodyBytes = ms.ToArray();
            stream.Dispose();

            // Allocate response body - Rust will call our free function
            var bodyPtr = Marshal.AllocHGlobal(bodyBytes.Length);
            Marshal.Copy(bodyBytes, 0, bodyPtr, bodyBytes.Length);

            // Allocate mime type
            var mimePtr = Marshal.StringToCoTaskMemUTF8(contentType ?? "application/octet-stream");

            // Pack both pointers into an allocation so FreeResponseData can free both
            var allocPtr = Marshal.AllocHGlobal(IntPtr.Size * 2);
            Marshal.WriteIntPtr(allocPtr, 0, bodyPtr);
            Marshal.WriteIntPtr(allocPtr, IntPtr.Size, mimePtr);

            // Build response using static free callback (single instance, no per-request allocation)
            var response = new WryCustomProtocolResponse
            {
                Status = 200,
                Headers = default,
                Body = new WryCustomProtocolBuffer { Ptr = bodyPtr, Len = (nuint)bodyBytes.Length },
                MimeType = mimePtr,
                Free = _freeResponseDataPtr,
                UserData = allocPtr,
            };

            // Write response (blittable struct - direct pointer write avoids Marshal runtime marshalling)
            System.Runtime.CompilerServices.Unsafe.Write((void*)responsePtr, response);

            return true;
        }
        catch (Exception ex)
        {
            if (_logger != null) TauriLog.ProtocolHandlerError(_logger, LogTitle, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Frees response data allocated by the protocol handler.
    /// userData points to a two-IntPtr block: [bodyPtr, mimePtr].
    /// </summary>
    private static void FreeResponseData(IntPtr userData)
    {
        if (userData != IntPtr.Zero)
        {
            var bodyPtr = Marshal.ReadIntPtr(userData, 0);
            var mimePtr = Marshal.ReadIntPtr(userData, IntPtr.Size);
            if (bodyPtr != IntPtr.Zero) Marshal.FreeHGlobal(bodyPtr);
            if (mimePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(mimePtr);
            Marshal.FreeHGlobal(userData);
        }
    }

    // ========================================================================
    // Event Loop Implementation
    // ========================================================================

    /// <summary>
    /// Runs the event loop until exit is requested.
    /// </summary>
    private void RunEventLoop()
    {
        var eventLoop = EnsureEventLoop();

        // Create and pin the callback
        _eventLoopCallback = HandleEventLoopEvent;
        _callbackRegistry.Register(_wryWindow, _eventLoopCallback);

        try
        {
            // Run the pump
            WryInterop.EventLoopPump(eventLoop.DangerousGetRawHandle(), _eventLoopCallback, IntPtr.Zero);
        }
        finally
        {
            _callbackRegistry.Unregister(_wryWindow);
        }
    }

    /// <summary>
    /// Handles events from the event loop.
    /// </summary>
    private WryEventLoopControlFlow HandleEventLoopEvent(IntPtr eventJsonPtr, IntPtr userData)
    {
        if (_shouldExit)
            return WryEventLoopControlFlow.Exit;

        if (eventJsonPtr == IntPtr.Zero)
            return WryEventLoopControlFlow.Wait;

        try
        {
            var json = Marshal.PtrToStringUTF8(eventJsonPtr);
            if (string.IsNullOrEmpty(json))
                return WryEventLoopControlFlow.Wait;

            // Parse and dispatch event
            DispatchEvent(json);
        }
        catch (Exception ex)
        {
            if (_logger != null) TauriLog.EventHandlingError(_logger, LogTitle, ex.Message);
        }

        return _shouldExit ? WryEventLoopControlFlow.Exit : WryEventLoopControlFlow.Wait;
    }

    /// <summary>
    /// Dispatches a JSON event to the appropriate handler.
    /// </summary>
    private void DispatchEvent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var eventType = typeElement.GetString();
            var windowId = root.TryGetProperty("window_id", out var widEl) ? widEl.GetString() : null;

            // Only process events for our window (or global events)
            if (windowId != null && _windowIdentifier != null && windowId != _windowIdentifier)
            {
                // Route to TauriApp for child/other windows
                TauriApp.TryRouteEvent(windowId, json, eventType);
                return;
            }

            switch (eventType)
            {
                case "window-close-requested":
                    var preventClose = OnWindowClosing();
                    if (preventClose == 0)
                    {
                        _shouldExit = true;
                        RestoreModalParent();
                    }
                    break;

                case "window-destroyed":
                    _shouldExit = true;
                    RestoreModalParent();
                    break;

                case "window-resized":
                    if (root.TryGetProperty("size", out var sizeEl))
                    {
                        var width = sizeEl.TryGetProperty("width", out var w) ? (int)w.GetDouble() : 0;
                        var height = sizeEl.TryGetProperty("height", out var h) ? (int)h.GetDouble() : 0;
                        OnSizeChanged(width, height);
                    }
                    break;

                case "window-moved":
                    if (root.TryGetProperty("position", out var posEl))
                    {
                        var x = posEl.TryGetProperty("x", out var xVal) ? (int)xVal.GetDouble() : 0;
                        var y = posEl.TryGetProperty("y", out var yVal) ? (int)yVal.GetDouble() : 0;
                        OnLocationChanged(x, y);
                    }
                    break;

                case "window-focused":
                    if (root.TryGetProperty("isFocused", out var focusedEl))
                    {
                        if (focusedEl.GetBoolean())
                            OnFocusIn();
                        else
                            OnFocusOut();
                    }
                    break;

                case "global-shortcut":
                    if (root.TryGetProperty("id", out var shortcutIdEl)
                        && shortcutIdEl.TryGetUInt32(out var shortcutId))
                    {
                        GlobalShortcuts.DispatchShortcutEvent(shortcutId);
                    }
                    break;

                case "user-exit":
                    _shouldExit = true;
                    break;

                case "loop-destroyed":
                    _shouldExit = true;
                    break;

                // These events don't need handling for basic functionality
                case "new-events":
                case "main-events-cleared":
                case "redraw-events-cleared":
                case "window-redraw-requested":
                case "window-cursor-moved":
                case "window-cursor-entered":
                case "window-cursor-left":
                case "window-modifiers-changed":
                    break;

                default:
                    // Log unknown events for debugging (Trace level)
                    if (_logger != null) TauriLog.UnhandledEvent(_logger, LogTitle, eventType ?? "unknown");
                    break;
            }
        }
        catch (JsonException ex)
        {
            if (_logger != null) TauriLog.JsonParseError(_logger, LogTitle, ex.Message);
        }
    }

    // ========================================================================
    // Multi-Window Support (TauriApp integration)
    // ========================================================================

    /// <summary>
    /// Initializes this window for use with TauriApp (multi-window mode).
    /// Creates the window and webview without starting the event loop.
    /// </summary>
    internal void InitializeForApp()
    {
        if (_nativeInstance != IntPtr.Zero)
            return; // Already initialized

        var errors = _startupParameters.GetParamErrors();
        if (errors.Count > 0)
            throw new TauriInitializationException("Window startup parameters are not valid.", errors);

        OnWindowCreating();
        CreateWryWindow();
        OnWindowCreated();

        // Register with the app
        if (_ownerApp != null && _windowIdentifier != null)
        {
            _ownerApp.RegisterWindow(_windowIdentifier, this);
        }
    }

    /// <summary>
    /// Dispatches a JSON event from TauriApp to this window.
    /// </summary>
    internal void DispatchEventFromApp(string json)
    {
        DispatchEvent(json);
    }

    /// <summary>
    /// Gets whether this window should exit (used by TauriApp to know when to unregister).
    /// </summary>
    internal bool ShouldExitFromApp => _shouldExit;

    /// <summary>
    /// Re-enables the parent window when a modal child closes.
    /// </summary>
    private void RestoreModalParent()
    {
        if (_isModal && _parentWindow != null && _parentWindow._wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetEnabled(_parentWindow._wryWindow, true);
            WryInterop.WindowFocus(_parentWindow._wryWindow);
            _parentWindow = null;
        }
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
            if (_wryWindow != IntPtr.Zero)
            {
                _callbackRegistry.Unregister(_wryWindow);
            }

            // Free protocol state
            _protocolState?.Free();
            _protocolState = null;

            // Free IPC state
            _ipcState?.Free();
            _ipcState = null;

            // Unmanaged cleanup — only safe during explicit disposal.
            // From the finalizer, the native library may already be unloaded.
            if (_wryWebview != IntPtr.Zero)
            {
                WryInterop.WebviewFree(_wryWebview);
                _wryWebview = IntPtr.Zero;
            }

            if (_wryWindow != IntPtr.Zero)
            {
                WryInterop.WindowFree(_wryWindow);
                _wryWindow = IntPtr.Zero;
                _nativeInstance = IntPtr.Zero;
            }
        }

        _disposed = true;
    }

    ~TauriWindow()
    {
        Dispose(disposing: false);
    }
}
