using System.Data;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxTstructData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AxData.ContractTests;

public sealed class AxPutEndpointErrorContractTests
{
    [Theory]
    [InlineData(404, AxPutObjectErrors.InvalidRequest, false)]
    [InlineData(409, AxPutObjectErrors.StaleVersion, true)]
    [InlineData(409, AxPutObjectErrors.StructDefChanged, true)]
    [InlineData(403, AxPutObjectErrors.AccessDenied, false)]
    public async Task Object_failures_preserve_http_status_code_objectid_and_known_version(
        int httpStatus,
        string errorCode,
        bool includeVersion)
    {
        APIResult serviceResult = new(false, "failure");
        serviceResult.data["errorcode"] = errorCode;
        serviceResult.data["httpstatus"] = httpStatus;
        serviceResult.data["objectid"] = "object-a";
        if (includeVersion)
        {
            serviceResult.data["version"] = 3L;
            serviceResult.data["dataObjectRetained"] = true;
        }

        AxPutController controller = new(new StubAxPutService(serviceResult), new NullTraceService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        ObjectResult response = Assert.IsType<ObjectResult>(await controller.AxPut(new AxPutInput
        {
            ARMSessionId = "session-a",
            ObjectId = "object-a"
        }));
        ARMResult envelope = Assert.IsType<ARMResult>(response.Value);

        Assert.Equal(httpStatus, response.StatusCode);
        Assert.Equal(errorCode, envelope.result["errorcode"]);
        Assert.Equal("object-a", envelope.result["objectid"]);
        Assert.Equal(includeVersion, envelope.result.ContainsKey("version"));
        Assert.Equal(includeVersion, envelope.result.ContainsKey("dataObjectRetained"));
        if (includeVersion)
            Assert.Equal(true, envelope.result["dataObjectRetained"]);
    }

    private sealed class StubAxPutService(APIResult result) : IAxPutService
    {
        public Task<APIResult> AxPut(AxPutInput putData) => Task.FromResult(result);
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
