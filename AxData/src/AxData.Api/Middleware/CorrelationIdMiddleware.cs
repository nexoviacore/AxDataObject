using Microsoft.Extensions.Primitives;

namespace AxData.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[HeaderName]);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(SafeLoggingScope.Create(context)))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(StringValues values)
    {
        if (values.Count == 1)
        {
            var candidate = values[0];
            if (!string.IsNullOrWhiteSpace(candidate) &&
                candidate.Length <= MaximumLength &&
                candidate.All(character => character is >= '!' and <= '~'))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
