using System.Data;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxTstructData;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxData.ContractTests;

public sealed class AxPutEndpointErrorContractTests
{
    [Theory]
    [InlineData(404, AxPutObjectErrors.InvalidRequest)]
    [InlineData(409, AxPutObjectErrors.ActiveConflict)]
    [InlineData(403, AxPutObjectErrors.AccessDenied)]
    public async Task Object_failures_preserve_http_status_code_and_objectid(
        int httpStatus,
        string errorCode)
    {
        APIResult serviceResult = new(false, "failure");
        serviceResult.data["errorcode"] = errorCode;
        serviceResult.data["httpstatus"] = httpStatus;
        serviceResult.data["objectid"] = "object-a";

        AxPutController controller = CreateController(new StubAxPutService(serviceResult), "session-a");

        ObjectResult response = Assert.IsType<ObjectResult>(await controller.AxPut(new AxPutInput
        {
            ARMSessionId = "session-a",
            ObjectId = "object-a"
        }));
        ARMResult envelope = Assert.IsType<ARMResult>(response.Value);

        Assert.Equal(httpStatus, response.StatusCode);
        Assert.Equal(errorCode, envelope.result["errorcode"]);
        Assert.Equal("object-a", envelope.result["objectid"]);
        Assert.DoesNotContain(envelope.result.Keys, key => string.Equals(key, "version", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(envelope.result.Keys, key => string.Equals(key, "currentVersion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Token_and_body_session_mismatch_fails_authentication_before_service_access()
    {
        var service = new StubAxPutService(new APIResult());
        AxPutController controller = CreateController(service, "token-session");

        UnauthorizedObjectResult response = Assert.IsType<UnauthorizedObjectResult>(await controller.AxPut(new AxPutInput
        {
            ARMSessionId = "body-session",
            ObjectId = "object-a"
        }));
        ARMResult envelope = Assert.IsType<ARMResult>(response.Value);

        Assert.Equal(AxPutObjectErrors.AuthenticationRequired, envelope.result["errorcode"]);
        Assert.Equal("object-a", envelope.result["objectid"]);
        Assert.Equal(0, service.CallCount);
    }

    private static AxPutController CreateController(StubAxPutService service, string sessionId)
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sid", sessionId)],
                authenticationType: "Bearer"))
        };
        return new AxPutController(service, new NullTraceService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class StubAxPutService(APIResult result) : IAxPutService
    {
        public int CallCount { get; private set; }
        public Task<APIResult> AxPut(AxPutInput putData)
        {
            CallCount++;
            return Task.FromResult(result);
        }
        public Task<APIResult> CheckTstructCreateAccess(AxPut putData) => throw new NotSupportedException();
        public Task<APIResult> CheckTstructEditAccess(AxPut putData) => throw new NotSupportedException();
    }

    private sealed class NullTraceService : IARMLogTraceService
    {
        public void EnableTracing(bool enabled, bool keepOldLogs = false) { }
        public bool IsTracingEnabled() => false;
        public void LogDbConnection(string connectionString) { }
        public void LogSqlExecution(string sql, string[]? paramNames = null, DbType[]? paramTypes = null, object[]? paramValues = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null) { }
        public void LogResultInfo(int rowCount, DateTime startTime, DateTime endTime) { }
        public void LogScalarResultInfo(string result, DateTime startTime, DateTime endTime) { }
        public void LogError(string errorMessage) { }
        public void LogMessage(string message) { }
        public void LogMessage(string title, string message) { }
        public void LogDataTable(DataTable dt, DateTime? startTime = null, DateTime? endTime = null) { }
        public string GetTraceLog() => string.Empty;
        public Task<string> GetTraceFile(string fileName) => Task.FromResult(string.Empty);
        public void Clear() { }
    }
}
