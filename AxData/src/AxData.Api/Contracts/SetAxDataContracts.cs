using System.Text.Json;
using AxData.Application.Ports.Axpert;
using AxData.Application.Set;

namespace AxData.Api.Contracts;

/// <summary>Addresses an active DataObject and submits field changes for server-side evaluation.</summary>
public sealed record SetAxDataRequest(
    string ObjectId,
    bool GetDeps,
    IReadOnlyDictionary<string, SetDataContainerRequest> Data)
{
    /// <summary>Validates the body address and maps submitted changes to an application command.</summary>
    public bool TryToCommand(out SetAxDataCommand command)
        => TryToCommand(correlationId: null, out command);

    /// <summary>Validates and maps the request while attaching safe request correlation metadata.</summary>
    public bool TryToCommand(
        string? correlationId,
        out SetAxDataCommand command)
    {
        command = new SetAxDataCommand(ObjectId, GetDeps, [], correlationId);
        if (string.IsNullOrWhiteSpace(ObjectId) || Data is null || Data.Count == 0)
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
        command = new SetAxDataCommand(ObjectId, GetDeps, containers, correlationId);
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

/// <summary>Returns accepted Set changes and field-level evaluation failures.</summary>
public sealed record SetAxDataApiResponse(
    string Status,
    string ObjectId,
    IReadOnlyList<SetCreatedRow> CreatedRows,
    IReadOnlyList<SetChangedFieldApiResponse> Changed,
    IReadOnlyList<SetFieldEvaluationErrorApiResponse> Errors,
    string CorrelationId)
{
    /// <summary>Maps the completed Set result and attaches the request correlation identifier.</summary>
    public static SetAxDataApiResponse From(SetAxDataResponse response, string correlationId) => new(
        response.Errors.Count == 0 ? "success" : "validation_error",
        response.ObjectId,
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
