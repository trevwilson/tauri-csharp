// Originally from Photino.NET (https://github.com/tryphotino/photino.NET)
// Modified by tauri-csharp project - 2025
// Changes: Namespace rename from Photino.NET to TauriCSharp

using System.Runtime.InteropServices;

namespace TauriCSharp;

// These are for the callbacks from C++ to C#.
// These are wired up automatically in the TauriWindow (.NET) constructor.

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate byte CppClosingDelegate();    //C++ uses 1 byte for bool, C# uses 4 bytes
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppFocusInDelegate();
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppFocusOutDelegate();
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppResizedDelegate(int width, int height);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppMaximizedDelegate();
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppRestoredDelegate();
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppMinimizedDelegate();
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppMovedDelegate(int x, int y);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate void CppWebMessageReceivedDelegate(string message);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate IntPtr CppWebResourceRequestedDelegate(string url, out int outNumBytes, out string outContentType);

// These are sent in during the request
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] public delegate int CppGetAllMonitorsDelegate(in NativeMonitor monitor);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void InvokeCallback();
