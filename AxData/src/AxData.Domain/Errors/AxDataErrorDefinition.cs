namespace AxData.Domain.Errors;

public sealed record AxDataErrorDefinition(
    string Code,
    string Category,
    string Message,
    int HttpStatusCode);
