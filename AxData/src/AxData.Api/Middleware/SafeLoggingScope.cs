using System.Security.Claims;
using AxData.Api.Identity;
using AxData.Api.Requests;

namespace AxData.Api.Middleware;

public static class SafeLoggingScope
{
    public static IReadOnlyDictionary<string, object> Create(HttpContext context)
    {
        var values = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["CorrelationId"] = context.TraceIdentifier
        };

        Add(values, "TenantId", context.User.FindFirstValue(HttpUserContextClaims.TenantId));
        Add(values, "UserName", GetUserName(context.User));
        Add(values, "ObjectId", context.Request.RouteValues["objectId"]?.ToString());
        Add(values, "TransId", context.Request.RouteValues["transId"]?.ToString());
        Add(values, "IdempotencyKey", context.Request.Headers[RequestMetadataParser.IdempotencyKeyHeader].FirstOrDefault());

        return values;
    }

    private static string? GetUserName(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
        principal.FindFirstValue("sub") ??
        principal.FindFirstValue(ClaimTypes.Name);

    private static void Add(IDictionary<string, object> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }
}
