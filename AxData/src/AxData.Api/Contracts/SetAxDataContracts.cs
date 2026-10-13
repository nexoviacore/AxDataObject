using System.Text.Json;
using AxData.Application.Ports.Axpert;
using AxData.Application.Set;

namespace AxData.Api.Contracts;

public sealed record SetAxDataRequest(
    bool GetDeps,
    long? ExpectedVersion,
    IReadOnlyDictionary<string, SetDataContainerRequest> Data)
{
    public bool TryToCommand(string objectId, long expectedVersion, out SetAxDataCommand command)
        => TryToCommand(objectId, expectedVersion, correlationId: null, out command);

    public bool TryToCommand(
        string objectId,
        long expectedVersion,
        string? correlationId,
        out SetAxDataCommand command)
    {
        command = new SetAxDataCommand(objectId, expectedVersion, GetDeps, [], correlationId);
        if (Data is null || Data.Count == 0)
        {
            return false;
        }

        var containers = Data.Select(container => new SetDataContainerValue(
            container.Key,
            container.Value.Rows?.Select(row => new SetRowValue(
                row.RowId,
                row.Action,
                row.Fields?.Select(field => new SetFieldValue(field.Key, ToValue(field.Value))).ToArray() ?? [])).ToArray() ?? []))
            .ToArray();
        command = new SetAxDataCommand(objectId, expectedVersion, GetDeps, containers, correlationId);
        return true;
    }

    private static object? ToValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        _ => value.Clone()
    };
}

public sealed record SetDataContainerRequest(IReadOnlyList<SetRowRequest> Rows);

public sealed record SetRowRequest(
    string? RowId,
    string Action,
    IReadOnlyDictionary<string, JsonElement> Fields);

public sealed record SetChangedFieldApiResponse(
    string DcId,
    string RowId,
    string Field,
    object? Value,
    string Source);

public sealed record SetFieldAddressApiResponse(string DcId, string RowId, string Field);

public sealed record SetFieldEvaluationErrorApiResponse(
    SetFieldAddressApiResponse Address,
    string Code,
    string Message);

public sealed record SetAxDataApiResponse(
    string Status,
    string ObjectId,
    long Version,
    IReadOnlyList<SetCreatedRow> CreatedRows,
    IReadOnlyList<SetChangedFieldApiResponse> Changed,
    IReadOnlyList<SetFieldEvaluationErrorApiResponse> Errors,
    string CorrelationId)
{
    public static SetAxDataApiResponse From(SetAxDataResponse response, string correlationId) => new(
        response.Errors.Count == 0 ? "success" : "validation_error",
        response.ObjectId,
        response.Version,
        response.CreatedRows,
        response.Changed.Select(change => new SetChangedFieldApiResponse(
            change.Address.DataContainer,
            change.Address.RowId,
            change.Address.FieldName,
            change.Value,
            change.Source.ToString().ToLowerInvariant())).ToArray(),
        response.Errors.Select(error => new SetFieldEvaluationErrorApiResponse(
            new SetFieldAddressApiResponse(
                error.Address.DataContainer,
                error.Address.RowId,
                error.Address.FieldName),
            error.Code,
            error.Message)).ToArray(),
        correlationId);
}
