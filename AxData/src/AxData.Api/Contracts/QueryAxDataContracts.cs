using System.Text.Json;
using System.Text.Json.Serialization;
using AxData.Application.Query;

namespace AxData.Api.Contracts;

public sealed record QueryAxDataRequest(JsonElement Select)
{
    public bool TryToCommand(string objectId, out QueryAxDataCommand command)
    {
        command = new QueryAxDataCommand(objectId, false, []);
        if (Select.ValueKind == JsonValueKind.String && Select.GetString() == "*")
        {
            command = new QueryAxDataCommand(objectId, true, []);
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

        command = new QueryAxDataCommand(objectId, false, selections);
        return true;
    }
}

public sealed record QueryAxDataApiResponse(
    string Status,
    string ObjectId,
    long Version,
    string TransId,
    long? RecordId,
    bool ReadOnly,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? PendingApproval,
    IReadOnlyList<string> AllowedOperations,
    IReadOnlyList<QueryDataContainerResponse> DataContainers,
    string CorrelationId)
{
    public static QueryAxDataApiResponse From(QueryAxDataResponse response, string correlationId) => new(
        "success",
        response.ObjectId,
        response.Version,
        response.TransId,
        response.RecordId,
        response.ReadOnly,
        response.PendingApproval ? true : null,
        response.AllowedOperations,
        response.DataContainers,
        correlationId);
}
