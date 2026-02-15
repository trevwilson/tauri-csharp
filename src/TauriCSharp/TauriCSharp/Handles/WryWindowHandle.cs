// wry-ffi C# bindings - SafeHandle for WryWindow
// Updated for Velox-based FFI with separate window and webview handles

using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp.Handles;

/// <summary>
/// SafeHandle wrapper for WryWindowHandle (the native window).
/// Ensures wry_window_free is called when the handle is released.
/// </summary>
internal sealed class WryNativeWindowHandle : SafeHandle
{
    private readonly WryCallbackRegistry? _callbackRegistry;

    public WryNativeWindowHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    private WryNativeWindowHandle(IntPtr handle, WryCallbackRegistry? callbackRegistry)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
        _callbackRegistry = callbackRegistry;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a new window using the event loop and config.
    /// </summary>
    public static WryNativeWindowHandle Create(
        WryEventLoopHandle eventLoop,
        in WryWindowConfig config,
        WryCallbackRegistry? callbackRegistry = null)
    {
        var ptr = WryInterop.WindowBuild(eventLoop.DangerousGetRawHandle(), in config);
        if (ptr == IntPtr.Zero)
        {
            throw new WryException("Failed to create window", WryErrorCode.WindowCreationFailed);
        }
        return new WryNativeWindowHandle(ptr, callbackRegistry);
    }

    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            _callbackRegistry?.Unregister(handle);
            WryInterop.WindowFree(handle);
        }
        return true;
    }
}

/// <summary>
/// SafeHandle wrapper for WryWebviewHandle.
/// Ensures wry_webview_free is called when the handle is released.
/// </summary>
internal sealed class WryNativeWebviewHandle : SafeHandle
{
    public WryNativeWebviewHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    private WryNativeWebviewHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Creates a new webview in the given window.
    /// </summary>
    public static WryNativeWebviewHandle Create(
        WryNativeWindowHandle window,
        in WryWebviewConfig config)
    {
        var ptr = WryInterop.WebviewBuild(window.DangerousGetRawHandle(), in config);
        if (ptr == IntPtr.Zero)
        {
            throw new WryException("Failed to create webview", WryErrorCode.WebviewCreationFailed);
        }
        return new WryNativeWebviewHandle(ptr);
    }

    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            WryInterop.WebviewFree(handle);
        }
        return true;
    }
}

// Legacy alias - maps to the old combined window approach
// Will be removed after migration
[System.Obsolete("Use WryNativeWindowHandle + WryNativeWebviewHandle instead")]
internal sealed class WryWindowHandle : SafeHandle
{
    private readonly WryCallbackRegistry? _callbackRegistry;

    public WryWindowHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    private WryWindowHandle(IntPtr handle, WryCallbackRegistry? callbackRegistry)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
        _callbackRegistry = callbackRegistry;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    // This method signature is kept for backward compatibility during migration
    // but it can't actually work without WryWindowParams which no longer exists
    // Callers should migrate to using WryNativeWindowHandle directly
    public static WryWindowHandle CreateLegacy(IntPtr handle, WryCallbackRegistry? callbackRegistry = null)
    {
        return new WryWindowHandle(handle, callbackRegistry);
    }

    public IntPtr DangerousGetRawHandle() => handle;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            _callbackRegistry?.Unregister(handle);
            WryInterop.WindowFree(handle);
        }
        return true;
    }
}
