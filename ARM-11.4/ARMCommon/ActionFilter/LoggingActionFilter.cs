
using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Npgsql;
using System.Text;
using System.Text.Json;
using ARMCommon.Interface;
using static ARMCommon.Helpers.Constants;
using Oracle.ManagedDataAccess.Client;


namespace ARMCommon.ActionFilter
{
    public class LoggingActionFilter  : IAsyncResultFilter
    {
        private readonly IRabbitMQProducer _iMessageProducer;
        private readonly IConfiguration _config;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;


        public LoggingActionFilter(IRabbitMQProducer iMessageProducer, IRedisHelper redis, IConfiguration config )
        {
            _redis = redis;
            _config = config;
            _iMessageProducer = iMessageProducer;
            _config = config;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            try
            {
                 bool IsLoggingEnabled = _config.GetValue<bool>("Islogging");
                //bool IsLoggingEnabled = true;
                if (IsLoggingEnabled)
                {
                    context.HttpContext.Request.EnableBuffering();
                    context.HttpContext.Request.Body.Position = 0;
                    var originalstream = context.HttpContext.Response.Body;
                    MemoryStream requestBody = new MemoryStream();
                    await context.HttpContext.Request.Body.CopyToAsync(requestBody);
                    requestBody.Seek(0, SeekOrigin.Begin);
                    string requesttext = await new StreamReader(requestBody).ReadToEndAsync();
                    requestBody.Seek(0, SeekOrigin.Begin);

                    var tempstreame = new MemoryStream();
                    context.HttpContext.Response.Body = tempstreame;
                    await next.Invoke();
                    context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
                    string responsetext = await new StreamReader(context.HttpContext.Response.Body).ReadToEndAsync();
                    context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
                    await context.HttpContext.Response.Body.CopyToAsync(originalstream);
                    var messageObjToLog = new
                    {
                        path = context.HttpContext.Request.Path,
                        Querystringvalue = context.HttpContext.Request.QueryString.Value,
                        requestBody = requesttext,
                        ResponseBody = responsetext
                    };
               
                    _iMessageProducer.SendMessages(JsonConvert.SerializeObject(messageObjToLog), "logQueue");

                }
                else {
                    var result = await next();
                }
                
            }
            catch (Exception ex)
            {
                LogException(context, ex);
            }
        }


        private async Task LogException(ResultExecutingContext context, Exception ex)
        {
            ExceptionModel exceptionModel = new ExceptionModel();
            context.HttpContext.Request.EnableBuffering();
            context.HttpContext.Request.Body.Position = 0;
            MemoryStream requestBody = new MemoryStream();
            await context.HttpContext.Request.Body.CopyToAsync(requestBody);
            requestBody.Seek(0, SeekOrigin.Begin);
            string requesttext = await new StreamReader(requestBody).ReadToEndAsync();
            requestBody.Seek(0, SeekOrigin.Begin);

            exceptionModel.HttpMethod = context.HttpContext.Request.Method;
            exceptionModel.Requestbody = requesttext;
            exceptionModel.QueryStringValue = context.HttpContext.Request.QueryString.Value;
            exceptionModel.ExceptionMessage = ex.Message;
            exceptionModel.ExceptionStackSTrace = ex.StackTrace;
            exceptionModel.InnerExceptionMessage = ex.InnerException?.Message;
            exceptionModel.InnerExceptionStackTrace = ex.InnerException?.StackTrace;
            _iMessageProducer.SendMessage(exceptionModel, "logQueue");
            // Log.Error("Exception in Application. " + JsonConvert.SerializeObject(exceptionModel));
        }

    }
}




public class ApiLoggingFilter : IAsyncActionFilter
{
    private readonly IConfiguration _config;
    private readonly IRedisHelper _redis;
    private readonly Utils _common;

    public ApiLoggingFilter(IRedisHelper redis, IConfiguration config, Utils common)
    {
        _redis = redis;
        _config = config;
        _common = common;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            bool IsLoggingEnabled = _config.GetValue<bool>("Islogging");
            if (IsLoggingEnabled)
            {
                var requestId = Guid.NewGuid().ToString();
                var requestTime = DateTime.UtcNow;

                // Read Request Body
                var requestBody = await ReadRequestBody(context.HttpContext.Request);
                var request = context.HttpContext.Request;

                // Extract ARMSessionId from request model
                List<string> nodes = new List<string> { "ARMSESSIONID", "APPNAME", "PROJECT", "USERNAME" };
                var properties = ExtractProperties(requestBody, nodes);
                string appName = "";
                string userName = "";
                string armSessionId = "";
                if (properties.ContainsKey("ARMSESSIONID"))
                {
                    armSessionId = properties["ARMSESSIONID"];
                    Dictionary<string, object> sessionVars = await _common.GetSessionData(properties["ARMSESSIONID"]);
                    appName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                    userName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

                }                
                else if (properties.ContainsKey("APPNAME") || properties.ContainsKey("PROJECT"))
                {
                    armSessionId = "";
                    appName = properties["APPNAME"] ?? properties["PROJECT"];
                    userName = properties["USERNAME"] ?? "";
                }


                if (!string.IsNullOrEmpty(appName)) {
                    // Execute API action
                    var executedContext = await next();

                    // Capture Response Data
                    var responseTime = DateTime.UtcNow;
                    var statusCode = executedContext.HttpContext.Response.StatusCode;
                    var responseBody = ((Microsoft.AspNetCore.Mvc.ObjectResult)executedContext.Result).Value != null ? ((Microsoft.AspNetCore.Mvc.ObjectResult)executedContext.Result).Value.ToString() : null;
                    var executionTime = (responseTime - requestTime).TotalMilliseconds.ToString("F2") + " ms";

                    // Log Request & Response together in a single DB call
                    await LogRequestAndResponse(appName, context.HttpContext, requestId, requestBody, armSessionId, userName, requestTime, responseTime, statusCode, responseBody, executionTime);
                }

            }
        }
        catch (Exception ex) 
        {
        
        }
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering(); // Allows multiple reads
        request.Body.Position = 0; // Ensure we're at the start

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0; // Reset stream position after reading
        return body;
    }

    private Dictionary<string, string> ExtractProperties(string requestBody, List<string> propertyNames)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(requestBody) || propertyNames == null || propertyNames.Count == 0)
            return result;

        try
        {
            using var jsonDoc = JsonDocument.Parse(requestBody);
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    result[property.Name.ToUpper()] = property.Value.GetString();
                }
            }
        }
        catch (Exception)
        {
            // Optionally handle JSON parsing errors
        }

        return result;
    }

    private async Task LogRequestAndResponse(string appName, HttpContext context, string requestId, string requestBody, string armSessionId, string userName, DateTime requestTime, DateTime responseTime, int statusCode, string responseBody, string executionTime)
    {
        
        Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
        string connectionString = config["ConnectionString"];
        string dbType = config["DBType"];

        if(dbType.ToLower() == "postgre")
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var query = @"
                INSERT INTO axrequest 
                (requestid, requestreceivedtime, sourcefrom, requeststring, authz, contenttype, contentlength, url, endpoint, requestmethod, apiname, username, additionaldetails) 
                VALUES 
                (@requestid, @requestreceivedtime, @sourcefrom, @requeststring, @authz, @contenttype, @contentlength, @url, @endpoint, @requestmethod, @apiname, @username, @additionaldetails);
                
                INSERT INTO axresponse 
                (responseid, responsesenttime, statuscode, responsestring, requestid, executiontime) 
                VALUES 
                (@responseid, @responsesenttime, @statuscode, @responsestring, @requestid, @executiontime);";

                using var command = new NpgsqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@requestid", requestId);
                command.Parameters.AddWithValue("@requestreceivedtime", requestTime);
                command.Parameters.AddWithValue("@sourcefrom", "ARM");
                command.Parameters.AddWithValue("@requeststring", requestBody ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@authz", (object)DBNull.Value);
                command.Parameters.AddWithValue("@contenttype", context.Request.ContentType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@contentlength", context.Request.ContentLength?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@url", context.Request.Path.ToString());
                command.Parameters.AddWithValue("@endpoint", context.Request.Path.ToString().Split('/').Last());
                command.Parameters.AddWithValue("@requestmethod", context.Request.Method);
                command.Parameters.AddWithValue("@apiname", context.Request.Path.ToString().Split('/').Last());
                command.Parameters.AddWithValue("@username", userName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@additionaldetails", armSessionId ?? (object)DBNull.Value);

                command.Parameters.AddWithValue("@responseid", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@responsesenttime", responseTime);
                command.Parameters.AddWithValue("@statuscode", statusCode);
                command.Parameters.AddWithValue("@responsestring", responseBody ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@executiontime", executionTime);

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else if (dbType.ToLower() == "oracle") {
            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var query = @"
                INSERT INTO axrequest 
                (requestid, requestreceivedtime, sourcefrom, requeststring, authz, contenttype, contentlength, url, endpoint, requestmethod, apiname, username, additionaldetails) 
                VALUES 
                (:requestid, :requestreceivedtime, :sourcefrom, :requeststring, :authz, :contenttype, :contentlength, :url, :endpoint, :requestmethod, :apiname, :username, :additionaldetails);

                INSERT INTO axresponse 
                (responseid, responsesenttime, statuscode, errordetails, requestid, executiontime) 
                VALUES 
                (:responseid, :responsesenttime, :statuscode, :errordetails, :requestid, :executiontime);";

                using var command = new OracleCommand(query, connection);
                command.Transaction = transaction;
                command.Parameters.Add(new OracleParameter(":requestid", requestId));
                command.Parameters.Add(new OracleParameter(":requestreceivedtime", requestTime));
                command.Parameters.Add(new OracleParameter(":sourcefrom", "ARM"));
                command.Parameters.Add(new OracleParameter(":requeststring", requestBody ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":authz", context.Request.Headers["Authorization"].ToString() ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":contenttype", context.Request.ContentType ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":contentlength", context.Request.ContentLength?.ToString() ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":url", context.Request.Path.ToString()));
                command.Parameters.Add(new OracleParameter(":endpoint", context.Request.Path.ToString().Split('/').Last()));
                command.Parameters.Add(new OracleParameter(":requestmethod", context.Request.Method));
                command.Parameters.Add(new OracleParameter(":apiname", context.Request.Path.ToString().Split('/').Last()));
                command.Parameters.Add(new OracleParameter(":username", context.User.Identity?.Name ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":additionaldetails", armSessionId ?? (object)DBNull.Value));

                command.Parameters.Add(new OracleParameter(":responseid", Guid.NewGuid().ToString()));
                command.Parameters.Add(new OracleParameter(":responsesenttime", responseTime));
                command.Parameters.Add(new OracleParameter(":statuscode", statusCode));
                command.Parameters.Add(new OracleParameter(":errordetails", responseBody ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter(":executiontime", executionTime));

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


    }
}
