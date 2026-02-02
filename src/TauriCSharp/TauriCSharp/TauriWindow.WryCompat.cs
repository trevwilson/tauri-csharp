// TauriWindow wry-ffi compatibility layer
// This partial class provides compatibility implementations for features that need
// to be migrated from Photino to the Velox-based wry-ffi backend.
// Updated for the new separate event loop, window, and webview architecture.

using System.Drawing;
using System.Runtime.InteropServices;
using TauriCSharp.Handles;
using TauriCSharp.Interop;

namespace TauriCSharp;

public partial class TauriWindow
{
    // ========================================================================
    // Window Properties - implemented via Velox wry-ffi
    // ========================================================================

    /// <summary>
    /// Gets the window size using wry-ffi.
    /// </summary>
    private Size GetSizeWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return new Size(_startupParameters.Width, _startupParameters.Height);

        if (WryInterop.WindowInnerSize(_wryWindow, out var size))
        {
            return new Size((int)size.Width, (int)size.Height);
        }
        return new Size(_startupParameters.Width, _startupParameters.Height);
    }

    /// <summary>
    /// Sets the window size using wry-ffi.
    /// </summary>
    private void SetSizeWry(int width, int height)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetSize(_wryWindow, width, height);
        }
    }

    /// <summary>
    /// Gets the window position using wry-ffi.
    /// </summary>
    private Point GetPositionWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return new Point(_startupParameters.Left, _startupParameters.Top);

        if (WryInterop.WindowOuterPosition(_wryWindow, out var pos))
        {
            return new Point((int)pos.X, (int)pos.Y);
        }
        return new Point(_startupParameters.Left, _startupParameters.Top);
    }

    /// <summary>
    /// Sets the window position using wry-ffi.
    /// </summary>
    private void SetPositionWry(int x, int y)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetPosition(_wryWindow, x, y);
        }
    }

    /// <summary>
    /// Gets the window title using wry-ffi.
    /// </summary>
    private string GetTitleWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return _startupParameters.Title ?? "TauriCSharp";

        var titlePtr = WryInterop.WindowTitle(_wryWindow);
        if (titlePtr != IntPtr.Zero)
        {
            return Marshal.PtrToStringUTF8(titlePtr) ?? "";
        }
        return _startupParameters.Title ?? "";
    }

    /// <summary>
    /// Sets the window title using wry-ffi.
    /// </summary>
    private void SetTitleWry(string title)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetTitle(_wryWindow, title);
        }
    }

    /// <summary>
    /// Sets fullscreen using wry-ffi.
    /// </summary>
    private void SetFullscreenWry(bool fullscreen)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetFullscreen(_wryWindow, fullscreen);
        }
    }

    /// <summary>
    /// Minimizes the window using wry-ffi.
    /// </summary>
    private void MinimizeWry()
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetMinimized(_wryWindow, true);
        }
    }

    /// <summary>
    /// Maximizes the window using wry-ffi.
    /// </summary>
    private void MaximizeWry()
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetMaximized(_wryWindow, true);
        }
    }

    /// <summary>
    /// Restores the window using wry-ffi.
    /// </summary>
    private void RestoreWry()
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetMinimized(_wryWindow, false);
            WryInterop.WindowSetMaximized(_wryWindow, false);
        }
    }

    /// <summary>
    /// Sets zoom using wry-ffi (on webview, not window).
    /// </summary>
    private void SetZoomWry(int zoomPercent)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            WryInterop.WebviewSetZoom(_wryWebview, zoomPercent / 100.0);
        }
    }

    /// <summary>
    /// Sends a message to the webview by executing script.
    /// </summary>
    private void SendWebMessageWry(string message)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            // Escape the message for JavaScript
            var escapedMessage = message
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            // The webview should have window.ipc.receive registered to handle this
            var script = $"if (window.ipc && window.ipc.receive) {{ window.ipc.receive('{escapedMessage}'); }}";
            WryInterop.WebviewEvaluateScript(_wryWebview, script);
        }
    }

    /// <summary>
    /// Executes script in the webview.
    /// </summary>
    private void ExecuteScriptWry(string script)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            WryInterop.WebviewEvaluateScript(_wryWebview, script);
        }
    }

    /// <summary>
    /// Navigates the webview to a URL.
    /// </summary>
    private void NavigateWry(string url)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            WryInterop.WebviewNavigate(_wryWebview, url);
        }
    }

    /// <summary>
    /// Gets the window visibility.
    /// </summary>
    private bool GetVisibleWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return true;

        return WryInterop.WindowIsVisible(_wryWindow);
    }

    /// <summary>
    /// Sets the window visibility.
    /// </summary>
    private void SetVisibleWry(bool visible)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetVisible(_wryWindow, visible);
        }
    }

    /// <summary>
    /// Focuses the window.
    /// </summary>
    private void FocusWry()
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowFocus(_wryWindow);
        }
    }

    /// <summary>
    /// Sets min/max size using wry-ffi.
    /// </summary>
    private void SetMinSizeWry(int minWidth, int minHeight)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetMinSize(_wryWindow, minWidth, minHeight);
        }
    }

    private void SetMaxSizeWry(int maxWidth, int maxHeight)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetMaxSize(_wryWindow, maxWidth, maxHeight);
        }
    }

    /// <summary>
    /// Gets whether the window is maximized.
    /// </summary>
    private bool GetMaximizedWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return _startupParameters.Maximized;

        return WryInterop.WindowIsMaximized(_wryWindow);
    }

    /// <summary>
    /// Gets whether the window is minimized.
    /// </summary>
    private bool GetMinimizedWry()
    {
        if (_wryWindow == IntPtr.Zero)
            return _startupParameters.Minimized;

        return WryInterop.WindowIsMinimized(_wryWindow);
    }

    // ========================================================================
    // Features not yet implemented in wry-ffi - throw NotSupportedException
    // ========================================================================

    private static void ThrowNotSupported(string feature)
    {
        throw new NotSupportedException($"{feature} is not yet supported with the wry-ffi backend. " +
            "This feature will be implemented in a future release.");
    }

    // ========================================================================
    // Invoke / Thread dispatch (simplified - runs inline for wry-ffi)
    // ========================================================================

    /// <summary>
    /// Dispatches an action. For wry-ffi, actions run inline since
    /// the event loop model doesn't support arbitrary dispatch.
    /// </summary>
    private TauriWindow InvokeWry(Action workItem)
    {
        // wry-ffi runs on a single thread with the event loop
        // For now, just execute inline - proper dispatch would require
        // sending a user event and processing it in the event loop callback
        workItem();
        return this;
    }

    /// <summary>
    /// Closes the window by signaling exit.
    /// </summary>
    private void CloseWry()
    {
        _shouldExit = true;
        // If we have a proxy, use it to request exit
        _eventLoopProxy?.RequestExit();
    }

    /// <summary>
    /// Navigates to a URL after window creation using wry-ffi.
    /// </summary>
    private void NavigateToUrlWry(string url)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            WryInterop.WebviewNavigate(_wryWebview, url);
        }
    }

    /// <summary>
    /// Loads raw HTML string into the webview.
    /// Note: wry-ffi doesn't have a direct LoadHtml, so we use a data URL.
    /// </summary>
    private void LoadHtmlWry(string html)
    {
        if (_wryWebview != IntPtr.Zero)
        {
            // Convert HTML to data URL
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            var base64 = Convert.ToBase64String(bytes);
            var dataUrl = $"data:text/html;base64,{base64}";
            WryInterop.WebviewNavigate(_wryWebview, dataUrl);
        }
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
}
