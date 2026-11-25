using System.Text.Json;
using System.Text.Json.Serialization;
using AxData.Application.Query;

namespace AxData.Api.Contracts;

/// <summary>Addresses an active DataObject and optionally selects a subset of its fields.</summary>
/// <remarks>When <see cref="Select"/> is omitted or null, the complete authorized object is returned.</remarks>
public sealed record GetAxDataRequest(string ObjectId, JsonElement Select)
{
    /// <summary>Validates the body address and maps the requested projection to an application command.</summary>
    public bool TryToCommand(out QueryAxDataCommand command)
    {
        command = new QueryAxDataCommand(ObjectId, false, []);
        if (string.IsNullOrWhiteSpace(ObjectId))
        {
            return false;
        }

        if (Select.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            command = new QueryAxDataCommand(ObjectId, true, []);
            return true;
        }

        if (Select.ValueKind == JsonValueKind.String && Select.GetString() == "*")
        {
            command = new QueryAxDataCommand(ObjectId, true, []);
            return true;
        }

        if (Select.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var selections = new List<QuerySelection>();
        foreach (var item in Select.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("dcId", out var dcId) || dcId.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var fieldNames = new List<string>();
            foreach (var field in fields.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                fieldNames.Add(field.GetString()!);
            }

            selections.Add(new QuerySelection(dcId.GetString()!, fieldNames));
        }

        command = new QueryAxDataCommand(ObjectId, false, selections);
        return true;
    }
}

/// <summary>Returns the authorized complete or selective DataObject projection.</summary>
public sealed record GetAxDataApiResponse(
    string Status,
    string ObjectId,
    string TransId,
    long? RecordId,
    bool ReadOnly,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? PendingApproval,
    IReadOnlyList<string> AllowedOperations,
    IReadOnlyList<QueryDataContainerResponse> DataContainers,
    string CorrelationId)
{
    /// <summary>Maps the application projection and attaches the request correlation identifier.</summary>
    public static GetAxDataApiResponse From(QueryAxDataResponse response, string correlationId) => new(
        "success",
        response.ObjectId,
        response.TransId,
        response.RecordId,
        response.ReadOnly,
        response.PendingApproval ? true : null,
        response.AllowedOperations,
        response.DataContainers,
        correlationId);
}
