using System.Text.Json;
using System.Text.Json.Serialization;

namespace TauriCSharp.Ipc;

/// <summary>
/// Represents a structured message for IPC between C# and JavaScript.
/// </summary>
public class TauriMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Message type identifier. Used to route messages to appropriate handlers.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional correlation ID for request/response patterns.
    /// When set, the receiver should include this ID in their response.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The message payload. Can be any JSON-serializable object.
    /// </summary>
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    /// <summary>
    /// Indicates if this message is a response to a previous request.
    /// </summary>
    [JsonPropertyName("isResponse")]
    public bool IsResponse { get; set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Creates an empty message.
    /// </summary>
    public TauriMessage() { }

    /// <summary>
    /// Creates a message with the specified type.
    /// </summary>
    public TauriMessage(string type)
    {
        Type = type;
    }

    /// <summary>
    /// Creates a message with the specified type and payload.
    /// </summary>
    public TauriMessage(string type, object? payload)
    {
        Type = type;
        if (payload != null)
        {
            Payload = JsonSerializer.SerializeToElement(payload, JsonOptions);
        }
    }

    /// <summary>
    /// Creates a request message with a correlation ID.
    /// </summary>
    public static TauriMessage CreateRequest(string type, object? payload = null)
    {
        return new TauriMessage(type, payload)
        {
            Id = Guid.NewGuid().ToString("N")
        };
    }

    /// <summary>
    /// Creates a response message for a given request.
    /// </summary>
    public static TauriMessage CreateResponse(string correlationId, object? payload = null)
    {
        return new TauriMessage
        {
            Id = correlationId,
            IsResponse = true,
            Payload = payload != null ? JsonSerializer.SerializeToElement(payload, JsonOptions) : null
        };
    }

    /// <summary>
    /// Creates an error response message.
    /// </summary>
    public static TauriMessage CreateErrorResponse(string correlationId, string error)
    {
        return new TauriMessage
        {
            Id = correlationId,
            IsResponse = true,
            Error = error
        };
    }

    /// <summary>
    /// Deserializes the payload to the specified type.
    /// </summary>
    public T? GetPayload<T>()
    {
        if (Payload == null)
            return default;

        return Payload.Value.Deserialize<T>(JsonOptions);
    }

    /// <summary>
    /// Serializes this message to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a TauriMessage.
    /// </summary>
    public static TauriMessage? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TauriMessage>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to parse a JSON string as a TauriMessage.
    /// Returns false if the string is not valid JSON or doesn't have required fields.
    /// </summary>
    public static bool TryParse(string json, out TauriMessage? message)
    {
        message = FromJson(json);
        return message != null;
    }
}
