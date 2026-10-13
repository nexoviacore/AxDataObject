namespace AxData.Api.Contracts;

public sealed record AxDataErrorResponse(
    string Status,
    string Code,
    string Message,
    string CorrelationId,
    long? CurrentVersion,
    IReadOnlyList<object> Errors)
{
    public static AxDataErrorResponse Create(
        string code,
        string message,
        string correlationId,
        long? currentVersion = null) =>
        new("error", code, message, correlationId, currentVersion, []);
}
