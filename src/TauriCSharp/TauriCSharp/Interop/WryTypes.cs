// wry-ffi C# bindings - Type definitions
// These structs must match the Rust FFI types exactly for correct marshalling
// Based on Velox's runtime-wry-ffi types

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

// ============================================================================
// Basic Types
// ============================================================================

/// <summary>
/// RGBA color matching Rust WryColor.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryColor
{
    public byte Red;
    public byte Green;
    public byte Blue;
    public byte Alpha;

    public WryColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}

/// <summary>
/// Point with f64 coordinates matching Rust WryPoint.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryPoint
{
    public double X;
    public double Y;

    public WryPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Size with f64 dimensions matching Rust WrySize.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WrySize
{
    public double Width;
    public double Height;

    public WrySize(double width, double height)
    {
        Width = width;
        Height = height;
    }
}

// ============================================================================
// Enums
// ============================================================================

/// <summary>
/// Window theme matching Rust WryWindowTheme.
/// </summary>
internal enum WryWindowTheme
{
    Unspecified = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>
/// macOS activation policy matching Rust WryActivationPolicy.
/// </summary>
internal enum WryActivationPolicy
{
    Regular = 0,
    Accessory = 1,
    Prohibited = 2,
}

/// <summary>
/// Event loop control flow matching Rust WryEventLoopControlFlow.
/// </summary>
internal enum WryEventLoopControlFlow
{
    Poll = 0,
    Wait = 1,
    Exit = 2,
}

/// <summary>
/// User attention type matching Rust WryUserAttentionType.
/// </summary>
internal enum WryUserAttentionType
{
    Informational = 0,
    Critical = 1,
}

/// <summary>
/// Resize direction matching Rust WryResizeDirection.
/// </summary>
internal enum WryResizeDirection
{
    East = 0,
    North = 1,
    NorthEast = 2,
    NorthWest = 3,
    South = 4,
    SouthEast = 5,
    SouthWest = 6,
    West = 7,
}

/// <summary>
/// Message dialog level matching Rust WryMessageDialogLevel.
/// </summary>
internal enum WryMessageDialogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

/// <summary>
/// Message dialog buttons matching Rust WryMessageDialogButtons.
/// </summary>
internal enum WryMessageDialogButtons
{
    Ok = 0,
    OkCancel = 1,
    YesNo = 2,
    YesNoCancel = 3,
}

// ============================================================================
// Config Structs
// ============================================================================

/// <summary>
/// Window creation config matching Rust WryWindowConfig.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryWindowConfig
{
    public uint Width;
    public uint Height;
    public IntPtr Title; // *const c_char

    public static WryWindowConfig CreateDefault() => new()
    {
        Width = 800,
        Height = 600,
        Title = IntPtr.Zero,
    };
}

/// <summary>
/// Webview creation config matching Rust WryWebviewConfig.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryWebviewConfig
{
    public IntPtr Url;  // *const c_char
    public WryCustomProtocolList CustomProtocols;
    [MarshalAs(UnmanagedType.U1)]
    public bool Devtools;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsChild;
    public double X;
    public double Y;
    public double Width;
    public double Height;

    public static WryWebviewConfig CreateDefault() => new()
    {
        Url = IntPtr.Zero,
        CustomProtocols = default,
        Devtools = true,
        IsChild = false,
        X = 0,
        Y = 0,
        Width = 0,
        Height = 0,
    };
}

// ============================================================================
// Custom Protocol Types
// ============================================================================

/// <summary>
/// HTTP header for custom protocol matching Rust WryCustomProtocolHeader.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolHeader
{
    public IntPtr Name;  // *const c_char
    public IntPtr Value; // *const c_char
}

/// <summary>
/// Header list matching Rust WryCustomProtocolHeaderList.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolHeaderList
{
    public IntPtr Headers; // *const WryCustomProtocolHeader
    public nuint Count;
}

/// <summary>
/// Binary buffer matching Rust WryCustomProtocolBuffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolBuffer
{
    public IntPtr Ptr; // *const u8
    public nuint Len;
}

/// <summary>
/// Request from webview matching Rust WryCustomProtocolRequest.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolRequest
{
    public IntPtr Url;       // *const c_char
    public IntPtr Method;    // *const c_char
    public WryCustomProtocolHeaderList Headers;
    public WryCustomProtocolBuffer Body;
    public IntPtr WebviewId; // *const c_char
}

/// <summary>
/// Response to webview matching Rust WryCustomProtocolResponse.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolResponse
{
    public ushort Status;
    public WryCustomProtocolHeaderList Headers;
    public WryCustomProtocolBuffer Body;
    public IntPtr MimeType;  // *const c_char
    public IntPtr Free;      // WryCustomProtocolResponseFree
    public IntPtr UserData;  // *mut c_void
}

/// <summary>
/// Protocol definition for registration matching Rust WryCustomProtocolDefinition.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolDefinition
{
    public IntPtr Scheme;   // *const c_char
    public IntPtr Handler;  // WryCustomProtocolHandler function pointer
    public IntPtr UserData; // *mut c_void
}

/// <summary>
/// List of protocol definitions matching Rust WryCustomProtocolList.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryCustomProtocolList
{
    public IntPtr Protocols; // *const WryCustomProtocolDefinition
    public nuint Count;
}

// ============================================================================
// Dialog Types
// ============================================================================

/// <summary>
/// File filter for dialogs matching Rust WryDialogFilter.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryDialogFilter
{
    public IntPtr Label;          // *const c_char
    public IntPtr Extensions;     // *const *const c_char
    public nuint ExtensionCount;
}

/// <summary>
/// Open dialog options matching Rust WryDialogOpenOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryDialogOpenOptions
{
    public IntPtr Title;       // *const c_char
    public IntPtr DefaultPath; // *const c_char
    public IntPtr Filters;     // *const WryDialogFilter
    public nuint FilterCount;
    [MarshalAs(UnmanagedType.U1)]
    public bool AllowDirectories;
    [MarshalAs(UnmanagedType.U1)]
    public bool AllowMultiple;
}

/// <summary>
/// Save dialog options matching Rust WryDialogSaveOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryDialogSaveOptions
{
    public IntPtr Title;       // *const c_char
    public IntPtr DefaultPath; // *const c_char
    public IntPtr DefaultName; // *const c_char
    public IntPtr Filters;     // *const WryDialogFilter
    public nuint FilterCount;
}

/// <summary>
/// Dialog selection result matching Rust WryDialogSelection.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryDialogSelection
{
    public IntPtr Paths; // *mut *mut c_char
    public nuint Count;
}

/// <summary>
/// Message dialog options matching Rust WryMessageDialogOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryMessageDialogOptions
{
    public IntPtr Title;       // *const c_char
    public IntPtr Message;     // *const c_char
    public WryMessageDialogLevel Level;
    public WryMessageDialogButtons Buttons;
    public IntPtr OkLabel;     // *const c_char
    public IntPtr CancelLabel; // *const c_char
    public IntPtr YesLabel;    // *const c_char
    public IntPtr NoLabel;     // *const c_char
}

/// <summary>
/// Confirm dialog options matching Rust WryConfirmDialogOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryConfirmDialogOptions
{
    public IntPtr Title;       // *const c_char
    public IntPtr Message;     // *const c_char
    public WryMessageDialogLevel Level;
    public IntPtr OkLabel;     // *const c_char
    public IntPtr CancelLabel; // *const c_char
}

/// <summary>
/// Ask dialog options matching Rust WryAskDialogOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryAskDialogOptions
{
    public IntPtr Title;   // *const c_char
    public IntPtr Message; // *const c_char
    public WryMessageDialogLevel Level;
    public IntPtr YesLabel; // *const c_char
    public IntPtr NoLabel;  // *const c_char
}

/// <summary>
/// Prompt dialog options matching Rust WryPromptDialogOptions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryPromptDialogOptions
{
    public IntPtr Title;        // *const c_char
    public IntPtr Message;      // *const c_char
    public IntPtr Placeholder;  // *const c_char
    public IntPtr DefaultValue; // *const c_char
    public IntPtr OkLabel;      // *const c_char
    public IntPtr CancelLabel;  // *const c_char
}

/// <summary>
/// Prompt dialog result matching Rust WryPromptDialogResult.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WryPromptDialogResult
{
    public IntPtr Value; // *mut c_char
    [MarshalAs(UnmanagedType.U1)]
    public bool Accepted;
}

// ============================================================================
// Error Handling (legacy - kept for compatibility)
// ============================================================================

/// <summary>
/// Error codes for legacy API compatibility.
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
