// Originally from Photino.NET (https://github.com/tryphotino/photino.NET)
// Modified by tauri-csharp project - 2025
// Changes: Namespace rename from Photino.NET to TauriCSharp

using System.Drawing;
using System.Runtime.InteropServices;

namespace TauriCSharp;

/// <summary>
/// Represents a 2D rectangle in a native (integer-based) coordinate system.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{
    public int x, y;
    public int width, height;
}

/// <summary>
/// The <c>NativeMonitor</c> structure is used for communicating information about the monitor setup
/// to and from native system calls. This structure is defined in a sequential layout for direct,
/// unmanaged access to the underlying memory.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMonitor
{
    public NativeRect monitor;
    public NativeRect work;
    public double scale;
}

/// <summary>
/// Represents information about a monitor.
/// </summary>
public readonly struct Monitor(string name, Rectangle monitor, Rectangle work, double scale)
{
    /// <summary>
    /// The display name of the monitor (e.g. "HDMI-1", "eDP-1").
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// The full area of the monitor.
    /// </summary>
    public readonly Rectangle MonitorArea = monitor;

    /// <summary>
    /// The working area of the monitor excluding taskbars, docked windows, and docked tool bars.
    /// </summary>
    public readonly Rectangle WorkArea = work;

    /// <summary>
    /// The scale factor of the monitor. Standard value is 1.0.
    /// </summary>
    public readonly double Scale = scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct using native structures.
    /// </summary>
    internal Monitor(NativeRect monitor, NativeRect work, double scale)
        : this("", new Rectangle(monitor.x, monitor.y, monitor.width, monitor.height), new Rectangle(work.x, work.y, work.width, work.height), scale)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct using a native monitor structure.
    /// </summary>
    internal Monitor(NativeMonitor nativeMonitor)
        : this(nativeMonitor.monitor, nativeMonitor.work, nativeMonitor.scale)
    { }

    /// <summary>
    /// Backward-compatible constructor without name.
    /// </summary>
    public Monitor(Rectangle monitor, Rectangle work, double scale)
        : this("", monitor, work, scale)
    { }
}
