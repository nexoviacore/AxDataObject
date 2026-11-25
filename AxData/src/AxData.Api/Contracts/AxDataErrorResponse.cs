namespace AxData.Api.Contracts;

public sealed record AxDataErrorResponse(
    string Status,
    string Code,
    string Message,
    string CorrelationId,
    IReadOnlyList<object> Errors)
{
    public static AxDataErrorResponse Create(
        string code,
        string message,
        string correlationId) =>
        new("error", code, message, correlationId, []);
}
