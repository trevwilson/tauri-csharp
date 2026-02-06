// wry-ffi C# bindings - P/Invoke declarations
// All FFI functions from the Velox-based wry-ffi crate

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// P/Invoke declarations for wry-ffi native library.
/// Based on Velox's runtime-wry-ffi with wry_* function names.
/// </summary>
internal static partial class WryInterop
{
    private const string WryLib = "wry_ffi";

    // ==========================================================================
    // Version/ABI Info
    // ==========================================================================

    /// <summary>
    /// Get the ABI version number for compatibility checking.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_ffi_abi_version")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial uint FfiAbiVersion();

    /// <summary>
    /// Get the library name.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_library_name")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr LibraryName();

    /// <summary>
    /// Get the crate version string.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_crate_version")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr CrateVersion();

    /// <summary>
    /// Get the webview version string.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_webview_version")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WebViewVersion();

    // ==========================================================================
    // Event Loop Creation/Destruction
    // ==========================================================================

    /// <summary>
    /// Create a new event loop. Must be called on main thread.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_new")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr EventLoopNew();

    /// <summary>
    /// Free the event loop.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void EventLoopFree(IntPtr eventLoop);

    /// <summary>
    /// Create a proxy handle for the event loop (for cross-thread communication).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_create_proxy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr EventLoopCreateProxy(IntPtr eventLoop);

    /// <summary>
    /// Request exit via proxy.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_proxy_request_exit")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopProxyRequestExit(IntPtr proxy);

    /// <summary>
    /// Send a user event via proxy.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_proxy_send_user_event", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopProxySendUserEvent(IntPtr proxy, string? payload);

    /// <summary>
    /// Free the proxy handle.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_proxy_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void EventLoopProxyFree(IntPtr proxy);

    /// <summary>
    /// Run the event loop, calling the callback for each event.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_pump")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void EventLoopPump(IntPtr eventLoop, WryEventLoopCallback callback, IntPtr userData);

    // ==========================================================================
    // macOS-specific Event Loop Functions
    // ==========================================================================

    /// <summary>
    /// Set macOS activation policy.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_set_activation_policy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopSetActivationPolicy(IntPtr eventLoop, WryActivationPolicy policy);

    /// <summary>
    /// Set macOS dock visibility.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_set_dock_visibility")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopSetDockVisibility(IntPtr eventLoop, [MarshalAs(UnmanagedType.U1)] bool visible);

    /// <summary>
    /// Hide macOS application.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_hide_application")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopHideApplication(IntPtr eventLoop);

    /// <summary>
    /// Show macOS application.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_event_loop_show_application")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool EventLoopShowApplication(IntPtr eventLoop);

    /// <summary>
    /// Force app state to launched (testing helper).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_app_state_force_launched")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void AppStateForceLaunched();

    // ==========================================================================
    // Window Creation/Destruction
    // ==========================================================================

    /// <summary>
    /// Create a new window.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_build")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr WindowBuild(IntPtr eventLoop, in WryWindowConfig config);

    /// <summary>
    /// Free a window.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WindowFree(IntPtr window);

    /// <summary>
    /// Get window identifier (do not free).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_identifier")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowIdentifier(IntPtr window);

    // ==========================================================================
    // Window Properties - Setters
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_title", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetTitle(IntPtr window, string title);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_fullscreen")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetFullscreen(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool fullscreen);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_decorations")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetDecorations(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool decorations);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_always_on_bottom")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetAlwaysOnBottom(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool onBottom);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_visible_on_all_workspaces")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetVisibleOnAllWorkspaces(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_content_protected")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetContentProtected(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool protected_);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_resizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetResizable(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool resizable);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_always_on_top")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetAlwaysOnTop(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool onTop);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_visible")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetVisible(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_maximized")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMaximized(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool maximized);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_minimized")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMinimized(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool minimized);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_skip_taskbar")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetSkipTaskbar(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool skip);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_minimizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMinimizable(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool minimizable);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_maximizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMaximizable(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool maximizable);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_closable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetClosable(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool closable);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_background_color")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetBackgroundColor(IntPtr window, in WryColor color);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_theme")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetTheme(IntPtr window, WryWindowTheme theme);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_focusable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetFocusable(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool focusable);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetSize(IntPtr window, double width, double height);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetPosition(IntPtr window, double x, double y);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_min_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMinSize(IntPtr window, double width, double height);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_max_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetMaxSize(IntPtr window, double width, double height);

    // ==========================================================================
    // Window Properties - Getters
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_maximized")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsMaximized(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_minimized")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsMinimized(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_visible")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsVisible(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_resizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsResizable(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_decorated")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsDecorated(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_always_on_top")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsAlwaysOnTop(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_minimizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsMinimizable(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_maximizable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsMaximizable(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_closable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsClosable(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_fullscreen")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsFullscreen(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_is_focused")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowIsFocused(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_scale_factor")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowScaleFactor(IntPtr window, out double scaleFactor);

    [LibraryImport(WryLib, EntryPoint = "wry_window_inner_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowInnerPosition(IntPtr window, out WryPoint position);

    [LibraryImport(WryLib, EntryPoint = "wry_window_outer_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowOuterPosition(IntPtr window, out WryPoint position);

    [LibraryImport(WryLib, EntryPoint = "wry_window_inner_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowInnerSize(IntPtr window, out WrySize size);

    [LibraryImport(WryLib, EntryPoint = "wry_window_outer_size")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowOuterSize(IntPtr window, out WrySize size);

    /// <summary>
    /// Get window title (pointer to internal buffer - do not free).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_title")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowTitle(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_cursor_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowCursorPosition(IntPtr window, out WryPoint position);

    // ==========================================================================
    // Window Actions
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_window_focus")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowFocus(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_request_redraw")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowRequestRedraw(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_request_user_attention")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowRequestUserAttention(IntPtr window, WryUserAttentionType attentionType);

    [LibraryImport(WryLib, EntryPoint = "wry_window_clear_user_attention")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowClearUserAttention(IntPtr window);

    // ==========================================================================
    // Cursor Operations
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_cursor_grab")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetCursorGrab(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool grab);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_cursor_visible")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetCursorVisible(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_cursor_position")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetCursorPosition(IntPtr window, double x, double y);

    [LibraryImport(WryLib, EntryPoint = "wry_window_set_ignore_cursor_events")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowSetIgnoreCursorEvents(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool ignore);

    // ==========================================================================
    // Drag Operations
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_window_start_dragging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowStartDragging(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_start_resize_dragging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WindowStartResizeDragging(IntPtr window, WryResizeDirection direction);

    // ==========================================================================
    // Monitor Information
    // ==========================================================================

    /// <summary>
    /// Get current monitor info as JSON (pointer to internal buffer - do not free).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_current_monitor")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowCurrentMonitor(IntPtr window);

    /// <summary>
    /// Get primary monitor info as JSON (pointer to internal buffer - do not free).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_primary_monitor")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowPrimaryMonitor(IntPtr window);

    /// <summary>
    /// Get all monitors as JSON array (pointer to internal buffer - do not free).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_window_available_monitors")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WindowAvailableMonitors(IntPtr window);

    [LibraryImport(WryLib, EntryPoint = "wry_window_monitor_from_point")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr WindowMonitorFromPoint(IntPtr window, WryPoint point);

    // ==========================================================================
    // Webview Creation/Destruction
    // ==========================================================================

    /// <summary>
    /// Create a webview in a window.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_webview_build")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial IntPtr WebviewBuild(IntPtr window, in WryWebviewConfig config);

    /// <summary>
    /// Free a webview.
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_webview_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void WebviewFree(IntPtr webview);

    /// <summary>
    /// Get webview identifier (caller must free with wry_string_free - actually returns allocated string).
    /// </summary>
    [LibraryImport(WryLib, EntryPoint = "wry_webview_identifier")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr WebviewIdentifier(IntPtr webview);

    // ==========================================================================
    // Webview Navigation
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_navigate", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewNavigate(IntPtr webview, string url);

    [LibraryImport(WryLib, EntryPoint = "wry_webview_reload")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewReload(IntPtr webview);

    // ==========================================================================
    // Webview Script Execution
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_evaluate_script", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewEvaluateScript(IntPtr webview, string script);

    // ==========================================================================
    // Webview Zoom
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_set_zoom")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewSetZoom(IntPtr webview, double scaleFactor);

    // ==========================================================================
    // Webview Visibility
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_show")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewShow(IntPtr webview);

    [LibraryImport(WryLib, EntryPoint = "wry_webview_hide")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewHide(IntPtr webview);

    // ==========================================================================
    // Webview Bounds (for child webviews)
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_set_bounds")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewSetBounds(IntPtr webview, double x, double y, double width, double height);

    // ==========================================================================
    // Webview Browsing Data
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_webview_clear_browsing_data")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool WebviewClearBrowsingData(IntPtr webview);

    // ==========================================================================
    // Dialogs
    // ==========================================================================

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_open")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial WryDialogSelection DialogOpen(in WryDialogOpenOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_save")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial WryDialogSelection DialogSave(in WryDialogSaveOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_selection_free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void DialogSelectionFree(WryDialogSelection selection);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_message")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool DialogMessage(in WryMessageDialogOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_confirm")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool DialogConfirm(in WryConfirmDialogOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_ask")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool DialogAsk(in WryAskDialogOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_prompt")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial WryPromptDialogResult DialogPrompt(in WryPromptDialogOptions options);

    [LibraryImport(WryLib, EntryPoint = "wry_dialog_prompt_result_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void DialogPromptResultFree(WryPromptDialogResult result);

    // ==========================================================================
    // Legacy API Shims (for backward compatibility during migration)
    // ==========================================================================

    // String free - no longer needed in new API, but kept for compatibility
    public static void StringFree(IntPtr _)
    {
        // New API uses internal buffers that don't need freeing
        // This is a no-op for compatibility
    }

    // Get last error - new API doesn't have this, return empty string
    public static IntPtr GetLastError() => IntPtr.Zero;
}
