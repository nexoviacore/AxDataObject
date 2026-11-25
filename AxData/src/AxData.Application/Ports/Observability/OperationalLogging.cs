namespace AxData.Application.Ports;

public enum OperationalLogLevel
{
    Information,
    Warning,
    Error
}

public enum OperationalLogDetail
{
    Baseline,
    Diagnostic
}

public sealed record OperationalEvent(
    string Name,
    string Stage,
    string Outcome,
    OperationalLogLevel Level = OperationalLogLevel.Information,
    OperationalLogDetail Detail = OperationalLogDetail.Baseline,
    string? CorrelationId = null,
    string? TenantId = null,
    string? TransId = null,
    string? ObjectId = null,
    string? ErrorCode = null,
    string? ReasonCode = null,
    string? Provider = null,
    int? HttpStatusCode = null,
    int? Count = null,
    int? SecondaryCount = null,
    int? Attempt = null,
    double? DurationMs = null);

public interface IOperationalEventSink
{
    /// <summary>Writes a value-free operational event containing only approved safe identifiers.</summary>
    void Write(OperationalEvent operationalEvent);
}

public sealed class NullOperationalEventSink : IOperationalEventSink
{
    public static NullOperationalEventSink Instance { get; } = new();

    private NullOperationalEventSink()
    {
    }

    /// <summary>Validates and discards an operational event.</summary>
    public void Write(OperationalEvent operationalEvent)
    {
        ArgumentNullException.ThrowIfNull(operationalEvent);
    }
}
