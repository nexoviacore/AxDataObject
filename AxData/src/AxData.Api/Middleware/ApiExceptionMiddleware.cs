using AxData.Api.Contracts;
using AxData.Api.Errors;
using AxData.Application.Ports;

namespace AxData.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    private const string UnexpectedCode = "AXD-INFRA-002";
    private const string UnexpectedMessage = "An unexpected error occurred.";

    public async Task InvokeAsync(
        HttpContext context,
        ILogger<ApiExceptionMiddleware> logger,
        IOperationalEventSink? operationalLog = null)
    {
        try
        {
            await next(context);
        }
        catch (AxDataApiException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = exception.Error.HttpStatusCode;
            await context.Response.WriteAsJsonAsync(
                AxDataErrorResponse.Create(
                    exception.Error.Code,
                    exception.Error.Message,
                    context.TraceIdentifier,
                    exception.CurrentVersion),
                context.RequestAborted);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            logger.LogError(
                "Unhandled API failure type {ExceptionType} for correlation {CorrelationId}.",
                exception.GetType().Name,
                context.TraceIdentifier);
            operationalLog?.Write(new OperationalEvent(
                "api.exception.unhandled",
                "api",
                "failed",
                OperationalLogLevel.Error,
                CorrelationId: context.TraceIdentifier,
                ObjectId: context.Request.RouteValues["objectId"]?.ToString(),
                ErrorCode: UnexpectedCode,
                ReasonCode: exception.GetType().Name));
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                AxDataErrorResponse.Create(
                    UnexpectedCode,
                    UnexpectedMessage,
                    context.TraceIdentifier),
                context.RequestAborted);
        }
    }
}
