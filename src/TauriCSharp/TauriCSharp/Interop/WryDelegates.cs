// wry-ffi C# bindings - Callback delegate definitions
// These delegates match the Rust callback function pointer types exactly
// Based on Velox's runtime-wry-ffi callback types

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

// ============================================================================
// Event Loop Callback
// ============================================================================

/// <summary>
/// Callback for event loop pump - receives JSON-serialized events.
/// Rust signature: extern "C" fn(event_description: *const c_char, user_data: *mut c_void) -> WryEventLoopControlFlow
/// </summary>
/// <param name="eventJson">Pointer to UTF-8 JSON string describing the event</param>
/// <param name="userData">User data pointer passed to wry_event_loop_pump</param>
/// <returns>Control flow indicating what the event loop should do next</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate WryEventLoopControlFlow WryEventLoopCallback(
    IntPtr eventJson,  // UTF-8 null-terminated JSON string
    IntPtr userData
);

// ============================================================================
// Custom Protocol Callback
// ============================================================================

/// <summary>
/// Called when custom protocol request is made.
/// Rust signature: extern "C" fn(request: *const WryCustomProtocolRequest, response: *mut WryCustomProtocolResponse, user_data: *mut c_void) -> bool
/// </summary>
/// <param name="request">Pointer to request struct with URL, method, headers, body</param>
/// <param name="response">Pointer to response struct to fill in</param>
/// <param name="userData">User data from protocol registration</param>
/// <returns>True if the request was handled, false to return 404</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool WryCustomProtocolHandler(
    IntPtr request,   // *const WryCustomProtocolRequest
    IntPtr response,  // *mut WryCustomProtocolResponse
    IntPtr userData
);

/// <summary>
/// Called to free response data after it's been sent.
/// Rust signature: extern "C" fn(user_data: *mut c_void)
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WryCustomProtocolResponseFree(IntPtr userData);

// ============================================================================
// Legacy Callbacks (kept for gradual migration, will be removed)
// ============================================================================

/// <summary>
/// [LEGACY] Called when webview sends a message to backend.
/// NOTE: This callback type is no longer used in the new event model.
/// Messages now come through JSON events in WryEventLoopCallback.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WebMessageCallbackNative(
    IntPtr window,
    IntPtr message,  // UTF-8 null-terminated string
    IntPtr userData
);

/// <summary>
/// [LEGACY] Called when window is closing.
/// NOTE: Close requests now come through JSON events.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool WindowClosingCallbackNative(
    IntPtr window,
    IntPtr userData
);

/// <summary>
/// [LEGACY] Called when window is resized.
/// NOTE: Resize events now come through JSON events.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowResizedCallbackNative(
    IntPtr window,
    uint width,
    uint height,
    IntPtr userData
);

/// <summary>
/// [LEGACY] Called when window is moved.
/// NOTE: Move events now come through JSON events.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowMovedCallbackNative(
    IntPtr window,
    int x,
    int y,
    IntPtr userData
);

/// <summary>
/// [LEGACY] Called when window focus changes.
/// NOTE: Focus events now come through JSON events.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void WindowFocusCallbackNative(
    IntPtr window,
    [MarshalAs(UnmanagedType.U1)] bool focused,
    IntPtr userData
);

/// <summary>
/// [LEGACY] Called when navigation starts.
/// NOTE: Navigation events now come through JSON events.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
internal delegate bool NavigationCallbackNative(
    IntPtr window,
    IntPtr url,  // UTF-8 null-terminated string
    IntPtr userData
);

/// <summary>
/// [LEGACY] Callback for UI thread invocation.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void InvokeCallbackNative(IntPtr userData);
