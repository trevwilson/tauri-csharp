using System.Collections.Concurrent;
using TauriCSharp.Interop;

namespace TauriCSharp;

/// <summary>
/// Provides global keyboard shortcut registration and management.
/// </summary>
/// <remarks>
/// On Linux/WSL2 with Wayland, global shortcuts may only work when X11 applications have focus,
/// not system-wide. This is a platform limitation.
/// </remarks>
public static class GlobalShortcuts
{
    private static readonly ConcurrentDictionary<uint, Action<uint>> _callbacks = new();

    /// <summary>
    /// Registers a global keyboard shortcut.
    /// </summary>
    /// <param name="accelerator">
    /// Accelerator string (e.g. "CmdOrCtrl+Shift+T", "Alt+F4", "Ctrl+A").
    /// Modifiers: Alt, Ctrl, CmdOrCtrl, Meta, Shift, Super.
    /// Keys: A-Z, 0-9, F1-F24, Space, Enter, Tab, Escape, etc.
    /// </param>
    /// <param name="callback">Action invoked when the shortcut is triggered. Receives the shortcut ID.</param>
    /// <returns>Non-zero shortcut ID on success, or 0 on failure.</returns>
    public static uint Register(string accelerator, Action<uint> callback)
    {
        var id = WryInterop.ShortcutRegister(accelerator);
        if (id != 0)
        {
            _callbacks[id] = callback;
        }
        return id;
    }

    /// <summary>
    /// Unregisters a global shortcut by ID.
    /// </summary>
    /// <param name="shortcutId">The shortcut ID returned by <see cref="Register"/>.</param>
    /// <returns>True if successfully unregistered.</returns>
    public static bool Unregister(uint shortcutId)
    {
        _callbacks.TryRemove(shortcutId, out _);
        return WryInterop.ShortcutUnregister(shortcutId);
    }

    /// <summary>
    /// Unregisters all global shortcuts.
    /// </summary>
    /// <returns>True if successfully unregistered all.</returns>
    public static bool UnregisterAll()
    {
        _callbacks.Clear();
        return WryInterop.ShortcutUnregisterAll();
    }

    /// <summary>
    /// Dispatches a global shortcut event. Called by the event loop.
    /// </summary>
    internal static void DispatchShortcutEvent(uint shortcutId)
    {
        if (_callbacks.TryGetValue(shortcutId, out var callback))
        {
            callback(shortcutId);
        }
    }
}

/// <summary>
/// Keyboard modifier flags for building shortcut accelerators.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Meta = 4,
    Shift = 8,
}
