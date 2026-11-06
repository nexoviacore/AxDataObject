using System.Globalization;
using System.Text.Json.Serialization;
using AxData.Contracts.ObjectIds;

namespace AxData.Contracts.AxPut;

public class AxPutObjectRequestContract
{
    public virtual string? ObjectId { get; set; }

    public virtual bool? RetainDataObject { get; set; } = true;

    public virtual long? ExpectedVersion { get; set; }
}

public sealed record AxPutObjectResponseContract(
    [property: JsonPropertyName("objectid")] string ObjectId,
    [property: JsonPropertyName("version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Version,
    [property: JsonPropertyName("dataObjectRetained"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? DataObjectRetained,
    [property: JsonPropertyName("dataObjectFinalizationPending"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? DataObjectFinalizationPending = null);

public sealed record AxPutObjectResolution(
    string ObjectId,
    bool ObjectIdWasSupplied,
    bool RetainDataObject,
    long? ExpectedVersion);

public static class AxPutObjectContractRules
{
    public static AxPutObjectResolution Resolve(
        AxPutObjectRequestContract request,
        string? ifMatch,
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

            var expectedVersion = ResolveExpectedVersion(ifMatch, request.ExpectedVersion);
            return new AxPutObjectResolution(
                request.ObjectId!.Trim(),
                true,
                request.RetainDataObject ?? true,
                expectedVersion);
        }

        if (!string.IsNullOrWhiteSpace(ifMatch) || request.ExpectedVersion is not null)
        {
            throw new ArgumentException(
                "If-Match/expectedVersion cannot be supplied when objectid is omitted.",
                nameof(ifMatch));
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
            request.RetainDataObject ?? true,
            null);
    }

    private static long ResolveExpectedVersion(string? ifMatch, long? bodyVersion)
    {
        long? headerVersion = null;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            var value = ifMatch.Trim();
            if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
            {
                throw new ArgumentException(
                    "If-Match must contain one positive quoted object version.",
                    nameof(ifMatch));
            }

            value = value[1..^1];

            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                || parsed < 1)
            {
                throw new ArgumentException(
                    "If-Match must contain one positive quoted object version.",
                    nameof(ifMatch));
            }

            headerVersion = parsed;
        }

        if (bodyVersion is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bodyVersion), "expectedVersion must be positive.");
        }

        if (headerVersion is not null && bodyVersion is not null && headerVersion != bodyVersion)
        {
            throw new ArgumentException(
                "If-Match and expectedVersion must match when both are supplied.",
                nameof(bodyVersion));
        }

        return headerVersion ?? bodyVersion
            ?? throw new ArgumentException(
                "If-Match or expectedVersion is required for an existing objectid.",
                nameof(bodyVersion));
    }
}
