using ARMCommon.Model;
using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace ARMCommon.Interface
{
    public interface IARMLogTraceService
    {
        void EnableTracing(bool enabled, bool keepOldLogs = false);
        bool IsTracingEnabled();
        void LogDbConnection(string connectionString);
        void LogSqlExecution(string sql, string[] paramNames = null, DbType[] paramTypes = null, object[] paramValues = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null);
        void LogResultInfo(int rowCount, DateTime startTime, DateTime endTime);
        void LogScalarResultInfo(string result, DateTime startTime, DateTime endTime);
        void LogError(string errorMessage);
        void LogMessage(string message);
        void LogMessage(string title, string message);
        void LogDataTable(DataTable dt, DateTime? startTime = null, DateTime? endTime = null);
        string GetTraceLog();
        Task<string> GetTraceFile(string fileName);
        void Clear();
    }
}
