using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using NPOI.SS.Formula.Functions;
using StackExchange.Redis;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Model
{
    public class ARMPEG : IARMPEG
    {
        private readonly IRedisHelper _redis;
        private readonly IPostgresHelper _postGres;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly ITokenService _tokenService;

        public ARMPEG(IRedisHelper redis, IPostgresHelper postGres, IConfiguration config, Utils common, ITokenService tokenService )
        {
            _redis = redis;
            _postGres = postGres;
            _config = config;
            _common = common;
            _tokenService = tokenService;
        }

        private async Task<string> GetDBConnString(string appName)
        {
            string connectionString = await _common.GetDBConfiguration(appName);
            return connectionString;

        }
        public async Task<object> GetActiveTasksList(string armSessionId, string appName)
        {
            var username = await _redis.HashGetAsync(armSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }
            return await GetActiveTasksForUser(username, appName);
        }

        public async Task<bool> IsValidAxpertConnection(ARMAxpertConnect axpert)
        {
            var axSession = await _redis.HashGetAsync(axpert.ARMSessionId, Constants.SESSION_DATA.AXPERT_SESSIONID.ToString());
            if (axSession == axpert.AxSessionId)
            {
                return true;
            }
            return false;
        }
        public async Task<DataTable> GetActiveTasksForUser(string userName, string appName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.AXACTIVETASKS.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<DataTable> GetBulkActiveTasks(ARMTask task)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(task.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_BULKACTIVETASKS.ToString();

            string[] paramName = { "@username", "@tasktype", "@processname" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { task.ToUser.ToLower(), task.TaskType.ToLower(), task.ProcessName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<string> BulkApprovalTask(ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);

            string apiInput = @"{
	            ""axapprove"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
                    ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
                    ""bulkapprove"": ""true"",
		            ""taskid"": """ + taskAction.TaskId + @""",
		            ""status"": ""approved"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
                ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
                ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
                ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxApprove";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<ARMTaskAction> GetTaskActionDetails(ARMTaskAction taskAction)
        {
            if (string.IsNullOrEmpty(taskAction.User))
            {
                taskAction.User = await _redis.HashGetAsync(taskAction.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }

            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = await _redis.HashGetAsync(taskAction.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            if (string.IsNullOrEmpty(taskAction.Password))
            {
                taskAction.Password = await GetUserPassword(taskAction.AppName, taskAction.User);
            }

            return taskAction;
        }

        private async Task<string> GetUserPassword(string appName, string userName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETUSERPASSWORD.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            return result.Rows[0][0]?.ToString();

        }

        public async Task<ARMTask> GetActiveTask(string sessionId, string taskId, string taskType, string appName)
        {
            var username = await _redis.HashGetAsync(sessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return null;
            }

            ARMTask task = new ARMTask();

            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"
                ];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETACTIVETASKS.ToString();
            string[] paramName = { "@taskid", "@tasktype", "@username" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { taskId, taskType.ToLower(), username.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var dtTask = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

            if (dtTask.Rows.Count > 0)
            {
                task = new ARMTask(dtTask.Rows[0]);
            }
            return task;
        }

        private string ParseAxTasksResult(ARMResult apiResult)
        {
            if (apiResult.result != null && Convert.ToBoolean(apiResult.result["success"]) == true)
            {
                var jObj = JObject.Parse(apiResult?.result["message"]?.ToString());
                if (jObj["result"]?[0]?["status"]?.ToString() == "success")
                {
                    return Constants.RESULTS.SUCCESS.ToString();
                }
                else if (jObj["result"]?[0]?["error"]?["status"]?.ToString().ToLower() == "failed")
                {
                    return jObj["result"]?[0]?["error"]?["msg"]?.ToString();
                }
                return jObj["result"]?[0]?["msg"]?.ToString();
            }
            return apiResult.result["message"]?.ToString();
        }

        public async Task<string> ApproveTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);

            string apiInput = @"{
	            ""axapprove"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""", 
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""approved"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
		            ""userdata"": {}
                    $AMENDMENT$
	            },
                """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
                ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";

            if (task.Amendment != null && task.RecordId != null && task.Amendment == "T")
                apiInput = apiInput.Replace("$AMENDMENT$", @",""isamendment"": ""true"",
		            ""recordid"": """ + task.RecordId + @"""");
            else
                apiInput = apiInput.Replace("$AMENDMENT$", "");

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxApprove";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> ApproveToTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axapprove"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""", 
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""approved"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
		            ""nextleveltask"":""" + taskAction.ToTask + @""",
		            ""userdata"": {}
                    $AMENDMENT$
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";

            if (task.Amendment != null && task.RecordId != null && task.Amendment == "T")
                apiInput = apiInput.Replace("$AMENDMENT$", @",""isamendment"": ""true"",
		            ""recordid"": """ + task.RecordId + @"""");
            else
                apiInput = apiInput.Replace("$AMENDMENT$", "");

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxApprove";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }


        public async Task<string> BulkApprovalTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);

            string[] taskIds = task.TaskId.Split(',');
            string apiInput = @"{
	            ""axapprove"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": ""false"",
                    ""multi"": ""true"",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + taskIds + @""",
		            ""status"": ""approved"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxApprove";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }
        public async Task<string> RejectTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);

            string apiInput = @"{
	            ""axreject"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""rejected"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
		            ""userdata"": {}
                    $AMENDMENT$
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";

            if (task.Amendment != null && task.RecordId != null && task.Amendment == "T")
                apiInput = apiInput.Replace("$AMENDMENT$", @",""isamendment"": ""true"",
		            ""recordid"": """ + task.RecordId + @"""");
            else
                apiInput = apiInput.Replace("$AMENDMENT$", "");

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxReject";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> ReturnTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axreturn"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""returned"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
                    ""returnto"":""" + taskAction.ReturnTo + @""",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxReturn";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> ReturnToTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);

            string apiInput = @"{
	            ""axreturn"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""returned"",
                    ""statusreason"":""" + taskAction.StatusReason + @""",
		            ""statustext"":""" + taskAction.StatusText + @""",
                    ""returnleveltask"":""" + taskAction.ToTask + @""",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxReturn";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }


        public async Task<string> ForwardTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axforward"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""forwarded"",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxForward";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> CheckTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axcheck"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
		            ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""checked"",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxCheck";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> SendTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axsend"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",		           
		            ""sendtouser"": """ + taskAction.SendTo + @""",
		            ""status"": ""checked"",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxSend";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<string> SkipTask(ARMTask task, ARMTaskAction taskAction)
        {
            var sessionData = await GetSessionDataForPEG(taskAction);
            string apiInput = @"{
	            ""axskip"": {
		            ""redisportno"": """",
		            ""redisserver"": """",
		            ""redispwd"": """",
		            ""axpapp"": """ + taskAction.AppName + @""",
		            ""username"": """ + taskAction.User + @""",
		            ""s"": """ + sessionData["AxSessionId"] + @""",
		            ""password"": """",
		            ""seed"": """",
		            ""trace"": """ + taskAction.Trace?.ToString() + @""",
		            ""transid"": """ + task.TransId + @""",
		            ""taskid"": """ + task.TaskId + @""",
                    ""taskname"": """ + task.TaskName + @""",
		            ""processname"": """ + task.ProcessName + @""",
		            ""keyfield"": """ + task.KeyField + @""",
		            ""keyvalue"": """ + task.KeyValue + @""",
		            ""status"": ""skipped"",
		            ""userdata"": {}
	            },
	            """ + taskAction.AppName + @""" : " + JsonConvert.SerializeObject(sessionData["AxApps"]) + @",
	            ""axprops"" : " + JsonConvert.SerializeObject(sessionData["AxProps"]) + @",
	            ""globalvars"":  " + JsonConvert.SerializeObject(sessionData["GlobalVars"]) + @",
	            ""uservars"":  " + JsonConvert.SerializeObject(sessionData["UserVars"]) + @"
            }";
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{taskAction.AppName}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(taskAction.AppName);
            string apiUrl = AxpertWebScriptsURL + "/ASBPegRest.dll/datasnap/rest/TASBPeg/AxSkip";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxTasksResult(apiResult);
        }

        public async Task<object> GetProcessDetails(ARMProcessFlowTask processFlow)
        {
            var processData = (DataTable)await GetProcessDefinition(processFlow);
            var processStatus = await GetProcessStatus(processFlow);

            var results = from pd in processData.AsEnumerable()
                          join ps in processStatus.AsEnumerable() on (string)pd["taskname"] equals (string)ps["taskname"]
                          into temp
                          from row in temp.DefaultIfEmpty()
                          select new
                          {
                              processname = pd["processname"],
                              indexno = pd["indexno"],
                              displayicon = pd["displayicon"],
                              tasktype = pd["tasktype"],
                              taskgroupname = pd["taskgroup"],
                              taskname = pd["taskname"],
                              transid = pd["transid"],
                              keyfield = pd["keyfield"],
                              taskdefid = pd["recordid"],
                              taskstatus = row == null ? null : row["taskstatus"],
                              taskid = row == null ? null : row["taskid"],
                              keyvalue = row == null ? null : row["keyvalue"],
                              recordid = row == null ? 0 : row["recordid"]
                          };
            return results;
        }

        private async Task<DataTable> GetProcessStatus(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMGETPROCESSDETAIL.ToString();
            string[] paramName = { "@processname", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.KeyValue.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.ARMGETPROCESSDETAIL_ORACLE.ToString();
                var dtSql = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                if (dtSql.Rows.Count > 0)
                {
                    sql = dtSql.Rows[0][0].ToString();
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
                else
                {
                    DataTable dt = new DataTable();
                    dt.Rows.Add("Error in oracle sql generation");
                    return dt;
                }
            }
            else
            {
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }

        }

        public async Task<object> GetProcessTask(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETPROCESSTASK.ToString();

            string[] paramName = { "@taskid" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { processFlow.TaskId.ToLower() };

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GETPROCESSTASK_ORACLE.ToString();
            }

            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var dtTasks = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

            if (dtTasks.Rows.Count > 0)
            {
                var currUserTaskRows = dtTasks.AsEnumerable()
                           .Where(r => r.Field<string>("touser").ToLower() == processFlow.ToUser.ToLower())
                           .GroupBy(r => r.Field<string>("touser")) // Adjust the field name if necessary
                           .Select(g => g.First());
                //dtTasks.AsEnumerable().Where(r => r.Field<string>("touser").ToLower() == processFlow.ToUser.ToLower());
                DataTable dtResult = new DataTable();
                if (currUserTaskRows.Any())
                    dtResult = currUserTaskRows.CopyToDataTable();

                if (dtResult?.Rows.Count == 1)
                {
                    return dtResult;
                }
                else
                {
                    var otherUserTaskRows = dtTasks.AsEnumerable().Where(r => r.Field<string>("taskstatus") != null && r.Field<string>("touser") == r.Field<string>("username"));
                    if (otherUserTaskRows.Any())
                    {
                        return otherUserTaskRows.CopyToDataTable();
                    }

                    var pendingUsersList = dtTasks.AsEnumerable().Select(r => r["touser"].ToString()).Distinct();
                    string pendingUsers = string.Join(",", pendingUsersList);
                    DataTable dtPending = dtTasks.Clone();
                    DataRow drPending = dtTasks.Rows[0];
                    drPending["touser"] = pendingUsers;
                    drPending["ispending"] = "T";
                    dtPending.Rows.Add(drPending.ItemArray);
                    return dtPending;
                }
            }
            return dtTasks;
        }

        public async Task<object> GetProcessKeyValues(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETPROCESSKEYVALUE.ToString();
            string[] paramName = { "@processname", "@touser" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.ToUser.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetKeyValue(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETKEYVALUE.ToString();
            string[] paramName = { "@processname", "@recordid" };
            DbType[] paramType = { DbType.String, DbType.Int64 };
            object[] paramValue = { processFlow.ProcessName.ToLower(), Convert.ToInt64(processFlow.RecordId) };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<object> GetAxProcess(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMGetAXPROCESS.ToString();
            string[] paramName = { "@processname" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetProcessDefinition(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMPROCESSDEFINITION.ToString();
            string[] paramName = { "@processname" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetProcessList(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMGETPROCESSLIST.ToString();
            string addnewnodesql = Constants_SQL.ARMGETADDNEWNODE.ToString();
            //string[] paramName = { "@processname", "@username" };
            //NpgsqlDbType[] paramType = { NpgsqlDbType.Varchar, NpgsqlDbType.Varchar };
            //object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.ToUser.ToLower() };

            string[] paramName = { "@processname", "@username" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.ToUser.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var listNode = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            var addNewNode = "";// await _postGres.ExecuteSql(addnewnodesql, connectionString, paramName, paramType, paramValue);
            var result = new
            {
                addnewnode = addNewNode,
                list = listNode

            };
            return result;
        }

        public async Task<object> GetProcessDetail(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMGETPROCESSDETAIL.ToString();
            string[] paramName = { "@processname", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.KeyValue.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.ARMGETPROCESSDETAIL_ORACLE.ToString();
                var dtSql = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                if (dtSql.Rows.Count > 0)
                {
                    sql = dtSql.Rows[0][0].ToString();
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
                else
                {
                    DataTable dt = new DataTable();
                    dt.Rows.Add("Error in oracle sql generation");
                    return dt;
                }
            }
            else
            {
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
        }

        private string GetParametersFromSQL(string sql)
        {
            var regex = new Regex(@"[:?:](?<Parameter>[\S]+)");
            var matchCollection = regex.Matches(sql);
            var result = matchCollection.Cast<System.Text.RegularExpressions.Match>().Select(x => x.Groups["Parameter"].Value).ToList<string>();
            return string.Join(",", result);
        }
        public async Task<object> GetDataSourceData(string appName, string datasource, Dictionary<string, string> sqlParams = null)
        {
            var sql = await _redis.HashGetAsync(appName.ToUpper(), datasource.ToUpper());
            if (string.IsNullOrEmpty(sql))
            {
                sql = await GetDataSourceSQLQuery(appName, datasource);

                if (string.IsNullOrEmpty(sql))
                {
                    return Constants.RESULTS.NO_RECORDS.ToString();
                }

            }
            sql = sql.Replace(":", "@");
            ParamsDetails parameters = GetSQLParams(sqlParams);

            string connectionString = await GetDBConnString(appName);
            return await _postGres.ExecuteSelectSql(sql, connectionString, parameters);
        }

        public async Task<Dictionary<string, string>> GetActiveTaskParams(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETACTIVETASKPARAMS.ToString();
            string[] paramName = { "@transid", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.SqlParams["transid"].ToLower(), processFlow.SqlParams["keyvalue"].ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var dtTaskParams = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

            Dictionary<string, string> taskParams = new Dictionary<string, string>();
            if (dtTaskParams.Rows.Count > 0)
            {
                taskParams = SplitTaskParams(dtTaskParams.Rows[0]["taskparams"].ToString());
            }
            return taskParams;
        }


        public async Task<object> GetTimelineData(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_TIMELINE_DATA.ToString();
            string[] paramName = { "@processname", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.KeyValue.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_TIMELINE_DATA_ORACLE.ToString();
                var dtSql = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                if (dtSql.Rows.Count > 0)
                {
                    sql = dtSql.Rows[0][0].ToString();
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
                else
                {
                    DataTable dt = new DataTable();
                    dt.Rows.Add("Error in oracle sql generation");
                    return dt;
                }
            }
            else
            {
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }

        }

        public async Task<object> GetSendToUsers(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_SENDTOUSERS.ToString();
            string[] paramName = { "@allowsendflg", "@actor", "@processname", "@keyvalue", "@taskname" };
            DbType[] paramType = { DbType.Int32, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { processFlow.SendToFlag, processFlow.SendToActor, processFlow.ProcessName, processFlow.KeyValue, processFlow.TaskName };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_SENDTOUSERS_ORACLE.ToString();
                var dtSql = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                if (dtSql.Rows.Count > 0)
                {
                    sql = dtSql.Rows[0][0].ToString();
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
                else
                {
                    DataTable dt = new DataTable();
                    dt.Rows.Add("Error in oracle sql generation");
                    return dt;
                }
            }
            else
            {
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }

        }

        public async Task<SQLResult> GetTaskStatus(ARMTaskAction taskAction)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(taskAction.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_TASKSTATUS.ToString();
            string[] paramName = { "@taskid" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { taskAction.TaskId };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_TASKSTATUS_ORACLE.ToString();
            }
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetProcessUserType(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_PROCESSUSERTYPE.ToString();
            string[] paramName = { "@processname", "@username" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.ToUser.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<object> GetNextTaskInProcess(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_NEXTTASKINPROCESS.ToString();
            string[] paramName = { "@processname", "@username", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.ToUser.ToLower(), processFlow.KeyValue.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);


        }

        public async Task<object> GetEditableTask(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_EDITABLETASK.ToString();
            string[] paramName = { "@processname", "@username", "@keyvalue", "@taskname", "@indexno" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.Int64 };
            object[] paramValue = { processFlow.ProcessName, processFlow.ToUser, processFlow.KeyValue, processFlow.TaskName, processFlow.IndexNo };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<object> GetEditableForm(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_PEGRECORDDETAILS.ToString();
            string[] paramName = { "@processname", "@keyvalue", "@recordid" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.Int64 };
            object[] paramValue = { processFlow.ProcessName, processFlow.KeyValue, Convert.ToInt64(processFlow.RecordId?.ToString()) };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var dt =  await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            if(dt != null && dt.Rows.Count > 0)
            {
                processFlow.TaskName = dt.Rows[0][0]?.ToString() ?? "";
                processFlow.IndexNo = Convert.ToInt16(dt.Rows[0][1]?.ToString() ?? "0");

                return await GetEditableTask(processFlow);
            }
            else
            {
                var table = new DataTable();
                table.Columns.Add("editable", typeof(string));
                table.Rows.Add("F");
                return table;
            }


        }

        public async Task<object> GetSkippableTask(ARMProcessFlowTask processFlow)
        {
            string connectionString = await GetDBConnString(processFlow.AppName);
            string sql = Constants_SQL.GET_EDITABLETASK.ToString();
            string[] paramName = { "@processname", "@username", "@keyvalue", "@taskname" };
            NpgsqlDbType[] paramType = { NpgsqlDbType.Varchar, NpgsqlDbType.Varchar, NpgsqlDbType.Varchar, NpgsqlDbType.Varchar };
            object[] paramValue = { processFlow.ProcessName, processFlow.ToUser, processFlow.KeyValue, processFlow.TaskName };
            return await _postGres.ExecuteSql(sql, connectionString, paramName, paramType, paramValue);
        }
        public async Task<object> IsEditableTask(ARMTask task, ARMTaskAction taskAction)
        {
            string sql = Constants_SQL.GET_EDITABLETASK.ToString();
            Dictionary<string, string> config = await _common.GetDBConfigurations(taskAction.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string[] paramName = { "@processname", "@taskname", "@keyvalue", "@username" };
            NpgsqlDbType[] paramType = { NpgsqlDbType.Varchar, NpgsqlDbType.Varchar, NpgsqlDbType.Varchar, NpgsqlDbType.Varchar };
            object[] paramValue = { task.ProcessName, task.TaskName, task.KeyValue, task.ToUser };
            //IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            return await _postGres.ExecuteSql(sql, connectionString, paramName, paramType, paramValue);

        }



        public async Task<object> GetOptionalTask(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_OPTIONALTASK.ToString();
            string[] paramName = { "@taskid", "@username" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.TaskId, processFlow.ToUser.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetNextOptionalTask(ARMTaskAction taskAction)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(taskAction.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_NEXTOPTIONALTASK.ToString();
            string[] paramName = { "@processname", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { taskAction.ProcessName, taskAction.KeyValue };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        private Dictionary<string, string> SplitTaskParams(string inputString)
        {
            Dictionary<string, string> paramDetails = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(inputString))
            {
                string[] keyValuePairs = inputString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string pair in keyValuePairs)
                {
                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Split('.')[1].Substring(1);
                        string value = keyValue[1];
                        paramDetails.Add(key, value);
                    }
                }
            }
            return paramDetails;
        }

        public async Task<string> GetDataSourceSQLQuery(string appName, string dataSource)
        {
            string connectionString = await GetDBConnString(appName);

            string selectSql = Constants_SQL.GETDATASOURCESSQL.ToString();

            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ARMCommon.Model.ConnectionParamsList>();

            parameters.ParamsNames.Add(new ARMCommon.Model.ConnectionParamsList
            {
                Name = "@datasource",
                Type = NpgsqlDbType.Varchar,
                Value = dataSource.ToLower()
            });

            var sql = "";
            var dt = await _postGres.ExecuteSelectSql(selectSql, connectionString, parameters);
            if (dt.Rows.Count > 0)
            {
                sql = dt.Rows[0]["sqltext"].ToString();
            }

            if (string.IsNullOrEmpty(sql))
            {
                return Constants.RESULTS.NO_RECORDS.ToString();
            }
            var paramsList = GetParametersFromSQL(sql);
            await _redis.HashSetAsync(appName.ToUpper(), dataSource.ToUpper(), sql);
            await _redis.HashSetAsync(appName.ToUpper(), $"{dataSource.ToUpper()}-PARAMS", paramsList);
            return $"{sql}";
        }

        private ParamsDetails GetSQLParams(Dictionary<string, string> sqlParams)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ARMCommon.Model.ConnectionParamsList>();

            foreach (var sqlParam in sqlParams)
            {
                parameters.ParamsNames.Add(new ARMCommon.Model.ConnectionParamsList
                {
                    Name = "@" + sqlParam.Key.Split("~")[0],
                    Type = NpgsqlDbType.Varchar, //GetNpgsqlDbType(sqlParam.Key.Split("~")[1]),
                    Value = sqlParam.Value
                });
            }
            return parameters;
        }

        public static NpgsqlDbType GetNpgsqlDbType(string type)
        {
            switch (type)
            {
                case "number":
                    return NpgsqlDbType.Integer;
                case "text":
                    return NpgsqlDbType.Varchar;
                case "largetext":
                    return NpgsqlDbType.Text;
                case "select":
                    return NpgsqlDbType.Varchar;
                case "date":
                    return NpgsqlDbType.Date;
                case "timestamp":
                    return NpgsqlDbType.Timestamp;
                case "boolean":
                    return NpgsqlDbType.Boolean;
                case "float":
                    return NpgsqlDbType.Double;
                case "numeric":
                    return NpgsqlDbType.Numeric;
                case "uuid":
                    return NpgsqlDbType.Uuid;
                case "bytea":
                    return NpgsqlDbType.Bytea;
                case "json":
                    return NpgsqlDbType.Json;
                case "jsonb":
                    return NpgsqlDbType.Jsonb;
                case "default":
                    return NpgsqlDbType.Varchar;
            }
            return NpgsqlDbType.Varchar;
        }

        #region Active List - Home Page
        public async Task<ARMProcessFlowTask> GetProcessTaskDetails(ARMProcessFlowTask taskAction)
        {
            if (string.IsNullOrEmpty(taskAction.ToUser))
            {
                taskAction.ToUser = await _redis.HashGetAsync(taskAction.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }

            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = await _redis.HashGetAsync(taskAction.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            return taskAction;
        }


        public async Task<object> GetActiveTasksList(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                int startrow = (int)((processFlow.PageNo - 1) * processFlow.PageSize) + 1;
                int endrow = (int)((processFlow.PageNo) * processFlow.PageSize);
                string sql = Constants_SQL.GET_ACTIVETASKSLIST_ORACLE.ToString();
                string[] paramName = { "@username", "@startrow", "@endrow" };
                DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), startrow, endrow };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                string sql = Constants_SQL.GET_ACTIVETASKSLIST.ToString();
                int offset = (int)((processFlow.PageNo - 1) * processFlow.PageSize);
                string[] paramName = { "@username", "@pagesize", "@offset" };
                DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), processFlow.PageSize, offset };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
        }

        public async Task<object> GetAllActiveTasks(ARMProcessFlowTask activeTasks)
        {
            var username = await _redis.HashGetAsync(activeTasks.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            if (string.IsNullOrEmpty(activeTasks.Filter))
            {
                activeTasks.Filter = "All";
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(activeTasks.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                int startrow = (int)((activeTasks.PageNo - 1) * activeTasks.PageSize) + 1;
                int endrow = (int)((activeTasks.PageNo) * activeTasks.PageSize);
                string sql = Constants_SQL.GET_ALL_ACTIVETASKS_ORACLE.ToString();
                string[] paramName = { "@username", "@filter", "@filter", "@startrow", "@endrow" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), activeTasks.Filter.ToLower(), activeTasks.Filter.ToLower(), startrow, endrow };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                string sql = Constants_SQL.GET_ALL_ACTIVETASKS.ToString();
                
                if(dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
                    sql = Constants_SQL.GET_ALL_ACTIVETASKS_MSSQL.ToString();

                int offset = (int)((activeTasks.PageNo - 1) * activeTasks.PageSize);
                string[] paramName = { "@username", "@filter", "@pagesize", "@offset" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), activeTasks.Filter.ToLower(), activeTasks.PageSize, offset };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
        }

        public async Task<object> GetCompletedTasksList(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                int startrow = (int)((processFlow.PageNo - 1) * processFlow.PageSize) + 1;
                int endrow = (int)((processFlow.PageNo) * processFlow.PageSize);
                string sql = Constants_SQL.GET_COMPLETEDTASKSLIST_ORACLE.ToString();
                string[] paramName = { "@username", "@startrow", "@endrow" };
                DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), startrow, endrow };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {

                string sql = Constants_SQL.GET_COMPLETEDTASKSLIST.ToString();
                int offset = (int)((processFlow.PageNo - 1) * processFlow.PageSize);
                string[] paramName = { "@username", "@pagesize", "@offset" };
                DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
                object[] paramValue = { username.ToLower(), processFlow.PageSize, offset };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
        }

        public async Task<object> GetActiveTasksCount(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_ACTIVETASKSCOUNT.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { username.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }
        public async Task<object> GetCompletedTasksCount(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_COMPLETEDTASKSCOUNT.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { username.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetFilteredActiveTasksList(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_FILTEREDACTIVETASKSLIST.ToString();
            int offset = (int)((processFlow.PageNo - 1) * processFlow.PageSize);
            string[] paramName = { "@username", "@pagesize", "@offset" };
            DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
            object[] paramValue = { username.ToLower(), processFlow.PageSize, offset };

            List<string> filterParamName = new List<string>();
            List<DbType> filterParamType = new List<DbType>();
            List<object> filterParamValue = new List<object>();

            if (!string.IsNullOrEmpty(processFlow.FromUser))
            {
                sql = sql.Replace("$FROMUSERFILER$", " and lower(fromuser) = @fromuser ");
                filterParamName.Add("@fromuser");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.FromUser.ToLower());
            }
            else
                sql = sql.Replace("$FROMUSERFILER$", "");

            if (!string.IsNullOrEmpty(processFlow.ProcessName))
            {
                sql = sql.Replace("$PROCESSFILER$", " and lower(processname) = @processname ");
                filterParamName.Add("@processname");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.ProcessName.ToLower());
            }
            else
                sql = sql.Replace("$PROCESSFILER$", "");

            if (!string.IsNullOrEmpty(processFlow.FromDate) && !string.IsNullOrEmpty(processFlow.ToDate))
            {
                sql = sql.Replace("$DATEFILTER$", " and (TO_TIMESTAMP(SUBSTRING(edatetime FROM 1 FOR 14), 'YYYYMMDDHH24MISS')  BETWEEN TO_TIMESTAMP(TO_CHAR(TO_DATE(@fromdate, 'DD-Mon-YYYY'), 'YYYYMMDD') || '000000', 'YYYYMMDDHH24MISS') AND TO_TIMESTAMP(TO_CHAR(TO_DATE(@todate, 'DD-Mon-YYYY'), 'YYYYMMDD') || '235959', 'YYYYMMDDHH24MISS')) ");

                filterParamName.Add("@fromdate");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.FromDate.ToLower());

                filterParamName.Add("@todate");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.ToDate.ToLower());
            }
            else
                sql = sql.Replace("$DATEFILTER$", "");

            if (!string.IsNullOrEmpty(processFlow.SearchText))
            {
                sql = sql.Replace("$SEARCHFILTER$", " and (lower(displaytitle) like '%$SEARCHTEXT$%' or lower(displaycontent) like '%$SEARCHTEXT$%' or lower(keyvalue) like '%$SEARCHTEXT$%') ");
                sql = sql.Replace("$SEARCHTEXT$", processFlow.SearchText.ToLower());
            }
            else
                sql = sql.Replace("$SEARCHFILTER$", "");

            if (filterParamName.Count > 0)
            {
                paramName = MergeParamNames(paramName, filterParamName);
            }

            if (filterParamType.Count > 0)
            {
                paramType = MergeParamTypes(paramType, filterParamType);
            }

            if (filterParamValue.Count > 0)
            {
                paramValue = MergeParamValues(paramValue, filterParamValue);
            }

            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetFilteredCompletedTasksList(ARMProcessFlowTask processFlow)
        {
            var username = await _redis.HashGetAsync(processFlow.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            if (username == null)
            {
                return Constants.RESULTS.NO_RECORDS;
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_FILTEREDCOMPLETEDTASKSLIST.ToString();
            int offset = (int)((processFlow.PageNo - 1) * processFlow.PageSize);
            string[] paramName = { "@username", "@pagesize", "@offset" };
            DbType[] paramType = { DbType.String, DbType.Int32, DbType.Int32 };
            object[] paramValue = { username.ToLower(), processFlow.PageSize, offset };

            List<string> filterParamName = new List<string>();
            List<DbType> filterParamType = new List<DbType>();
            List<object> filterParamValue = new List<object>();

            if (!string.IsNullOrEmpty(processFlow.FromUser))
            {
                sql = sql.Replace("$FROMUSERFILER$", " and lower(fromuser) = @fromuser ");
                filterParamName.Add("@fromuser");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.FromUser.ToLower());
            }
            else
                sql = sql.Replace("$FROMUSERFILER$", "");

            if (!string.IsNullOrEmpty(processFlow.ProcessName))
            {
                sql = sql.Replace("$PROCESSFILER$", " and lower(processname) = @processname ");
                filterParamName.Add("@processname");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.ProcessName.ToLower());
            }
            else
                sql = sql.Replace("$PROCESSFILER$", "");

            if (!string.IsNullOrEmpty(processFlow.FromDate) && !string.IsNullOrEmpty(processFlow.ToDate))
            {
                sql = sql.Replace("$DATEFILTER$", " and (TO_TIMESTAMP(SUBSTRING(edatetime FROM 1 FOR 14), 'YYYYMMDDHH24MISS')  BETWEEN TO_TIMESTAMP(TO_CHAR(TO_DATE(@fromdate, 'DD-Mon-YYYY'), 'YYYYMMDD') || '000000', 'YYYYMMDDHH24MISS') AND TO_TIMESTAMP(TO_CHAR(TO_DATE(@todate, 'DD-Mon-YYYY'), 'YYYYMMDD') || '235959', 'YYYYMMDDHH24MISS')) ");

                filterParamName.Add("@fromdate");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.FromDate.ToLower());

                filterParamName.Add("@todate");
                filterParamType.Add(DbType.String);
                filterParamValue.Add(processFlow.ToDate.ToLower());
            }
            else
                sql = sql.Replace("$DATEFILTER$", "");

            if (!string.IsNullOrEmpty(processFlow.SearchText))
            {
                sql = sql.Replace("$SEARCHFILTER$", " and (lower(displaytitle) like '%$SEARCHTEXT$%' or lower(displaycontent) like '%$SEARCHTEXT$%' or lower(keyvalue) like '%$SEARCHTEXT$%') ");
                sql = sql.Replace("$SEARCHTEXT$", processFlow.SearchText.ToLower());
            }
            else
                sql = sql.Replace("$SEARCHFILTER$", "");

            if (filterParamName.Count > 0)
            {
                paramName = MergeParamNames(paramName, filterParamName);
            }

            if (filterParamType.Count > 0)
            {
                paramType = MergeParamTypes(paramType, filterParamType);
            }

            if (filterParamValue.Count > 0)
            {
                paramValue = MergeParamValues(paramValue, filterParamValue);
            }

            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }
        private string[] MergeParamNames(string[] oldArr, List<string> newList)
        {
            string[] tmpArr = newList.ToArray();
            int totalSize = oldArr.Length + tmpArr.Length;
            string[] newArr = new string[totalSize];
            Array.Copy(oldArr, newArr, oldArr.Length);
            Array.Copy(tmpArr, 0, newArr, oldArr.Length, tmpArr.Length);
            return newArr;
        }

        private DbType[] MergeParamTypes(DbType[] oldArr, List<DbType> newList)
        {
            DbType[] tmpArr = newList.ToArray();
            int totalSize = oldArr.Length + tmpArr.Length;
            DbType[] newArr = new DbType[totalSize];
            Array.Copy(oldArr, newArr, oldArr.Length);
            Array.Copy(tmpArr, 0, newArr, oldArr.Length, tmpArr.Length);
            return newArr;
        }

        private Object[] MergeParamValues(Object[] oldArr, List<Object> newList)
        {
            Object[] tmpArr = newList.ToArray();
            int totalSize = oldArr.Length + tmpArr.Length;
            Object[] newArr = new Object[totalSize];
            Array.Copy(oldArr, newArr, oldArr.Length);
            Array.Copy(tmpArr, 0, newArr, oldArr.Length, tmpArr.Length);
            return newArr;
        }

        public async Task<DataTable> GetBulkApprovalCount(ARMTask task)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(task.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_BULKAPPROVALCOUNT.ToString();

            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { task.ToUser.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<object> GetApproveToTasks(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_APPROVETO_TASKS.ToString();
            string[] paramName = { "@processname", "@indexno", "@taskname", "@transid", "@recordid", "@username", "@taskid", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.Int32, DbType.String, DbType.String, DbType.Int64, DbType.String, DbType.Int64, DbType.String };
            object[] paramValue = { processFlow.ProcessName, processFlow.IndexNo, processFlow.TaskName, processFlow.TransId, (string.IsNullOrEmpty(processFlow.RecordId) ? 0 : Convert.ToInt64(processFlow.RecordId)), processFlow.AxUserName, Convert.ToInt64(processFlow.TaskId), processFlow.KeyValue };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_APPROVETO_TASKS_ORACLE.ToString();
            }

            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<object> GetReturnToTasks(ARMProcessFlowTask processFlow)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(processFlow.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_RETURNTO_TASKS.ToString();
            string[] paramName = { "@processname", "@keyvalue", "@indexno" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.Int32 };
            object[] paramValue = { processFlow.ProcessName, processFlow.KeyValue, processFlow.IndexNo };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        #endregion
        #region PEG approval via mail

        public async Task<object> ValidateAndSaveSession(ARMMailTaskAction task)
        {
            var powerUser = await GetPowerUser(task.UserId, task.Password, task.AppName);
            string usergroup = Constants.GROUPTYPES.POWER.ToString();
            if (string.IsNullOrEmpty(powerUser.error) && powerUser.data.Rows.Count > 0)
            {
                var userRoles = await GetPowerUserRoles(task.UserId, task.AppName);
                var userRoleslist = userRoles.data.AsEnumerable().Select(r => r["USERROLES"].ToString());
                string sessionId = $"{REDIS_PREFIX.ARMSESSION.ToString()}-{Guid.NewGuid().ToString()}";
                var generatedToken = _tokenService.BuildToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(),
                task.UserId, task.AppName, usergroup, usergroup);

                string allroles = String.Empty;
                task.token = generatedToken;
                task.ARMSessionId = sessionId;
                await StoreSessionValues(sessionId, string.Join(",", userRoleslist), task.UserId, task.AppName, usergroup, usergroup, generatedToken);

            }
            else
                return RESULTS.INVALIDPOWERUSER;

            return task;
        }


        public async Task<object> ValidateAndSaveSessionForViewRecord(ARMViewRecordDetails record)
        {
            var powerUser = await GetPowerUser(record.UserId, record.Password, record.AppName);
            string usergroup = Constants.GROUPTYPES.POWER.ToString();
            if (string.IsNullOrEmpty(powerUser.error) && powerUser.data.Rows.Count > 0)
            {
                var userRoles = await GetPowerUserRoles(record.UserId, record.AppName);
                var userRoleslist = userRoles.data.AsEnumerable().Select(r => r["USERROLES"].ToString());
                string sessionId = $"{REDIS_PREFIX.ARMSESSION.ToString()}-{Guid.NewGuid().ToString()}";
                var generatedToken = _tokenService.BuildToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(),
                record.UserId, record.AppName, usergroup, usergroup);

                string allroles = String.Empty;
                record.token = generatedToken;
                record.ARMSessionId = sessionId;
                await StoreSessionValues(sessionId, string.Join(",", userRoleslist), record.UserId, record.AppName, usergroup, usergroup, generatedToken);

            }
            else
                return RESULTS.INVALIDPOWERUSER;

            return record;
        }
        private async Task<SQLResult> GetPowerUser(string username, string password, string appname)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(appname);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETPOWERUSER.ToString();
            string[] paramName = { "@username", "@password" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { username, password };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);

        }
        private async Task<SQLResult> GetPowerUserRoles(string username, string appname)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(appname);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETPOWERUSERROLES.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { username };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<Dictionary<string, string>> StoreSessionValues(string sessionId, string userRoleslist, string username, string appname, string usergroup, string grouptype, string token)
        {
            Dictionary<string, string> sessionValues = new Dictionary<string, string>();
            sessionValues.Add(SESSION_DATA.USER_ROLES.ToString(), string.Join(",", userRoleslist));
            sessionValues.Add(SESSION_DATA.USERNAME.ToString(), username);
            sessionValues.Add(SESSION_DATA.ARMTOKEN.ToString(), token);
            sessionValues.Add(SESSION_DATA.APPNAME.ToString(), appname);
            sessionValues.Add(SESSION_DATA.USERGROUP.ToString(), usergroup);
            if (sessionValues.Count > 0)
            {
                var hashEntries = sessionValues.Select(pair => new HashEntry(pair.Key, pair.Value)).ToArray();
                await _redis.HashSetEntriesAsync(sessionId, hashEntries);
                return sessionValues;
            }
            return sessionValues;

        }

        public async Task<SQLResult> ValidatePowerUsers(string username, string appname)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(appname);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.VALIDATEPOWERUSERS.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { username };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetProcessTasks(string taskid, string appname, string user)
        {
            SQLResult sqlresult = new SQLResult();
            Dictionary<string, string> config = await _common.GetDBConfigurations(appname);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETPROCESSTASK.ToString();
            string[] paramName = { "@taskid" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { taskid.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            var dtTasks = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            if (dtTasks.Rows.Count > 0)
            {
                var currUserTaskRows = dtTasks.AsEnumerable().Where(r => r.Field<string>("touser").ToLower() == user.ToLower());
                DataTable dtResult = new DataTable();
                if (currUserTaskRows.Any())
                    dtResult = currUserTaskRows.CopyToDataTable();

                if (dtResult?.Rows.Count == 1)
                {
                    sqlresult.data = dtResult;
                    return sqlresult;
                }
                else
                {
                    var otherUserTaskRows = dtTasks.AsEnumerable().Where(r => r.Field<string>("taskstatus") != null && r.Field<string>("touser") == r.Field<string>("username"));
                    if (otherUserTaskRows.Any())
                    {
                        sqlresult.data = otherUserTaskRows.CopyToDataTable(); ;
                        return sqlresult;
                    }

                    var pendingUsersList = dtTasks.AsEnumerable().Select(r => r["touser"].ToString()).Distinct();
                    string pendingUsers = string.Join(",", pendingUsersList);
                    DataTable dtPending = dtTasks.Clone();
                    DataRow drPending = dtTasks.Rows[0];
                    drPending["touser"] = pendingUsers;
                    drPending["ispending"] = "T";
                    dtPending.Rows.Add(drPending.ItemArray);
                    sqlresult.data = dtPending;
                    return sqlresult;
                }
            }
            sqlresult.data = dtTasks;
            return sqlresult;
        }


        #endregion

        private async Task<Dictionary<string, string>> GetSessionDataForPEG(ARMTaskAction taskAction)
        {
            var isValidSession = await _common.ValidateSession(taskAction.ARMSessionId);
            if (!isValidSession)
            {
                throw new Exception($"SessionId is not valid");
            }

            var sessionData = new Dictionary<string, string>();
            sessionData["AxSessionId"] = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();
            taskAction.AppName = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();

            var userVars = _common._sessionVars[SESSION_DATA.USERVARS.ToString()].ToString();
            if (userVars == "{}")
                sessionData["UserVars"] = "<uservars></uservars>";
            else
            {
                XmlDocument userVarsDoc = JsonConvert.DeserializeXmlNode("{\"uservars\": " + userVars + "}");
                sessionData["UserVars"] = userVarsDoc.OuterXml;
            }

            var gloVars = _common._sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString();
            if (gloVars == "{}")
                sessionData["GlobalVars"] = "<globalvars></globalvars>";
            else
            {
                XmlDocument gloVarsDoc = JsonConvert.DeserializeXmlNode("{\"globalvars\": " + gloVars + "}");
                sessionData["GlobalVars"] = gloVarsDoc.OuterXml;
            }

            var axApps = await _common.GetAxApps(taskAction.AppName);
            var axProps = await _common.GetAxProps(taskAction.AppName);

            sessionData["AxApps"] = axApps;
            sessionData["AxProps"] = axProps;
                        
            return sessionData;

        }
    }
}
