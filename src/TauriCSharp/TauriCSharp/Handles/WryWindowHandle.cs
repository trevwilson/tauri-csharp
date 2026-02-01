// wry-ffi C# bindings - SafeHandle for WryWindow
// Ensures proper cleanup of window handles and their callbacks

using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp.Handles;

/// <summary>
/// SafeHandle wrapper for WryWindow handles.
/// Ensures wry_window_destroy is called when the handle is released,
/// and unregisters any pinned callbacks from the registry.
/// </summary>
internal sealed class WryWindowHandle : SafeHandle
{
    private readonly WryCallbackRegistry? _callbackRegistry;

    /// <summary>
    /// Creates a new invalid handle.
    /// </summary>
    public WryWindowHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    /// <summary>
    /// Creates a handle wrapping an existing native pointer.
    /// </summary>
    /// <param name="handle">The native window pointer</param>
    /// <param name="callbackRegistry">Optional callback registry for cleanup</param>
    private WryWindowHandle(IntPtr handle, WryCallbackRegistry? callbackRegistry)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
        _callbackRegistry = callbackRegistry;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a new WryWindow and returns a SafeHandle wrapper.
    /// Must be called on the main thread.
    /// </summary>
    /// <param name="appHandle">The app handle</param>
    /// <param name="parameters">Window creation parameters</param>
    /// <param name="callbackRegistry">Optional callback registry for tracking pinned delegates</param>
    /// <returns>A valid WryWindowHandle, or throws on failure</returns>
    /// <exception cref="WryException">Thrown if window creation fails</exception>
    public static WryWindowHandle Create(
        IntPtr appHandle,
        in WryWindowParams parameters,
        WryCallbackRegistry? callbackRegistry = null)
    {
        var ptr = WryInterop.WindowCreate(appHandle, in parameters);
        if (ptr == IntPtr.Zero)
        {
            var error = Marshal.PtrToStringUTF8(WryInterop.GetLastError());
            throw new WryException(error ?? "Failed to create window", WryErrorCode.WindowCreationFailed);
        }
        return new WryWindowHandle(ptr, callbackRegistry);
    }

    /// <summary>
    /// Gets the raw handle value. Use with caution.
    /// </summary>
    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            // First unregister callbacks to prevent dangling delegate issues
            _callbackRegistry?.Unregister(handle);

            // Then destroy the window
            WryInterop.WindowDestroy(handle);
        }
        return true;
    }
}
