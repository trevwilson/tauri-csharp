namespace TauriCSharp.Ipc;

/// <summary>
/// Extension methods for TauriWindow to simplify IPC usage.
/// </summary>
public static class TauriWindowIpcExtensions
{
    /// <summary>
    /// Creates a new TauriIpc instance attached to this window.
    /// </summary>
    /// <remarks>
    /// The returned TauriIpc instance should be disposed when no longer needed.
    /// Typically, create one TauriIpc per window and keep it for the window's lifetime.
    /// </remarks>
    /// <example>
    /// <code>
    /// var ipc = window.CreateIpc();
    /// ipc.On("greeting", msg => $"Hello, {msg.GetPayload&lt;string&gt;()}!");
    /// var response = await ipc.RequestAsync&lt;string&gt;("getVersion");
    /// </code>
    /// </example>
    public static TauriIpc CreateIpc(this TauriWindow window)
    {
        return new TauriIpc(window);
    }

    /// <summary>
    /// Sends a structured message to JavaScript.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="messageType">The message type identifier.</param>
    /// <param name="payload">Optional payload object (will be JSON serialized).</param>
    public static TauriWindow SendMessage(this TauriWindow window, string messageType, object? payload = null)
    {
        var message = new TauriMessage(messageType, payload);
        window.SendWebMessage(message.ToJson());
        return window;
    }

    /// <summary>
    /// Sends a structured message to JavaScript asynchronously.
    /// </summary>
    public static async Task<TauriWindow> SendMessageAsync(this TauriWindow window, string messageType, object? payload = null)
    {
        var message = new TauriMessage(messageType, payload);
        await window.SendWebMessageAsync(message.ToJson());
        return window;
    }
}
