using System.Collections.Concurrent;

namespace TauriCSharp.Ipc;

/// <summary>
/// Manages IPC (inter-process communication) between C# and JavaScript with support
/// for request/response patterns and message routing.
/// </summary>
public class TauriIpc : IDisposable
{
    private readonly TauriWindow _window;
    private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, Func<TauriMessage, Task<object?>>> _handlers = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private bool _disposed;

    /// <summary>
    /// Event raised when a message is received that doesn't match a pending request or handler.
    /// </summary>
    public event EventHandler<TauriMessage>? UnhandledMessage;

    /// <summary>
    /// Creates a new TauriIpc instance attached to the specified window.
    /// </summary>
    public TauriIpc(TauriWindow window)
    {
        _window = window;
        _window.RegisterWebMessageReceivedHandler(OnWebMessageReceived);
    }

    /// <summary>
    /// Registers a handler for messages of the specified type.
    /// </summary>
    /// <param name="messageType">The message type to handle.</param>
    /// <param name="handler">Async handler that receives the message and returns an optional response payload.</param>
    public TauriIpc On(string messageType, Func<TauriMessage, Task<object?>> handler)
    {
        _handlers[messageType] = handler;
        return this;
    }

    /// <summary>
    /// Registers a synchronous handler for messages of the specified type.
    /// </summary>
    public TauriIpc On(string messageType, Func<TauriMessage, object?> handler)
    {
        _handlers[messageType] = msg => Task.FromResult(handler(msg));
        return this;
    }

    /// <summary>
    /// Registers a handler that doesn't return a response.
    /// </summary>
    public TauriIpc On(string messageType, Action<TauriMessage> handler)
    {
        _handlers[messageType] = msg =>
        {
            handler(msg);
            return Task.FromResult<object?>(null);
        };
        return this;
    }

    /// <summary>
    /// Removes a handler for the specified message type.
    /// </summary>
    public TauriIpc Off(string messageType)
    {
        _handlers.TryRemove(messageType, out _);
        return this;
    }

    /// <summary>
    /// Sends a message to JavaScript without expecting a response.
    /// </summary>
    public void Send(string messageType, object? payload = null)
    {
        var message = new TauriMessage(messageType, payload);
        _window.SendWebMessage(message.ToJson());
    }

    /// <summary>
    /// Sends a request to JavaScript and waits for a response.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="payload">Optional payload.</param>
    /// <param name="timeout">Optional timeout (defaults to 30 seconds).</param>
    /// <returns>The response message.</returns>
    /// <exception cref="TauriIpcException">Thrown if the request times out or returns an error.</exception>
    public async Task<TauriMessage> RequestAsync(string messageType, object? payload = null, TimeSpan? timeout = null)
    {
        var request = TauriMessage.CreateRequest(messageType, payload);
        var pending = new PendingRequest(request.Id!, timeout ?? _defaultTimeout);

        _pendingRequests[request.Id!] = pending;

        try
        {
            _window.SendWebMessage(request.ToJson());
            return await pending.Task;
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id!, out _);
        }
    }

    /// <summary>
    /// Sends a request and deserializes the response payload to the specified type.
    /// </summary>
    public async Task<T?> RequestAsync<T>(string messageType, object? payload = null, TimeSpan? timeout = null)
    {
        var response = await RequestAsync(messageType, payload, timeout);

        if (response.Error != null)
            throw new TauriIpcException($"Request '{messageType}' failed: {response.Error}", response.Id);

        return response.GetPayload<T>();
    }

    private void OnWebMessageReceived(object? sender, string rawMessage)
    {
        if (!TauriMessage.TryParse(rawMessage, out var message) || message == null)
        {
            // Not a structured message - could be legacy format
            return;
        }

        if (message.IsResponse && message.Id != null)
        {
            // This is a response to a pending request
            if (_pendingRequests.TryGetValue(message.Id, out var pending))
            {
                pending.Complete(message);
                return;
            }
        }

        // Try to find a handler for this message type
        if (!string.IsNullOrEmpty(message.Type) && _handlers.TryGetValue(message.Type, out var handler))
        {
            _ = HandleMessageAsync(message, handler);
            return;
        }

        // No handler found
        UnhandledMessage?.Invoke(this, message);
    }

    private async Task HandleMessageAsync(TauriMessage message, Func<TauriMessage, Task<object?>> handler)
    {
        try
        {
            var result = await handler(message);

            // If the message has a correlation ID, send a response
            if (message.Id != null)
            {
                var response = TauriMessage.CreateResponse(message.Id, result);
                _window.SendWebMessage(response.ToJson());
            }
        }
        catch (Exception ex)
        {
            // If the message has a correlation ID, send an error response
            if (message.Id != null)
            {
                var errorResponse = TauriMessage.CreateErrorResponse(message.Id, ex.Message);
                _window.SendWebMessage(errorResponse.ToJson());
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe from window events to prevent leaks/double handling
        _window.WebMessageReceived -= OnWebMessageReceived;

        // Cancel all pending requests
        foreach (var pending in _pendingRequests.Values)
        {
            pending.Cancel();
        }
        _pendingRequests.Clear();
        _handlers.Clear();

        GC.SuppressFinalize(this);
    }

    private class PendingRequest
    {
        private readonly TaskCompletionSource<TauriMessage> _tcs;
        private readonly CancellationTokenSource _cts;
        private readonly string _correlationId;

        public Task<TauriMessage> Task => _tcs.Task;

        public PendingRequest(string correlationId, TimeSpan timeout)
        {
            _correlationId = correlationId;
            _tcs = new TaskCompletionSource<TauriMessage>();
            _cts = new CancellationTokenSource(timeout);
            _cts.Token.Register(() =>
            {
                _tcs.TrySetException(new TauriIpcException($"Request '{correlationId}' timed out.", correlationId));
            });
        }

        public void Complete(TauriMessage response)
        {
            if (response.Error != null)
            {
                _tcs.TrySetException(new TauriIpcException(response.Error, _correlationId));
            }
            else
            {
                _tcs.TrySetResult(response);
            }
        }

        public void Cancel()
        {
            _cts.Cancel();
            _tcs.TrySetCanceled();
        }
    }
}
