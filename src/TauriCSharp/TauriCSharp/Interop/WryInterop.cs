// wry-ffi C# bindings - P/Invoke declarations
// All FFI functions from the wry-ffi crate

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// P/Invoke declarations for wry-ffi native library.
/// Uses DllImport for methods involving structs with bool fields (runtime marshalling required).
/// </summary>
internal static partial class WryInterop
{
    private const string LibraryName = "wry_ffi";

    // ==========================================================================
    // App Lifecycle
    // ==========================================================================

    /// <summary>
    /// Initialize the application. Must be called first, on main thread.
    /// </summary>
    /// <returns>App handle or NULL on failure</returns>
    [LibraryImport(LibraryName, EntryPoint = "wry_app_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr AppCreate();

    /// <summary>
    /// Run the event loop. Blocks until all windows closed or wry_app_quit called.
    /// Must be called on main thread.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_app_run", CallingConvention = CallingConvention.Cdecl)]
    public static extern WryResult AppRun(IntPtr app);

    /// <summary>
    /// Request app to quit.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_app_quit")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void AppQuit(IntPtr app);

    /// <summary>
    /// Destroy app and free resources.
    /// The app handle must not be used after this call.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_app_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void AppDestroy(IntPtr app);

    /// <summary>
    /// Get last error message (valid until next wry_* call).
    /// </summary>
    /// <returns>Pointer to error string (do not free)</returns>
    [LibraryImport(LibraryName, EntryPoint = "wry_get_last_error")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr GetLastError();

    /// <summary>
    /// Get version string.
    /// </summary>
    /// <returns>Pointer to version string (do not free)</returns>
    [LibraryImport(LibraryName, EntryPoint = "wry_version")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr GetVersion();

    // ==========================================================================
    // Window Management
    // ==========================================================================

    /// <summary>
    /// Create a new window with webview.
    /// Must be called on main thread with a valid app handle.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_window_create", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WindowCreate(IntPtr app, in WryWindowParams parameters);

    /// <summary>
    /// Destroy window and free resources.
    /// Thread-safe - dispatches via event loop.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowDestroy(IntPtr window);

    // ==========================================================================
    // WebView Operations
    // ==========================================================================

    /// <summary>
    /// Navigate to URL.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_webview_navigate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern WryResult WebViewNavigate(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

    /// <summary>
    /// Load HTML content directly.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_webview_load_html", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern WryResult WebViewLoadHtml(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string html);

    /// <summary>
    /// Execute JavaScript in webview context.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_webview_evaluate_script", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern WryResult WebViewEvaluateScript(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string script);

    /// <summary>
    /// Send message to JavaScript (calls window.tauri.__receive).
    /// Thread-safe - dispatches via event loop.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_webview_send_message", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern WryResult WebViewSendMessage(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    /// <summary>
    /// Get current URL. Caller must free with StringFree.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_webview_get_url")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WebViewGetUrl(IntPtr window);

    /// <summary>
    /// Set zoom level (1.0 = 100%).
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_webview_set_zoom")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WebViewSetZoom(IntPtr window, double zoom);

    /// <summary>
    /// Open devtools (if enabled).
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_webview_open_devtools")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WebViewOpenDevtools(IntPtr window);

    /// <summary>
    /// Close devtools.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_webview_close_devtools")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WebViewCloseDevtools(IntPtr window);

    // ==========================================================================
    // Window Operations
    // ==========================================================================

    /// <summary>
    /// Show/hide window.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_visible")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetVisible(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool visible);

    /// <summary>
    /// Get window visibility.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_is_visible")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsVisible(IntPtr window);

    /// <summary>
    /// Set window title.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_title", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetTitle(IntPtr window, string title);

    /// <summary>
    /// Get window title. Caller must free with StringFree.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_get_title")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowGetTitle(IntPtr window);

    /// <summary>
    /// Set window size.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetSize(IntPtr window, WrySize size);

    /// <summary>
    /// Get window size.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_get_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial WrySize WindowGetSize(IntPtr window);

    /// <summary>
    /// Set window position.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetPosition(IntPtr window, WryPosition position);

    /// <summary>
    /// Get window position.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_get_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial WryPosition WindowGetPosition(IntPtr window);

    /// <summary>
    /// Minimize window.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_minimize")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowMinimize(IntPtr window);

    /// <summary>
    /// Maximize window.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_maximize")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowMaximize(IntPtr window);

    /// <summary>
    /// Unmaximize window.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_unmaximize")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowUnmaximize(IntPtr window);

    /// <summary>
    /// Set fullscreen mode.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_fullscreen")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetFullscreen(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool fullscreen);

    /// <summary>
    /// Focus window.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_focus")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowFocus(IntPtr window);

    /// <summary>
    /// Close window.
    /// Thread-safe - dispatches via event loop.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_close")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowClose(IntPtr window);

    // ==========================================================================
    // Callbacks
    // ==========================================================================

    /// <summary>
    /// Set callback for web messages.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_message_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetMessageCallback(IntPtr window, WebMessageCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Set callback for window closing.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_closing_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetClosingCallback(IntPtr window, WindowClosingCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Set callback for window resize.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_resized_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetResizedCallback(IntPtr window, WindowResizedCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Set callback for window move.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_moved_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetMovedCallback(IntPtr window, WindowMovedCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Set callback for focus change.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_focus_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetFocusCallback(IntPtr window, WindowFocusCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Set callback for navigation (can cancel).
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_window_set_navigation_callback")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowSetNavigationCallback(IntPtr window, NavigationCallbackNative callback, IntPtr userData);

    // ==========================================================================
    // Dispatch
    // ==========================================================================

    /// <summary>
    /// Execute callback on UI thread (thread-safe, can be called from any thread).
    /// The callback will be queued and executed asynchronously.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_invoke")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void Invoke(IntPtr app, InvokeCallbackNative callback, IntPtr userData);

    /// <summary>
    /// Execute callback on UI thread and wait for completion.
    /// WARNING: Do not call this from the UI thread as it will deadlock.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_invoke_sync")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void InvokeSync(IntPtr app, InvokeCallbackNative callback, IntPtr userData);

    // ==========================================================================
    // Protocol
    // ==========================================================================

    /// <summary>
    /// Register custom protocol handler (e.g., "app" for app://...).
    /// Must be called before window creation.
    /// </summary>
    [DllImport(LibraryName, EntryPoint = "wry_register_protocol", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern WryResult RegisterProtocol(IntPtr app, [MarshalAs(UnmanagedType.LPUTF8Str)] string scheme, CustomProtocolCallbackNative callback, IntPtr userData);

    // ==========================================================================
    // String Management
    // ==========================================================================

    /// <summary>
    /// Free a string allocated by wry-ffi.
    /// Must be called for strings returned by wry_webview_get_url, wry_window_get_title, etc.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "wry_string_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void StringFree(IntPtr str);
}
