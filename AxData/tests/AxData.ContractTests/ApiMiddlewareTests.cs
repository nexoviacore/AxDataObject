using System.Security.Claims;
using System.Text.Json;
using AxData.Api.Contracts;
using AxData.Api.Errors;
using AxData.Api.Identity;
using AxData.Api.Middleware;
using AxData.Api.Requests;
using AxData.Api.Routing;
using AxData.Application.Ports;
using AxData.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using AxData.TestSupport.Fakes;

namespace AxData.ContractTests;

public sealed class ApiMiddlewareTests
{
    [Fact]
    public async Task Correlation_id_is_preserved_and_returned()
    {
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "client-correlation-1";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

        Assert.Equal("client-correlation-1", context.TraceIdentifier);
        Assert.Equal("client-correlation-1", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task Invalid_correlation_id_is_replaced()
    {
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "contains whitespace";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

        Assert.NotEqual("contains whitespace", context.TraceIdentifier);
        Assert.Equal(32, context.TraceIdentifier.Length);
    }

    [Fact]
    public async Task Unhandled_exception_returns_safe_error_and_correlation_id()
    {
        var context = CreateContext();
        context.TraceIdentifier = "corr-safe";
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("password=secret; SELECT * FROM private_table"));

        await middleware.InvokeAsync(context, NullLogger<ApiExceptionMiddleware>.Instance);
        var response = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("AXD-INFRA-002", response.Code);
        Assert.Equal("corr-safe", response.CorrelationId);
        Assert.DoesNotContain("secret", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Known_stale_version_error_returns_409_and_current_version()
    {
        var context = CreateContext();
        context.TraceIdentifier = "corr-conflict";
        var middleware = new ApiExceptionMiddleware(
            _ => throw new AxDataApiException(AxDataErrors.StaleVersion, 14));

        await middleware.InvokeAsync(context, NullLogger<ApiExceptionMiddleware>.Instance);
        var response = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("AXD-CONC-001", response.Code);
        Assert.Equal(14, response.CurrentVersion);
    }

    [Fact]
    public async Task Request_completion_records_status_and_correlation_without_body_data()
    {
        var context = CreateContext();
        context.TraceIdentifier = "corr-complete";
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        var sink = new RecordingOperationalEventSink();
        var middleware = new RequestCompletionLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, sink);

        var operationalEvent = Assert.Single(sink.Events);
        Assert.Equal("api.request.completed", operationalEvent.Name);
        Assert.Equal("corr-complete", operationalEvent.CorrelationId);
        Assert.Equal(StatusCodes.Status409Conflict, operationalEvent.HttpStatusCode);
        Assert.Equal(OperationalLogLevel.Warning, operationalEvent.Level);
    }

    [Fact]
    public void Logging_scope_contains_only_allow_listed_metadata()
    {
        var context = CreateContext();
        context.TraceIdentifier = "corr-1";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(HttpUserContextClaims.TenantId, "tenant-a"),
            new Claim(ClaimTypes.NameIdentifier, "user-a"),
            new Claim("token", "must-not-log")
        ], "test"));
        context.Request.RouteValues["objectId"] = "object-a";
        context.Request.RouteValues["transId"] = "sales";
        context.Request.Headers[RequestMetadataParser.IdempotencyKeyHeader] = "request-a";
        context.Request.Headers.Authorization = "Bearer must-not-log";

        var scope = SafeLoggingScope.Create(context);

        Assert.Equal(
            ["CorrelationId", "IdempotencyKey", "ObjectId", "TenantId", "TransId", "UserName"],
            scope.Keys.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(scope.Values, value => value.ToString()!.Contains("must-not-log", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_base_path_is_versioned()
    {
        Assert.Equal("/api/v1", ApiRoutes.BasePath);
    }

    [Fact]
    public void Every_domain_error_has_an_http_status_mapping()
    {
        Assert.All(AxDataErrors.All, error => Assert.InRange(error.HttpStatusCode, 400, 599));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<AxDataErrorResponse> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<AxDataErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }
}
