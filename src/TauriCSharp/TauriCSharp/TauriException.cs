namespace TauriCSharp;

/// <summary>
/// Base exception class for all TauriCSharp-related errors.
/// </summary>
public class TauriException : Exception
{
    public TauriException(string message) : base(message) { }
    public TauriException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when window initialization fails.
/// </summary>
public class TauriInitializationException : TauriException
{
    /// <summary>
    /// List of validation errors that caused initialization to fail.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    public TauriInitializationException(string message) : base(message)
    {
        ValidationErrors = [];
    }

    public TauriInitializationException(string message, IEnumerable<string> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    public TauriInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationErrors = [];
    }
}

/// <summary>
/// Exception thrown when custom URL scheme registration or handling fails.
/// </summary>
public class TauriSchemeException : TauriException
{
    /// <summary>
    /// The scheme that caused the error, if applicable.
    /// </summary>
    public string? Scheme { get; }

    /// <summary>
    /// The URL that was being processed, if applicable.
    /// </summary>
    public string? Url { get; }

    public TauriSchemeException(string message) : base(message) { }

    public TauriSchemeException(string message, string scheme) : base(message)
    {
        Scheme = scheme;
    }

    public TauriSchemeException(string message, string scheme, string url) : base(message)
    {
        Scheme = scheme;
        Url = url;
    }
}

/// <summary>
/// Exception thrown when IPC (inter-process communication) between C# and JavaScript fails.
/// </summary>
public class TauriIpcException : TauriException
{
    /// <summary>
    /// The correlation ID of the failed request, if applicable.
    /// </summary>
    public string? CorrelationId { get; }

    public TauriIpcException(string message) : base(message) { }

    public TauriIpcException(string message, string? correlationId) : base(message)
    {
        CorrelationId = correlationId;
    }

    public TauriIpcException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a platform-specific operation is not supported.
/// </summary>
public class TauriPlatformException(string message, string platform) : TauriException(message)
{
    /// <summary>
    /// The platform where the operation was attempted.
    /// </summary>
    public string Platform { get; } = platform;
}
