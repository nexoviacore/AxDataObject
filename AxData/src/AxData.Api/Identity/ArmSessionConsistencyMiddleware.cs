using System.Security.Claims;
using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Domain.Errors;

namespace AxData.Api.Identity;

public sealed class ArmSessionConsistencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            IsJsonRequest(context.Request) &&
            await ReadBodySessionIdAsync(context.Request, context.RequestAborted).ConfigureAwait(false) is { } bodySessionId)
        {
            var tokenSessionId = context.User.FindFirstValue(HttpUserContextClaims.ArmSessionId);
            if (string.IsNullOrWhiteSpace(tokenSessionId) ||
                !string.Equals(tokenSessionId, bodySessionId, StringComparison.Ordinal))
            {
                context.Response.StatusCode = AxDataErrors.AuthenticationRequired.HttpStatusCode;
                await context.Response.WriteAsJsonAsync(
                    AxDataErrorResponse.Create(
                        AxDataErrors.AuthenticationRequired.Code,
                        AxDataErrors.AuthenticationRequired.Message,
                        context.TraceIdentifier),
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsJsonRequest(HttpRequest request) =>
        request.ContentLength is > 0 &&
        request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<string?> ReadBodySessionIdAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        try
        {
            request.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(
                request.Body,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "ARMSessionId", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}
