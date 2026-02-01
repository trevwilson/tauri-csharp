// TauriWindow wry-ffi compatibility layer
// This partial class provides compatibility implementations for features that need
// to be migrated from Photino to wry-ffi. Some features throw NotSupportedException
// when not yet implemented in wry-ffi.

using System.Drawing;
using System.Runtime.InteropServices;
using TauriCSharp.Handles;
using TauriCSharp.Interop;

namespace TauriCSharp;

public partial class TauriWindow
{
    // ========================================================================
    // Window Properties - implemented via wry-ffi
    // ========================================================================

    /// <summary>
    /// Gets the window size using wry-ffi.
    /// </summary>
    private Size GetSizeWry()
    {
        if (_nativeInstance == IntPtr.Zero)
            return new Size(_startupParameters.Width, _startupParameters.Height);

        var size = WryInterop.WindowGetSize(_nativeInstance);
        return new Size((int)size.Width, (int)size.Height);
    }

    /// <summary>
    /// Sets the window size using wry-ffi.
    /// </summary>
    private void SetSizeWry(int width, int height)
    {
        WryInterop.WindowSetSize(_nativeInstance, new WrySize((uint)width, (uint)height));
    }

    /// <summary>
    /// Gets the window position using wry-ffi.
    /// </summary>
    private Point GetPositionWry()
    {
        if (_nativeInstance == IntPtr.Zero)
            return new Point(_startupParameters.Left, _startupParameters.Top);

        var pos = WryInterop.WindowGetPosition(_nativeInstance);
        return new Point(pos.X, pos.Y);
    }

    /// <summary>
    /// Sets the window position using wry-ffi.
    /// </summary>
    private void SetPositionWry(int x, int y)
    {
        WryInterop.WindowSetPosition(_nativeInstance, new WryPosition(x, y));
    }

    /// <summary>
    /// Gets the window title using wry-ffi.
    /// </summary>
    private string GetTitleWry()
    {
        if (_nativeInstance == IntPtr.Zero)
            return _startupParameters.Title ?? "TauriCSharp";

        using var nativeStr = new WryNativeString(WryInterop.WindowGetTitle(_nativeInstance));
        return nativeStr.Value ?? "";
    }

    /// <summary>
    /// Sets the window title using wry-ffi.
    /// </summary>
    private void SetTitleWry(string title)
    {
        WryInterop.WindowSetTitle(_nativeInstance, title);
    }

    /// <summary>
    /// Sets fullscreen using wry-ffi.
    /// </summary>
    private void SetFullscreenWry(bool fullscreen)
    {
        WryInterop.WindowSetFullscreen(_nativeInstance, fullscreen);
    }

    /// <summary>
    /// Minimizes the window using wry-ffi.
    /// </summary>
    private void MinimizeWry()
    {
        WryInterop.WindowMinimize(_nativeInstance);
    }

    /// <summary>
    /// Maximizes the window using wry-ffi.
    /// </summary>
    private void MaximizeWry()
    {
        WryInterop.WindowMaximize(_nativeInstance);
    }

    /// <summary>
    /// Sets zoom using wry-ffi.
    /// </summary>
    private void SetZoomWry(int zoomPercent)
    {
        WryInterop.WebViewSetZoom(_nativeInstance, zoomPercent / 100.0);
    }

    /// <summary>
    /// Sends a message to the webview using wry-ffi.
    /// </summary>
    private void SendWebMessageWry(string message)
    {
        WryInterop.WebViewSendMessage(_nativeInstance, message).ThrowIfError();
    }

    // ========================================================================
    // Features not yet implemented in wry-ffi - throw NotSupportedException
    // ========================================================================

    private static void ThrowNotSupported(string feature)
    {
        throw new NotSupportedException($"{feature} is not yet supported with the wry-ffi backend. " +
            "This feature will be implemented in a future release.");
    }

    // Platform-specific features that need platform-specific implementations
    private void SetIconFileNotSupported() => ThrowNotSupported("Window icon");
    private void GetTransparentNotSupported() => ThrowNotSupported("Transparent window state query");
    private void GetContextMenuEnabledNotSupported() => ThrowNotSupported("Context menu enabled state");
    private void SetContextMenuEnabledNotSupported() => ThrowNotSupported("Context menu enabled");
    private void GetDevToolsEnabledNotSupported() => ThrowNotSupported("DevTools enabled state query");
    private void SetDevToolsEnabledNotSupported() => ThrowNotSupported("DevTools enabled");
    private void GetGrantBrowserPermissionsNotSupported() => ThrowNotSupported("Browser permissions state");
    private void GetResizableNotSupported() => ThrowNotSupported("Resizable state query");
    private void SetResizableNotSupported() => ThrowNotSupported("Resizable");
    private void GetTopmostNotSupported() => ThrowNotSupported("Topmost state query");
    private void SetTopmostNotSupported() => ThrowNotSupported("Topmost");

    // Monitor/DPI features
    private void GetMonitorsNotSupported() => ThrowNotSupported("Monitor enumeration");
    private void GetScreenDpiNotSupported() => ThrowNotSupported("Screen DPI");

    // Dialog features
    private void ShowOpenFileNotSupported() => ThrowNotSupported("Open file dialog");
    private void ShowOpenFolderNotSupported() => ThrowNotSupported("Open folder dialog");
    private void ShowSaveFileNotSupported() => ThrowNotSupported("Save file dialog");
    private void ShowMessageDialogNotSupported() => ThrowNotSupported("Message dialog");

    // Windows-specific
    private void GetWindowHandleNotSupported() => ThrowNotSupported("Native window handle");
    private void SetWebView2PathNotSupported() => ThrowNotSupported("WebView2 runtime path");
    private void ClearBrowserAutoFillNotSupported() => ThrowNotSupported("Browser autofill clearing");
    private void ShowNotificationNotSupported() => ThrowNotSupported("Notifications");

    // Browser settings that wry doesn't expose at runtime
    private void GetMediaAutoplayNotSupported() => ThrowNotSupported("Media autoplay state query");
    private void GetUserAgentNotSupported() => ThrowNotSupported("User agent query");
    private void GetFileSystemAccessNotSupported() => ThrowNotSupported("File system access state");
    private void GetWebSecurityNotSupported() => ThrowNotSupported("Web security state");
    private void GetJavascriptClipboardNotSupported() => ThrowNotSupported("JavaScript clipboard state");
    private void GetMediaStreamNotSupported() => ThrowNotSupported("Media stream state");
    private void GetSmoothScrollingNotSupported() => ThrowNotSupported("Smooth scrolling state");
    private void GetIgnoreCertErrorsNotSupported() => ThrowNotSupported("Ignore cert errors state");
    private void GetNotificationsNotSupported() => ThrowNotSupported("Notifications enabled state");

    // Center functionality
    private void CenterNotSupported() => ThrowNotSupported("Window centering");

    // ========================================================================
    // Features that have partial support - need review
    // ========================================================================

    /// <summary>
    /// wry-ffi doesn't currently support min/max size after window creation.
    /// This is a no-op after creation.
    /// </summary>
    private void SetMinSizeWry(int minWidth, int minHeight)
    {
        // wry-ffi sets min/max size at creation time only
        // Log a warning if called after creation
        if (_nativeInstance != IntPtr.Zero)
        {
            Log("Warning: SetMinSize after window creation is not supported with wry-ffi backend");
        }
    }

    /// <summary>
    /// wry-ffi doesn't currently support min/max size after window creation.
    /// This is a no-op after creation.
    /// </summary>
    private void SetMaxSizeWry(int maxWidth, int maxHeight)
    {
        // wry-ffi sets min/max size at creation time only
        if (_nativeInstance != IntPtr.Zero)
        {
            Log("Warning: SetMaxSize after window creation is not supported with wry-ffi backend");
        }
    }
}
