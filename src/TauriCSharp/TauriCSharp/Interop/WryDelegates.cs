// wry-ffi C# bindings - Callback delegate definitions
// These delegates match the Rust callback function pointer types exactly

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// Called when webview sends a message to backend.
/// Rust signature: extern "C" fn(window: WryWindow, message: *const c_char, user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WebMessageCallbackNative(
    IntPtr window,
    IntPtr message,  // UTF-8 null-terminated string
    IntPtr userData
);

/// <summary>
/// Called when custom protocol request is made.
/// Rust signature: extern "C" fn(window, url, *out_data, *out_len, *out_mime_type, user_data) -> bool
/// </summary>
/// <returns>True if the request was handled, false to pass through</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool CustomProtocolCallbackNative(
    IntPtr window,
    IntPtr url,              // UTF-8 null-terminated string
    out IntPtr outData,      // Caller allocates, wry-ffi copies synchronously
    out nuint outLen,
    out IntPtr outMimeType,  // Caller allocates, wry-ffi copies synchronously
    IntPtr userData
);

/// <summary>
/// Called when window is closing.
/// Rust signature: extern "C" fn(window: WryWindow, user_data: *mut c_void) -> bool
/// </summary>
/// <returns>True to allow close, false to prevent</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool WindowClosingCallbackNative(
    IntPtr window,
    IntPtr userData
);

/// <summary>
/// Called when window is resized.
/// Rust signature: extern "C" fn(window: WryWindow, width: u32, height: u32, user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowResizedCallbackNative(
    IntPtr window,
    uint width,
    uint height,
    IntPtr userData
);

/// <summary>
/// Called when window is moved.
/// Rust signature: extern "C" fn(window: WryWindow, x: i32, y: i32, user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowMovedCallbackNative(
    IntPtr window,
    int x,
    int y,
    IntPtr userData
);

/// <summary>
/// Called when window focus changes.
/// Rust signature: extern "C" fn(window: WryWindow, focused: bool, user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowFocusCallbackNative(
    IntPtr window,
    [MarshalAs(UnmanagedType.U1)] bool focused,
    IntPtr userData
);

/// <summary>
/// Called when navigation starts.
/// Rust signature: extern "C" fn(window: WryWindow, url: *const c_char, user_data: *mut c_void) -> bool
/// </summary>
/// <returns>True to allow navigation, false to cancel</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool NavigationCallbackNative(
    IntPtr window,
    IntPtr url,  // UTF-8 null-terminated string
    IntPtr userData
);

/// <summary>
/// Callback for UI thread invocation.
/// Rust signature: extern "C" fn(user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void InvokeCallbackNative(IntPtr userData);
