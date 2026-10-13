using System.Globalization;
using AxData.Api.Errors;
using AxData.Domain.Errors;
using Microsoft.Extensions.Primitives;

namespace AxData.Api.Requests;

public static class RequestMetadataParser
{
    public const string IfMatchHeader = "If-Match";
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const int MaximumIdempotencyKeyLength = 128;

    public static long GetExpectedVersion(HttpRequest request, long? bodyExpectedVersion = null)
    {
        var headerVersion = ParseIfMatch(request.Headers[IfMatchHeader]);

        if (headerVersion is not null && bodyExpectedVersion is not null && headerVersion != bodyExpectedVersion)
        {
            throw InvalidRequest();
        }

        var version = headerVersion ?? bodyExpectedVersion;
        if (version is null or < 1)
        {
            throw InvalidRequest();
        }

        return version.Value;
    }

    public static string GetIdempotencyKey(HttpRequest request)
    {
        StringValues values = request.Headers[IdempotencyKeyHeader];
        if (values.Count != 1)
        {
            throw InvalidRequest();
        }

        var value = values[0];
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaximumIdempotencyKeyLength ||
            value.Any(character => character is < '!' or > '~'))
        {
            throw InvalidRequest();
        }

        return value;
    }

    private static long? ParseIfMatch(StringValues values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count != 1)
        {
            throw InvalidRequest();
        }

        var value = values[0];
        if (value is null || value.Length < 3 || value[0] != '"' || value[^1] != '"')
        {
            throw InvalidRequest();
        }

        var text = value[1..^1];
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 1)
        {
            throw InvalidRequest();
        }

        return version;
    }

    private static AxDataApiException InvalidRequest() => new(AxDataErrors.InvalidRequest);
}
