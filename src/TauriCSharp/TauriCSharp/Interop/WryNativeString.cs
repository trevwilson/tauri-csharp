// wry-ffi C# bindings - Native string wrapper
// IDisposable wrapper for strings allocated by wry-ffi that need cleanup via wry_string_free

using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// Disposable wrapper for native strings allocated by wry-ffi.
/// Automatically calls wry_string_free when disposed.
///
/// Usage:
/// <code>
/// using var str = new WryNativeString(WryInterop.WebViewGetUrl(window));
/// return str.Value;
/// </code>
/// </summary>
internal readonly struct WryNativeString(IntPtr ptr) : IDisposable
{
    private readonly IntPtr _ptr = ptr;

    /// <summary>
    /// Gets the managed string value, or null if the pointer is null.
    /// </summary>
    public string? Value => _ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(_ptr) : null;

    /// <summary>
    /// Returns true if this contains a valid (non-null) string pointer.
    /// </summary>
    public bool HasValue => _ptr != IntPtr.Zero;

    public void Dispose()
    {
        if (_ptr != IntPtr.Zero)
        {
            WryInterop.StringFree(_ptr);
        }
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// Note: This still requires proper disposal of the WryNativeString.
    /// </summary>
    public static implicit operator string?(WryNativeString ns) => ns.Value;
}

/// <summary>
/// RAII wrapper for native strings that need to be allocated for passing to native code.
/// Allocates a UTF-8 string and frees it on dispose.
/// </summary>
internal readonly struct MarshalledUtf8String : IDisposable
{
    private readonly IntPtr _ptr;

    public MarshalledUtf8String(string? value)
    {
        if (value == null)
        {
            _ptr = IntPtr.Zero;
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value + '\0');
            _ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, _ptr, bytes.Length);
        }
    }

    /// <summary>
    /// Gets the native pointer to the UTF-8 string.
    /// </summary>
    public IntPtr Pointer => _ptr;

    public void Dispose()
    {
        if (_ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ptr);
        }
    }
}
