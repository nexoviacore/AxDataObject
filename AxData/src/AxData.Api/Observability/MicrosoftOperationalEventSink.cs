using AxData.Application.Ports;

namespace AxData.Api.Observability;

public sealed class MicrosoftOperationalEventSink(
    ILogger<MicrosoftOperationalEventSink> logger,
    IHttpContextAccessor httpContextAccessor,
    OperationalLoggingOptions options) : IOperationalEventSink
{
    public void Write(OperationalEvent operationalEvent)
    {
        ArgumentNullException.ThrowIfNull(operationalEvent);
        if (operationalEvent.Detail > options.DetailLevel)
        {
            return;
        }

        var context = httpContextAccessor.HttpContext;
        var correlationId = operationalEvent.CorrelationId ?? context?.TraceIdentifier;
        var logLevel = operationalEvent.Level switch
        {
            OperationalLogLevel.Warning => LogLevel.Warning,
            OperationalLogLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        logger.Log(
            logLevel,
            new EventId(7100, operationalEvent.Name),
            "AxData event {EventName} stage={Stage} outcome={Outcome} correlationId={CorrelationId} tenantId={TenantId} transId={TransId} objectId={ObjectId} version={Version} currentVersion={CurrentVersion} errorCode={ErrorCode} reasonCode={ReasonCode} provider={Provider} httpStatus={HttpStatusCode} count={Count} secondaryCount={SecondaryCount} attempt={Attempt} durationMs={DurationMs}",
            operationalEvent.Name,
            operationalEvent.Stage,
            operationalEvent.Outcome,
            correlationId,
            operationalEvent.TenantId,
            operationalEvent.TransId,
            operationalEvent.ObjectId,
            operationalEvent.Version,
            operationalEvent.CurrentVersion,
            operationalEvent.ErrorCode,
            operationalEvent.ReasonCode,
            operationalEvent.Provider,
            operationalEvent.HttpStatusCode,
            operationalEvent.Count,
            operationalEvent.SecondaryCount,
            operationalEvent.Attempt,
            operationalEvent.DurationMs);
    }
}
