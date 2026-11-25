namespace AxData.Api.Middleware;

/// <summary>Shares allow-listed request identifiers with operational middleware.</summary>
public static class RequestOperationalMetadata
{
    private static readonly object ObjectIdKey = new();

    /// <summary>Records a body-bound ObjectId without retaining or parsing the request body.</summary>
    public static void SetObjectId(HttpContext context, string? objectId)
    {
        if (!string.IsNullOrWhiteSpace(objectId))
        {
            context.Items[ObjectIdKey] = objectId;
        }
    }

    /// <summary>Returns the body-bound ObjectId, falling back to a route value when present.</summary>
    public static string? GetObjectId(HttpContext context) =>
        context.Items.TryGetValue(ObjectIdKey, out var value)
            ? value as string
            : context.Request.RouteValues["objectId"]?.ToString();
}
