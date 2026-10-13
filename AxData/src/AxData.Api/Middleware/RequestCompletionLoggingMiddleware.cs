using System.Diagnostics;
using AxData.Application.Ports;

namespace AxData.Api.Middleware;

public sealed class RequestCompletionLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOperationalEventSink operationalLog)
    {
        var started = Stopwatch.GetTimestamp();
        await next(context).ConfigureAwait(false);
        var statusCode = context.Response.StatusCode;
        operationalLog.Write(new OperationalEvent(
            "api.request.completed",
            "api",
            statusCode < 400 ? "succeeded" : "failed",
            statusCode >= 500 ? OperationalLogLevel.Error : statusCode >= 400 ? OperationalLogLevel.Warning : OperationalLogLevel.Information,
            CorrelationId: context.TraceIdentifier,
            ObjectId: context.Request.RouteValues["objectId"]?.ToString(),
            HttpStatusCode: statusCode,
            DurationMs: Stopwatch.GetElapsedTime(started).TotalMilliseconds));
    }
}
