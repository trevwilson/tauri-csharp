// wry-ffi C# bindings - SafeHandle for WryApp
// Ensures proper cleanup of the app handle

using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp.Handles;

/// <summary>
/// SafeHandle wrapper for WryApp handles.
/// Ensures wry_app_destroy is called when the handle is released.
/// </summary>
internal sealed class WryAppHandle : SafeHandle
{
    /// <summary>
    /// Creates a new invalid handle. Use Create() to get a valid handle.
    /// </summary>
    public WryAppHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    /// <summary>
    /// Creates a handle wrapping an existing native pointer.
    /// </summary>
    private WryAppHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a new WryApp and returns a SafeHandle wrapper.
    /// Must be called on the main thread.
    /// </summary>
    /// <returns>A valid WryAppHandle, or throws on failure</returns>
    /// <exception cref="WryException">Thrown if app creation fails</exception>
    public static WryAppHandle Create()
    {
        var ptr = WryInterop.AppCreate();
        if (ptr == IntPtr.Zero)
        {
            var error = Marshal.PtrToStringUTF8(WryInterop.GetLastError());
            throw new WryException(error ?? "Failed to create app", WryErrorCode.EventLoopError);
        }
        return new WryAppHandle(ptr);
    }

    /// <summary>
    /// Gets the raw handle value. Use with caution.
    /// </summary>
    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            WryInterop.AppDestroy(handle);
        }
        return true;
    }
}
