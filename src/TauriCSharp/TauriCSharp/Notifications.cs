using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp;

/// <summary>
/// Provides cross-platform desktop notification functionality.
/// </summary>
/// <remarks>
/// On Linux, uses D-Bus notifications via notify-rust.
/// On WSL2, D-Bus may not be available — notifications will fail gracefully.
/// On Windows/macOS, uses native notification APIs.
/// </remarks>
public static class Notifications
{
    /// <summary>
    /// Shows a desktop notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body text.</param>
    /// <param name="iconPath">Optional path to an icon file.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. -1 for default, 0 for never, positive for specific duration.</param>
    /// <param name="urgency">Notification urgency level (Linux only).</param>
    /// <returns>True if the notification was shown successfully.</returns>
    public static bool Show(
        string title,
        string body,
        string? iconPath = null,
        int timeoutMs = -1,
        NotificationUrgency urgency = NotificationUrgency.Normal)
    {
        var titlePtr = Marshal.StringToCoTaskMemUTF8(title);
        var bodyPtr = Marshal.StringToCoTaskMemUTF8(body);
        var iconPtr = iconPath != null ? Marshal.StringToCoTaskMemUTF8(iconPath) : IntPtr.Zero;

        try
        {
            var options = new WryNotificationOptions
            {
                Title = titlePtr,
                Body = bodyPtr,
                Icon = iconPtr,
                TimeoutMs = timeoutMs,
                Urgency = (WryNotificationUrgency)urgency,
            };

            return WryInterop.NotificationShow(in options);
        }
        finally
        {
            Marshal.FreeCoTaskMem(titlePtr);
            Marshal.FreeCoTaskMem(bodyPtr);
            if (iconPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(iconPtr);
        }
    }
}

/// <summary>
/// Notification urgency level. Only affects Linux (D-Bus notifications).
/// </summary>
public enum NotificationUrgency
{
    Low = 0,
    Normal = 1,
    Critical = 2,
}
