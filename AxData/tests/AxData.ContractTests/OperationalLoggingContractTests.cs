using AxData.Api.Observability;
using AxData.Api.Middleware;
using AxData.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AxData.ContractTests;

public sealed class OperationalLoggingContractTests
{
    [Fact]
    public void Diagnostic_logging_emits_only_allow_listed_operational_metadata()
    {
        const string token = "token-must-not-log";
        const string session = "session-must-not-log";
        const string connection = "Host=secret-host;Password=must-not-log";
        const string sqlParameter = "sql-parameter-must-not-log";
        const string fieldValue = "field-value-must-not-log";
        var context = new DefaultHttpContext { TraceIdentifier = "corr-safe" };
        context.Request.Headers.Authorization = $"Bearer {token}";
        context.Request.Headers["X-Session-Id"] = session;
        context.Request.Headers["X-Test-Connection"] = connection;
        context.Request.Headers["X-Test-Sql-Parameter"] = sqlParameter;
        context.Request.Headers["X-Test-Field-Value"] = fieldValue;
        var accessor = new HttpContextAccessor { HttpContext = context };
        var logger = new RecordingLogger<MicrosoftOperationalEventSink>();
        var sink = new MicrosoftOperationalEventSink(
            logger,
            accessor,
            new OperationalLoggingOptions(OperationalLogDetail.Diagnostic));

        sink.Write(new OperationalEvent(
            "field.evaluation.completed",
            "set",
            "validation-errors",
            OperationalLogLevel.Warning,
            OperationalLogDetail.Diagnostic,
            TenantId: "tenant-a",
            TransId: "sales",
            ObjectId: "object-a",
            Version: 2,
            ErrorCode: "AXD-VAL-001",
            Count: 1,
            DurationMs: 12.5));

        var rendered = Assert.Single(logger.Messages);
        Assert.Contains("corr-safe", rendered, StringComparison.Ordinal);
        Assert.Contains("AXD-VAL-001", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(token, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(session, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(connection, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(sqlParameter, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(fieldValue, rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_logging_suppresses_diagnostic_events()
    {
        var logger = new RecordingLogger<MicrosoftOperationalEventSink>();
        var sink = new MicrosoftOperationalEventSink(
            logger,
            new HttpContextAccessor(),
            new OperationalLoggingOptions(OperationalLogDetail.Baseline));

        sink.Write(new OperationalEvent(
            "redis.load.completed",
            "query",
            "hit",
            Detail: OperationalLogDetail.Diagnostic));

        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task Unhandled_exception_logs_type_and_error_code_but_not_exception_message()
    {
        const string sensitiveMessage = "Password=must-not-log; field-value-must-not-log";
        var context = new DefaultHttpContext { TraceIdentifier = "corr-exception" };
        context.Response.Body = new MemoryStream();
        var logger = new RecordingLogger<ApiExceptionMiddleware>();
        var operationalLog = new AxData.TestSupport.Fakes.RecordingOperationalEventSink();
        var middleware = new ApiExceptionMiddleware(_ => throw new InvalidOperationException(sensitiveMessage));

        await middleware.InvokeAsync(context, logger, operationalLog);

        Assert.DoesNotContain(sensitiveMessage, Assert.Single(logger.Messages), StringComparison.Ordinal);
        var operationalEvent = Assert.Single(operationalLog.Events);
        Assert.Equal("api.exception.unhandled", operationalEvent.Name);
        Assert.Equal("InvalidOperationException", operationalEvent.ReasonCode);
        Assert.Equal("AXD-INFRA-002", operationalEvent.ErrorCode);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
