using System.Text.Json.Serialization;
using AxData.Contracts.ObjectIds;

namespace AxData.Contracts.AxPut;

public class AxPutObjectRequestContract
{
    public virtual string? ObjectId { get; set; }

    public virtual bool? RetainDataObject { get; set; } = true;

}

public sealed record AxPutObjectResponseContract(
    [property: JsonPropertyName("objectid")] string ObjectId,
    [property: JsonPropertyName("dataObjectRetained"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? DataObjectRetained,
    [property: JsonPropertyName("dataObjectFinalizationPending"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? DataObjectFinalizationPending = null);

public sealed record AxPutObjectResolution(
    string ObjectId,
    bool ObjectIdWasSupplied,
    bool RetainDataObject);

public static class AxPutObjectContractRules
{
    public static AxPutObjectResolution Resolve(
        AxPutObjectRequestContract request,
        bool dataWasSupplied,
        int dataCount,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (dataCount < 0 || (!dataWasSupplied && dataCount != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(dataCount));
        }

        var objectIdWasSupplied = !string.IsNullOrWhiteSpace(request.ObjectId);
        if (objectIdWasSupplied)
        {
            if (dataWasSupplied && dataCount == 0)
            {
                throw new ArgumentException(
                    "Data must be omitted or contain at least one item for an existing objectid.",
                    nameof(dataCount));
            }

            return new AxPutObjectResolution(
                request.ObjectId!.Trim(),
                true,
                request.RetainDataObject ?? true);
        }

        if (!dataWasSupplied || dataCount == 0)
        {
            throw new ArgumentException(
                "Data must contain at least one item when objectid is omitted.",
                nameof(dataCount));
        }

        return new AxPutObjectResolution(
            CryptographicUlid.New(now),
            false,
            request.RetainDataObject ?? true);
    }
}
