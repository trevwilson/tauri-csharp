// Originally from Photino.NET (https://github.com/tryphotino/photino.NET)
// Modified by tauri-csharp project - 2025
// Changes: Namespace rename from Photino.NET to TauriCSharp

namespace TauriCSharp;

// Legacy callback delegate types from Photino era.
// These are used purely as C#-side event handlers stored in TauriNativeParameters.
// They are NOT used for native interop — all FFI goes through WryInterop/WryDelegates.

public delegate byte CppClosingDelegate();
public delegate void CppFocusInDelegate();
public delegate void CppFocusOutDelegate();
public delegate void CppResizedDelegate(int width, int height);
public delegate void CppMaximizedDelegate();
public delegate void CppRestoredDelegate();
public delegate void CppMinimizedDelegate();
public delegate void CppMovedDelegate(int x, int y);
public delegate void CppWebMessageReceivedDelegate(string message);
public delegate IntPtr CppWebResourceRequestedDelegate(string url, out int outNumBytes, out string outContentType);
public delegate int CppGetAllMonitorsDelegate(in NativeMonitor monitor);
