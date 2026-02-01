# C# P/Invoke Bindings Specification

This document specifies the .NET bindings that map to the wry-ffi C API.

## Interop Strategy

Use modern .NET 7+ source-generated P/Invoke via `LibraryImport` for:
- Better performance (no runtime marshaling generation)
- AOT compatibility
- Compile-time validation

## Native Library Loading

```csharp
internal static class NativeLibrary
{
    private const string LibraryName = "wry_ffi";

    static NativeLibrary()
    {
        // Configure native library resolution
        System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
            typeof(NativeLibrary).Assembly,
            DllImportResolver);
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
            return IntPtr.Zero;

        string rid = RuntimeInformation.RuntimeIdentifier;
        string ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll"
                   : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib"
                   : ".so";

        string path = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            rid,
            "native",
            $"{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib")}{LibraryName}{ext}");

        return System.Runtime.InteropServices.NativeLibrary.Load(path);
    }
}
```

## Struct Definitions

```csharp
[StructLayout(LayoutKind.Sequential)]
internal struct WryWindowParams
{
    public IntPtr Title;           // UTF-8 string pointer
    public IntPtr Url;
    public IntPtr Html;
    public IntPtr UserAgent;
    public IntPtr DataDirectory;

    public int X;
    public int Y;
    public uint Width;
    public uint Height;
    public uint MinWidth;
    public uint MinHeight;
    public uint MaxWidth;
    public uint MaxHeight;

    [MarshalAs(UnmanagedType.U1)]
    public bool Resizable;
    [MarshalAs(UnmanagedType.U1)]
    public bool Fullscreen;
    [MarshalAs(UnmanagedType.U1)]
    public bool Maximized;
    [MarshalAs(UnmanagedType.U1)]
    public bool Minimized;
    [MarshalAs(UnmanagedType.U1)]
    public bool Visible;
    [MarshalAs(UnmanagedType.U1)]
    public bool Transparent;
    [MarshalAs(UnmanagedType.U1)]
    public bool Decorations;
    [MarshalAs(UnmanagedType.U1)]
    public bool AlwaysOnTop;
    [MarshalAs(UnmanagedType.U1)]
    public bool DevtoolsEnabled;
    [MarshalAs(UnmanagedType.U1)]
    public bool AutoplayEnabled;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WrySize
{
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryPosition
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryResult
{
    [MarshalAs(UnmanagedType.U1)]
    public bool Success;
    public int ErrorCode;
    public IntPtr ErrorMessage;  // Do not free - valid until next call

    public void ThrowIfFailed()
    {
        if (!Success)
        {
            string message = ErrorMessage != IntPtr.Zero
                ? Marshal.PtrToStringUTF8(ErrorMessage) ?? "Unknown error"
                : "Unknown error";
            throw new TauriException((TauriErrorCode)ErrorCode, message);
        }
    }
}
```

## Delegate Definitions

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WebMessageCallbackNative(
    IntPtr window,
    IntPtr message,  // UTF-8 string
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool CustomProtocolCallbackNative(
    IntPtr window,
    IntPtr url,           // UTF-8 string
    out IntPtr outData,
    out nuint outLen,
    out IntPtr outMimeType,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool WindowClosingCallbackNative(
    IntPtr window,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowResizedCallbackNative(
    IntPtr window,
    uint width,
    uint height,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowMovedCallbackNative(
    IntPtr window,
    int x,
    int y,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowFocusCallbackNative(
    IntPtr window,
    [MarshalAs(UnmanagedType.U1)] bool focused,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool NavigationCallbackNative(
    IntPtr window,
    IntPtr url,
    IntPtr userData);
```

## P/Invoke Declarations

```csharp
internal static partial class WryInterop
{
    private const string DllName = "wry_ffi";

    // ===== Application Lifecycle =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_app_create();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_app_run(IntPtr app);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_app_quit(IntPtr app);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_app_destroy(IntPtr app);

    // ===== Window Management =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_window_create(IntPtr app, in WryWindowParams parameters);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_destroy(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_visible(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool wry_window_is_visible(IntPtr window);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_title(IntPtr window, string title);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_window_get_title(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_size(IntPtr window, WrySize size);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WrySize wry_window_get_size(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_position(IntPtr window, WryPosition position);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryPosition wry_window_get_position(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_minimize(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_maximize(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_unmaximize(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_fullscreen(IntPtr window, [MarshalAs(UnmanagedType.U1)] bool fullscreen);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_focus(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_close(IntPtr window);

    // ===== Webview Operations =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_navigate(IntPtr window, string url);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_load_html(IntPtr window, string html);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_evaluate_script(IntPtr window, string script);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_send_message(IntPtr window, string message);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_webview_open_devtools(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_webview_close_devtools(IntPtr window);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_webview_set_zoom(IntPtr window, double zoom);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_webview_get_url(IntPtr window);

    // ===== Custom Protocol =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_register_protocol(
        IntPtr app,
        string scheme,
        CustomProtocolCallbackNative callback,
        IntPtr userData);

    // ===== Event Callbacks =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_message_callback(
        IntPtr window,
        WebMessageCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_closing_callback(
        IntPtr window,
        WindowClosingCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_resized_callback(
        IntPtr window,
        WindowResizedCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_moved_callback(
        IntPtr window,
        WindowMovedCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_focus_callback(
        IntPtr window,
        WindowFocusCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_navigation_callback(
        IntPtr window,
        NavigationCallbackNative callback,
        IntPtr userData);

    // ===== Utility =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_string_free(IntPtr s);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_get_last_error();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_version();
}
```

## Safe Wrapper Classes

```csharp
/// <summary>
/// Manages UTF-8 string marshaling for native calls
/// </summary>
internal ref struct Utf8String
{
    private readonly IntPtr _ptr;
    private readonly bool _owned;

    public Utf8String(string? value)
    {
        if (value == null)
        {
            _ptr = IntPtr.Zero;
            _owned = false;
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value + '\0');
            _ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, _ptr, bytes.Length);
            _owned = true;
        }
    }

    public IntPtr Pointer => _ptr;

    public void Dispose()
    {
        if (_owned && _ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ptr);
        }
    }
}

/// <summary>
/// Wraps a native string that must be freed with wry_string_free
/// </summary>
internal readonly struct NativeString : IDisposable
{
    private readonly IntPtr _ptr;

    public NativeString(IntPtr ptr) => _ptr = ptr;

    public string? Value => _ptr != IntPtr.Zero
        ? Marshal.PtrToStringUTF8(_ptr)
        : null;

    public void Dispose()
    {
        if (_ptr != IntPtr.Zero)
        {
            WryInterop.wry_string_free(_ptr);
        }
    }
}
```

## Callback Prevent-Collection Pattern

```csharp
/// <summary>
/// Prevents delegates from being garbage collected while registered with native code
/// </summary>
internal class CallbackRegistry
{
    private readonly Dictionary<IntPtr, List<Delegate>> _callbacks = new();

    public void Register(IntPtr handle, Delegate callback)
    {
        if (!_callbacks.TryGetValue(handle, out var list))
        {
            list = new List<Delegate>();
            _callbacks[handle] = list;
        }
        list.Add(callback);
    }

    public void Unregister(IntPtr handle)
    {
        _callbacks.Remove(handle);
    }
}
```

## Usage Example

```csharp
// High-level API usage (TauriWindow wraps this)
var app = WryInterop.wry_app_create();
if (app == IntPtr.Zero)
    throw new TauriException("Failed to create app");

var parameters = new WryWindowParams
{
    Width = 1200,
    Height = 800,
    Resizable = true,
    Visible = true,
    Decorations = true,
    DevtoolsEnabled = true
};

// Marshal strings
using var title = new Utf8String("My App");
using var url = new Utf8String("https://example.com");
parameters.Title = title.Pointer;
parameters.Url = url.Pointer;

var window = WryInterop.wry_window_create(app, in parameters);
if (window == IntPtr.Zero)
    throw new TauriException("Failed to create window");

// Set up message callback
WebMessageCallbackNative messageCallback = (win, msg, userData) =>
{
    string? message = Marshal.PtrToStringUTF8(msg);
    Console.WriteLine($"Received: {message}");
};
callbackRegistry.Register(window, messageCallback);  // Prevent GC
WryInterop.wry_window_set_message_callback(window, messageCallback, IntPtr.Zero);

// Run event loop (blocks)
var result = WryInterop.wry_app_run(app);
result.ThrowIfFailed();

// Cleanup
WryInterop.wry_window_destroy(window);
WryInterop.wry_app_destroy(app);
```

## Additional Structs (Phase 3+)

```csharp
[StructLayout(LayoutKind.Sequential)]
internal struct WryFileFilter
{
    public IntPtr Name;      // UTF-8 string, e.g., "Images"
    public IntPtr Pattern;   // UTF-8 string, e.g., "*.png;*.jpg"
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryFileDialogResult
{
    public IntPtr Paths;     // Array of UTF-8 string pointers
    public nuint Count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryMonitor
{
    public int X;
    public int Y;
    public uint Width;
    public uint Height;
    public int WorkX;
    public int WorkY;
    public uint WorkWidth;
    public uint WorkHeight;
    public double ScaleFactor;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsPrimary;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryMonitorList
{
    public IntPtr Monitors;  // Array of WryMonitor
    public nuint Count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WryCapabilities
{
    [MarshalAs(UnmanagedType.U1)]
    public bool HasSystemTray;
    [MarshalAs(UnmanagedType.U1)]
    public bool HasNotifications;
    [MarshalAs(UnmanagedType.U1)]
    public bool HasTransparentWindows;
    [MarshalAs(UnmanagedType.U1)]
    public bool HasDragDrop;
    [MarshalAs(UnmanagedType.U1)]
    public bool HasDevtools;
    public IntPtr WebviewVersion;  // UTF-8 string
    public IntPtr Platform;        // UTF-8 string
}

internal enum WryMessageBoxButtons
{
    Ok = 0,
    OkCancel = 1,
    YesNo = 2,
    YesNoCancel = 3,
}

internal enum WryMessageBoxIcon
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Question = 3,
}

internal enum WryMessageBoxResult
{
    Ok = 0,
    Cancel = 1,
    Yes = 2,
    No = 3,
}

internal enum WryDragDropEvent
{
    Enter = 0,
    Over = 1,
    Drop = 2,
    Leave = 3,
}
```

## Additional Delegates (Phase 3+)

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void PageLoadCallbackNative(
    IntPtr window,
    IntPtr url,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void TitleChangedCallbackNative(
    IntPtr window,
    IntPtr title,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool DownloadStartedCallbackNative(
    IntPtr window,
    IntPtr url,
    IntPtr suggestedFilename,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void DownloadCompletedCallbackNative(
    IntPtr window,
    IntPtr path,
    [MarshalAs(UnmanagedType.U1)] bool success,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool DragDropCallbackNative(
    IntPtr window,
    WryDragDropEvent eventType,
    IntPtr paths,      // Array of UTF-8 string pointers
    nuint pathCount,
    int x,
    int y,
    IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void InvokeCallbackNative(IntPtr userData);
```

## Additional P/Invoke Declarations (Phase 3+)

```csharp
internal static partial class WryInterop
{
    // ===== Dialogs =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryFileDialogResult wry_dialog_open_file(
        IntPtr window,
        string? title,
        string? defaultPath,
        WryFileFilter* filters,
        nuint filterCount,
        [MarshalAs(UnmanagedType.U1)] bool multiSelect);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryFileDialogResult wry_dialog_open_folder(
        IntPtr window,
        string? title,
        string? defaultPath,
        [MarshalAs(UnmanagedType.U1)] bool multiSelect);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr wry_dialog_save_file(
        IntPtr window,
        string? title,
        string? defaultPath,
        WryFileFilter* filters,
        nuint filterCount);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_dialog_result_free(WryFileDialogResult result);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryMessageBoxResult wry_dialog_message(
        IntPtr window,
        string title,
        string message,
        WryMessageBoxButtons buttons,
        WryMessageBoxIcon icon);

    // ===== Notifications =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_notification_show(
        IntPtr app,
        string title,
        string body,
        string? iconPath);

    // ===== Monitor/Display =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryMonitorList wry_get_monitors(IntPtr app);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_monitors_free(WryMonitorList list);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryMonitor wry_get_primary_monitor(IntPtr app);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial double wry_window_get_scale_factor(IntPtr window);

    // ===== Window Icon =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_window_set_icon_file(IntPtr window, string path);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_window_set_icon_rgba(
        IntPtr window,
        byte* data,
        uint width,
        uint height);

    // ===== Thread Dispatch =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_invoke(
        IntPtr app,
        InvokeCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_invoke_sync(
        IntPtr app,
        InvokeCallbackNative callback,
        IntPtr userData);

    // ===== Additional Webview =====

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_add_init_script(IntPtr window, string script);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_webview_set_context_menu_enabled(
        IntPtr window,
        [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_webview_set_clipboard_enabled(
        IntPtr window,
        [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryResult wry_webview_clear_data(IntPtr window);

    // ===== Additional Callbacks =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_page_load_callback(
        IntPtr window,
        PageLoadCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_title_changed_callback(
        IntPtr window,
        TitleChangedCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_download_started_callback(
        IntPtr window,
        DownloadStartedCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_download_completed_callback(
        IntPtr window,
        DownloadCompletedCallbackNative callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void wry_window_set_drag_drop_callback(
        IntPtr window,
        DragDropCallbackNative callback,
        IntPtr userData);

    // ===== Capabilities =====

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial WryCapabilities wry_get_capabilities();
}
```

## Safe Wrapper for Dialog Results

```csharp
/// <summary>
/// Safe wrapper for file dialog results
/// </summary>
internal readonly struct FileDialogResult : IDisposable
{
    private readonly WryFileDialogResult _result;

    public FileDialogResult(WryFileDialogResult result) => _result = result;

    public string[] GetPaths()
    {
        if (_result.Count == 0 || _result.Paths == IntPtr.Zero)
            return Array.Empty<string>();

        var paths = new string[(int)_result.Count];
        for (int i = 0; i < (int)_result.Count; i++)
        {
            var ptr = Marshal.ReadIntPtr(_result.Paths, i * IntPtr.Size);
            paths[i] = Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }
        return paths;
    }

    public void Dispose()
    {
        if (_result.Paths != IntPtr.Zero)
        {
            WryInterop.wry_dialog_result_free(_result);
        }
    }
}

/// <summary>
/// Safe wrapper for monitor list
/// </summary>
internal readonly struct MonitorListResult : IDisposable
{
    private readonly WryMonitorList _list;

    public MonitorListResult(WryMonitorList list) => _list = list;

    public Monitor[] GetMonitors()
    {
        if (_list.Count == 0 || _list.Monitors == IntPtr.Zero)
            return Array.Empty<Monitor>();

        var monitors = new Monitor[(int)_list.Count];
        int structSize = Marshal.SizeOf<WryMonitor>();

        for (int i = 0; i < (int)_list.Count; i++)
        {
            var ptr = IntPtr.Add(_list.Monitors, i * structSize);
            var native = Marshal.PtrToStructure<WryMonitor>(ptr);
            monitors[i] = new Monitor(
                new Rectangle(native.X, native.Y, (int)native.Width, (int)native.Height),
                new Rectangle(native.WorkX, native.WorkY, (int)native.WorkWidth, (int)native.WorkHeight),
                native.ScaleFactor);
        }
        return monitors;
    }

    public void Dispose()
    {
        if (_list.Monitors != IntPtr.Zero)
        {
            WryInterop.wry_monitors_free(_list);
        }
    }
}
```
