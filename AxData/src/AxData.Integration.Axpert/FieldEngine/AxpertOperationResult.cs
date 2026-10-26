namespace AxData.Integration.Axpert.FieldEngine;

public sealed record AxpertOperationResult<T>(
    bool Succeeded,
    T Value,
    string? Error,
    bool DefinitionChanged = false)
{
    public static AxpertOperationResult<T> Success(T value) => new(true, value, null);

    public static AxpertOperationResult<T> Failure(string error, bool definitionChanged = false) =>
        new(false, default!, error, definitionChanged);
}
