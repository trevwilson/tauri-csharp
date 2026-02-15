// wry-ffi C# bindings - SafeHandle for WryEventLoop
// Renamed from WryAppHandle - now wraps the event loop instead of a combined "app"

using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp.Handles;

/// <summary>
/// SafeHandle wrapper for WryEventLoop handles.
/// Ensures wry_event_loop_free is called when the handle is released.
/// </summary>
internal sealed class WryEventLoopHandle : SafeHandle
{
    /// <summary>
    /// Creates a new invalid handle. Use Create() to get a valid handle.
    /// </summary>
    public WryEventLoopHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    /// <summary>
    /// Creates a handle wrapping an existing native pointer.
    /// </summary>
    private WryEventLoopHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a new WryEventLoop and returns a SafeHandle wrapper.
    /// Must be called on the main thread.
    /// </summary>
    /// <returns>A valid WryEventLoopHandle, or throws on failure</returns>
    /// <exception cref="WryException">Thrown if event loop creation fails</exception>
    public static WryEventLoopHandle Create()
    {
        var ptr = WryInterop.EventLoopNew();
        if (ptr == IntPtr.Zero)
        {
            throw new WryException("Failed to create event loop", WryErrorCode.EventLoopError);
        }
        return new WryEventLoopHandle(ptr);
    }

    /// <summary>
    /// Gets the raw handle value. Use with caution.
    /// </summary>
    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            WryInterop.EventLoopFree(handle);
        }
        return true;
    }
}

/// <summary>
/// SafeHandle wrapper for WryEventLoopProxy handles.
/// Used for cross-thread communication with the event loop.
/// </summary>
internal sealed class WryEventLoopProxyHandle : SafeHandle
{
    public WryEventLoopProxyHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    private WryEventLoopProxyHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a proxy for the given event loop.
    /// </summary>
    public static WryEventLoopProxyHandle Create(WryEventLoopHandle eventLoop)
    {
        var ptr = WryInterop.EventLoopCreateProxy(eventLoop.DangerousGetRawHandle());
        if (ptr == IntPtr.Zero)
        {
            throw new WryException("Failed to create event loop proxy", WryErrorCode.EventLoopError);
        }
        return new WryEventLoopProxyHandle(ptr);
    }

    /// <summary>
    /// Request the event loop to exit.
    /// </summary>
    public bool RequestExit()
    {
        return WryInterop.EventLoopProxyRequestExit(handle);
    }

    /// <summary>
    /// Send a custom user event payload.
    /// </summary>
    public bool SendUserEvent(string? payload)
    {
        return WryInterop.EventLoopProxySendUserEvent(handle, payload);
    }

    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            WryInterop.EventLoopProxyFree(handle);
        }
        return true;
    }
}

// Legacy alias for backward compatibility during migration
[System.Obsolete("Use WryEventLoopHandle instead")]
internal sealed class WryAppHandle : SafeHandle
{
    private readonly WryEventLoopHandle _eventLoop;

    public WryAppHandle() : base(IntPtr.Zero, ownsHandle: false)
    {
        _eventLoop = WryEventLoopHandle.Create();
        SetHandle(_eventLoop.DangerousGetRawHandle());
    }

    public override bool IsInvalid => _eventLoop.IsInvalid;

    public static WryAppHandle Create() => new();

    public IntPtr DangerousGetRawHandle() => _eventLoop.DangerousGetRawHandle();

    protected override bool ReleaseHandle()
    {
        _eventLoop.Dispose();
        return true;
    }
}
