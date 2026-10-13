using ARMCommon.Interface;
using System.Text;
using System.Data;
using ARMCommon.Helpers;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ARMCommon.Helpers
{
    public class ARMLogTraceService : IARMLogTraceService
    {
        private StringBuilder _logBuilder = new StringBuilder();
        private bool _isTracingEnabled = false;
        private readonly IHttpContextAccessor _http;
        public ARMLogTraceService( IHttpContextAccessor http)
        {
            _http = http;
        }



        public void EnableTracing(bool enabled, bool keepOldLogs = false)
        {
            _isTracingEnabled = enabled;
            if (!keepOldLogs)
            {
                Clear(); // Clear previous logs when enabling tracing
            }
        }

        public bool IsTracingEnabled()
        {
            return _isTracingEnabled;
        }

        public void LogDbConnection(string connectionString)
        {
            if (!_isTracingEnabled) return;


            // Mask password in connection string
            //string maskedConnectionString = MaskConnectionStringPassword(connectionString);
            //_logBuilder.AppendLine($"[Connection] {maskedConnectionString}");
        }

        public void LogSqlExecution(string sql, string[] paramNames = null, DbType[] paramTypes = null, object[] paramValues = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null)
        {
            if (!_isTracingEnabled) return;

            _logBuilder.AppendLine($"<b>[SQL]</b>");
            _logBuilder.AppendLine($"{sql}");
            if (paramNames != null && paramTypes != null && paramValues != null)
            {
                _logBuilder.AppendLine("<b>[Parameters]</b>");
                for (int i = 0; i < paramNames.Length; i++)
                {
                    _logBuilder.AppendLine($"  <b>{paramNames[i]}</b> ({paramTypes[i]}/{paramValues[i].GetType()}) : <b>{paramValues[i]}</b>");
                }
            }

            if (viewFilters != null)
            {
                _logBuilder.AppendLine("<b>[View Filters]</b>");
                _logBuilder.AppendLine($"  {JsonConvert.SerializeObject(viewFilters)}");
            }

            if (globalParams != null)
            {
                _logBuilder.AppendLine("<b>[Global Params]</b>");
                _logBuilder.AppendLine($"  {JsonConvert.SerializeObject(globalParams)}");
            }

            if (sqlCommentsReplace != null)
            {
                _logBuilder.AppendLine("<b>[SQL Comments to be replaced]</b>");
                _logBuilder.AppendLine($"  {JsonConvert.SerializeObject(sqlCommentsReplace)}");
            }
        }

        public void LogResultInfo(int rowCount, DateTime startTime, DateTime endTime)
        {
            if (!_isTracingEnabled) return;

            TimeSpan duration = endTime - startTime;
            _logBuilder.AppendLine($"<b>[Result]</b>");
            _logBuilder.AppendLine($"Rows count: {rowCount}, Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
        }

        public void LogScalarResultInfo(string result, DateTime startTime, DateTime endTime)
        {
            if (!_isTracingEnabled) return;

            TimeSpan duration = endTime - startTime;
            _logBuilder.AppendLine($"<b>[Result]</b>");
            _logBuilder.AppendLine($"Result: {result}, Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
        }


        public void LogError(string errorMessage)
        {
            if (!_isTracingEnabled) return;

            _logBuilder.AppendLine($"<b>[Error]</b>");
            _logBuilder.AppendLine($"{errorMessage}");
        }

        public void LogMessage(string message)
        {
            if (!_isTracingEnabled) return;

            _logBuilder.AppendLine(message);
        }

        public void LogMessage(string title, string message)
        {
            if (!_isTracingEnabled) return;

            _logBuilder.AppendLine($"<b>{title}</b>");
            _logBuilder.AppendLine($"{message}");
        }

        public string GetTraceLog()
        {
            return _logBuilder.ToString();
        }

        public void Clear()
        {
            _logBuilder.Clear();
        }

        private string MaskConnectionStringPassword(string connectionString)
        {
            // Handle common connection string formats
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|PWD)=([^;]*)",
                "$1=********",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        public void LogDataTable(DataTable dt, DateTime? startTime = null, DateTime? endTime = null)
        {
            if (!_isTracingEnabled) return;

            if (endTime != null && startTime != null)
            {
                TimeSpan duration = (DateTime)endTime - (DateTime)startTime;
                _logBuilder.AppendLine($"<b>[Result]</b>");
                _logBuilder.AppendLine($"Rows count: {dt.Rows.Count}, Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
            }
            else {
                _logBuilder.AppendLine($"<b>[Result]</b>");
                _logBuilder.AppendLine($"Rows count: {dt.Rows.Count}");
            }

            StringBuilder sb = new StringBuilder();
            if (dt == null || dt.Rows.Count == 0)
            {
                sb.AppendLine($"Rows count: 0");
                sb.AppendLine("<p style='color:red;'><b>No data available</b></p>");
            }
            else
            {
                sb.Append("<table border='1' style='border-collapse: collapse; width: 100%;'>");

                // ✅ Create Header Row
                sb.Append("<thead><tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.AppendFormat("<th style='padding: 8px; background-color: #f2f2f2;'>{0}</th>", column.ColumnName);
                }
                sb.Append("</tr></thead>");

                // ✅ Create Data Rows
                sb.Append("<tbody>");
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var dateFormat = culture.DateTimeFormat.ShortDatePattern;
                var timeFormat = culture.DateTimeFormat.ShortTimePattern;

                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (var cell in row.ItemArray)
                    {
                        string cellText;

                        if (cell is DateTime dtValue)
                        {
                            cellText = dtValue.ToString($"{dateFormat} {timeFormat}");
                        }
                        else
                        {
                            cellText = cell?.ToString() ?? string.Empty;
                        }

                        sb.AppendFormat("<td style='padding: 8px; text-align: left;'>{0}</td>", cellText);
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</tbody>");
                sb.Append("</table>");
            }

            _logBuilder.Append(sb);
        }

        public async Task<string> GetTraceFile(string fileName)
        {
            try
            {
                var result = "";
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var logFolder = Path.Combine(exeDir, "Logs");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);                

                string filePath = logFolder;
                fileName = $"{fileName}_{DateTime.Now.ToString("ddMMMyyyy_HHmmssfff")}_{Guid.NewGuid().ToString().Replace("-", "")}.html";
                string content = GetTraceLog();
                filePath = Path.Combine(filePath, fileName);

                // ✅ Write content to file asynchronously
                await File.WriteAllTextAsync(filePath, content);

                var request = _http.HttpContext?.Request;
                if (request == null) return "Not able to load trace file/url.";

                var traceUrl = "";
                if (request.PathBase.ToString().ToUpper() == "ARM_APIS")
                {
                    traceUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/v1/GetTraceFile";
                }
                else
                {
                    traceUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/v1/GetTrace";
                }

                return $"{traceUrl.TrimEnd('/')}/{fileName}";
            }
            catch (Exception ex) {
                return $"Error in trace file generation. {ex.Message}";
            }
        }
    }
}