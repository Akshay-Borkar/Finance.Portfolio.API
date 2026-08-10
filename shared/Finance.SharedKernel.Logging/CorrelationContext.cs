namespace Finance.SharedKernel.Logging;

/// <summary>
/// Ambient (AsyncLocal) holder for the current correlation id, set by <see cref="Middleware.CorrelationIdMiddleware"/>
/// on inbound HTTP requests and read by the MassTransit send/publish filters so outgoing messages
/// carry the same id without every call site having to pass it explicitly.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}
