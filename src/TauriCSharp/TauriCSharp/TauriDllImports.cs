// wry-ffi P/Invoke declarations for TauriWindow
// This file replaces the old Photino.Native imports with wry-ffi bindings

using System.Runtime.InteropServices;
using TauriCSharp.Interop;

namespace TauriCSharp;

/// <summary>
/// P/Invoke imports for wry-ffi native library.
/// This partial class contains all native method declarations used by TauriWindow.
/// </summary>
public partial class TauriWindow
{
    // The wry-ffi library provides a different API than Photino.Native.
    // We bridge the old TauriWindow API to wry-ffi here.
    // Imports are defined in TauriCSharp.Interop.WryInterop.

    // ========================================================================
    // Legacy delegate types kept for API compatibility
    // ========================================================================

    // These delegate types are used by the TauriNativeParameters struct
    // and callbacks from wry-ffi to C#. The wry-ffi uses different signatures
    // so we adapt between them.

    // The following imports are no longer needed as we now use WryInterop:
    // - Photino_ctor -> WryInterop.AppCreate + WryInterop.WindowCreate
    // - Photino_WaitForExit -> WryInterop.AppRun
    // - Photino_Close -> WryInterop.WindowClose
    // - Photino_Invoke -> WryInterop.InvokeSync

    // ========================================================================
    // Dialog imports - NOT YET SUPPORTED in wry-ffi
    // These will throw NotSupportedException when used with wry-ffi backend
    // ========================================================================

    // Placeholder - dialogs require platform-specific implementation
    // For now, these will throw when called
}
