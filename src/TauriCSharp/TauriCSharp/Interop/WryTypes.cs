// wry-ffi C# bindings - Type definitions
// These structs must match the Rust FFI types exactly for correct marshalling

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// Window creation parameters matching Rust WryWindowParams.
/// All strings are UTF-8 null-terminated pointers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryWindowParams
{
    // Strings (UTF-8, null-terminated)
    public IntPtr Title;
    public IntPtr Url;
    public IntPtr Html;
    public IntPtr UserAgent;
    public IntPtr DataDirectory;

    // Dimensions
    public int X;
    public int Y;
    public uint Width;
    public uint Height;
    public uint MinWidth;
    public uint MinHeight;
    public uint MaxWidth;   // 0 = no max
    public uint MaxHeight;  // 0 = no max

    // Flags (C bool = 1 byte, but Rust bool is also 1 byte)
    [MarshalAs(UnmanagedType.U1)]
    public bool Resizable;
    [MarshalAs(UnmanagedType.U1)]
    public bool Fullscreen;
    [MarshalAs(UnmanagedType.U1)]
    public bool Maximized;
    [MarshalAs(UnmanagedType.U1)]
    public bool Minimized;
    [MarshalAs(UnmanagedType.U1)]
    public bool Visible;
    [MarshalAs(UnmanagedType.U1)]
    public bool Transparent;
    [MarshalAs(UnmanagedType.U1)]
    public bool Decorations;
    [MarshalAs(UnmanagedType.U1)]
    public bool AlwaysOnTop;
    [MarshalAs(UnmanagedType.U1)]
    public bool DevtoolsEnabled;
    [MarshalAs(UnmanagedType.U1)]
    public bool AutoplayEnabled;

    /// <summary>
    /// Creates default parameters matching Rust's Default implementation.
    /// </summary>
    public static WryWindowParams CreateDefault()
    {
        return new WryWindowParams
        {
            Title = IntPtr.Zero,
            Url = IntPtr.Zero,
            Html = IntPtr.Zero,
            UserAgent = IntPtr.Zero,
            DataDirectory = IntPtr.Zero,
            X = 0,
            Y = 0,
            Width = 800,
            Height = 600,
            MinWidth = 0,
            MinHeight = 0,
            MaxWidth = 0,
            MaxHeight = 0,
            Resizable = true,
            Fullscreen = false,
            Maximized = false,
            Minimized = false,
            Visible = true,
            Transparent = false,
            Decorations = true,
            AlwaysOnTop = false,
            DevtoolsEnabled = true,
            AutoplayEnabled = false,
        };
    }
}

/// <summary>
/// Window size matching Rust WrySize.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WrySize
{
    public uint Width;
    public uint Height;

    public WrySize(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public static WrySize Default => new(0, 0);
}

/// <summary>
/// Window position matching Rust WryPosition.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryPosition
{
    public int X;
    public int Y;

    public WryPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static WryPosition Default => new(0, 0);
}

/// <summary>
/// Result type for FFI operations matching Rust WryResult.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryResult
{
    [MarshalAs(UnmanagedType.U1)]
    public bool Success;
    public int ErrorCode;
    public IntPtr ErrorMessage;  // Pointer to error string (do not free - points to thread-local storage)

    public bool IsOk => Success;
    public bool IsError => !Success;

    /// <summary>
    /// Gets the error message if present. Does not free the pointer.
    /// </summary>
    public string? GetErrorMessage()
    {
        if (ErrorMessage == IntPtr.Zero)
            return null;
        return Marshal.PtrToStringUTF8(ErrorMessage);
    }

    /// <summary>
    /// Throws an exception if the result indicates an error.
    /// </summary>
    public void ThrowIfError()
    {
        if (!Success)
        {
            var message = GetErrorMessage() ?? $"wry-ffi error code {ErrorCode}";
            throw new WryException(message, (WryErrorCode)ErrorCode);
        }
    }
}

/// <summary>
/// Error codes matching Rust WryErrorCode enum.
/// </summary>
public enum WryErrorCode
{
    Success = 0,
    InvalidHandle = 1,
    WindowCreationFailed = 2,
    WebviewCreationFailed = 3,
    NavigationFailed = 4,
    ScriptError = 5,
    ProtocolError = 6,
    InvalidParameter = 7,
    NotSupported = 8,
    DialogCancelled = 9,
    NotificationFailed = 10,
    IconLoadFailed = 11,
    EventLoopError = 12,
    Unknown = 255,
}

/// <summary>
/// Exception thrown when wry-ffi operations fail.
/// </summary>
public class WryException : Exception
{
    public WryErrorCode ErrorCode { get; }

    internal WryException(string message, WryErrorCode errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    internal WryException(string message, WryErrorCode errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
