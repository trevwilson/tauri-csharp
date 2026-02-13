// TauriWindow wry-ffi compatibility layer
// This partial class provides compatibility implementations for features that need
// to be migrated from Photino to the Velox-based wry-ffi backend.
// Updated for the new separate event loop, window, and webview architecture.

using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using TauriCSharp.Interop;

namespace TauriCSharp;

public partial class TauriWindow
{
    // ========================================================================
    // Window Properties - implemented via Velox wry-ffi
    // ========================================================================

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
            // JSON serialization produces a properly escaped JS string literal (including quotes)
            var jsonEscaped = System.Text.Json.JsonSerializer.Serialize(message);
            var script = $"if (window.ipc && window.ipc.receive) {{ window.ipc.receive({jsonEscaped}); }}";
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

    // ========================================================================
    // Monitor JSON Parsing
    // ========================================================================

    /// <summary>
    /// Parses the available monitors JSON array from the native FFI.
    /// </summary>
    private IReadOnlyList<Monitor> ParseMonitorsFromNative()
    {
        var jsonPtr = WryInterop.WindowAvailableMonitors(_wryWindow);
        if (jsonPtr == IntPtr.Zero)
            return [];

        var json = Marshal.PtrToStringUTF8(jsonPtr);
        if (string.IsNullOrEmpty(json))
            return [];

        using var doc = JsonDocument.Parse(json);
        var monitors = new List<Monitor>();

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            monitors.Add(ParseMonitorFromJson(element));
        }

        return monitors;
    }

    /// <summary>
    /// Parses a single monitor JSON from a native FFI pointer.
    /// </summary>
    private static Monitor? ParseSingleMonitorFromNative(IntPtr jsonPtr)
    {
        if (jsonPtr == IntPtr.Zero)
            return null;

        var json = Marshal.PtrToStringUTF8(jsonPtr);
        if (string.IsNullOrEmpty(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        return ParseMonitorFromJson(doc.RootElement);
    }

    /// <summary>
    /// Parses a Monitor from a JSON element matching the Rust monitor_to_json format:
    /// { "name": "...", "scale_factor": 1.0, "position": { "x": 0, "y": 0 }, "size": { "width": 1920, "height": 1080 } }
    /// </summary>
    private static Monitor ParseMonitorFromJson(JsonElement element)
    {
        var name = element.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
        var scale = element.TryGetProperty("scale_factor", out var scaleEl) ? scaleEl.GetDouble() : 1.0;

        int x = 0, y = 0, width = 0, height = 0;
        if (element.TryGetProperty("position", out var posEl))
        {
            x = posEl.TryGetProperty("x", out var xEl) ? (int)xEl.GetDouble() : 0;
            y = posEl.TryGetProperty("y", out var yEl) ? (int)yEl.GetDouble() : 0;
        }
        if (element.TryGetProperty("size", out var sizeEl))
        {
            width = sizeEl.TryGetProperty("width", out var wEl) ? (int)wEl.GetDouble() : 0;
            height = sizeEl.TryGetProperty("height", out var hEl) ? (int)hEl.GetDouble() : 0;
        }

        var monitorArea = new Rectangle(x, y, width, height);
        // Work area is not provided by tao's monitor_to_json — use full area as approximation
        return new Monitor(name, monitorArea, monitorArea, scale);
    }

    // ========================================================================
    // Window Icon
    // ========================================================================

    /// <summary>
    /// Sets the window icon from a file path using wry-ffi.
    /// </summary>
    private void SetIconFileWry(string path)
    {
        if (_wryWindow != IntPtr.Zero)
        {
            WryInterop.WindowSetIconFile(_wryWindow, path);
        }
    }

}
