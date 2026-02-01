// wry-ffi C# bindings - Callback registry for GCHandle pinning
// Fixes the delegate GC hole by pinning delegates for the lifetime of the window

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TauriCSharp.Interop;

/// <summary>
/// Registry that pins delegate instances using GCHandle to prevent garbage collection
/// while they are being used as native callbacks.
///
/// This fixes the critical GC hole where delegates passed to native code could be
/// collected by the GC, causing crashes or undefined behavior.
/// </summary>
internal sealed class WryCallbackRegistry : IDisposable
{
    private readonly ConcurrentDictionary<IntPtr, CallbackSet> _callbacksByWindow = new();
    private bool _disposed;

    /// <summary>
    /// Set of pinned callbacks for a single window.
    /// </summary>
    private sealed class CallbackSet : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<GCHandle> _handles = [];
        private bool _disposed;

        public void Add(Delegate callback)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                var handle = GCHandle.Alloc(callback);
                _handles.Add(handle);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                foreach (var handle in _handles)
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
                _handles.Clear();
            }
        }
    }

    /// <summary>
    /// Register a callback delegate for a window, pinning it to prevent GC collection.
    /// </summary>
    /// <param name="windowHandle">The native window handle</param>
    /// <param name="callback">The delegate to pin</param>
    public void Register(IntPtr windowHandle, Delegate callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var set = _callbacksByWindow.GetOrAdd(windowHandle, _ => new CallbackSet());
        set.Add(callback);
    }

    /// <summary>
    /// Unregister all callbacks for a window, freeing their GCHandles.
    /// Call this when the window is destroyed.
    /// </summary>
    /// <param name="windowHandle">The native window handle</param>
    public void Unregister(IntPtr windowHandle)
    {
        if (_callbacksByWindow.TryRemove(windowHandle, out var set))
        {
            set.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _callbacksByWindow)
        {
            kvp.Value.Dispose();
        }
        _callbacksByWindow.Clear();
    }
}

/// <summary>
/// Scoped GCHandle wrapper for short-lived delegate pinning (e.g., wry_invoke_sync).
/// </summary>
internal readonly struct PinnedDelegate : IDisposable
{
    private readonly GCHandle _handle;

    public PinnedDelegate(Delegate callback)
    {
        _handle = GCHandle.Alloc(callback);
    }

    public void Dispose()
    {
        if (_handle.IsAllocated)
            _handle.Free();
    }
}
