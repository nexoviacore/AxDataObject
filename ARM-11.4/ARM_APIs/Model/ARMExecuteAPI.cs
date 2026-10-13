using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using NPOI.POIFS.NIO;
using Org.BouncyCastle.Asn1.X509.Qualified;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static ARMCommon.Helpers.Constants;
using Constants = ARMCommon.Helpers.Constants;


namespace ARM_APIs.Model
{
    public class ARMExecuteAPI : IARMExecuteAPI
    {
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly ILogger<ARMExecuteAPI> _logger;

        public ARMExecuteAPI(IRedisHelper redis, IConfiguration config, Utils common, ILogger<ARMExecuteAPI> logger)
        {
            _redis = redis;
            _config = config;
            _common = common;
            _logger = logger;
        }


        public async Task<SQLResult> GetPublishedAPI(string appName, string publicKey)
        {
            SQLResult sqlresult = new SQLResult();
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            if (config == null || config?.Count == 0)
            {
                sqlresult.error = "Invalid project details.";
                return sqlresult;
            }

            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_PUBLISHEDAPI.ToString();
            string[] paramName = { "@publickey" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { publicKey };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            sqlresult.data = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            return sqlresult;
        }


        public ARMAPIDetails DataRowToARMAPIDetailsObject(DataRow row)
        {
            ARMAPIDetails obj = new ARMAPIDetails();
            try
            {
                PropertyInfo[] properties = obj.GetType().GetProperties();
                foreach (PropertyInfo prop in properties)
                {
                    foreach (DataColumn column in row.Table.Columns)
                    {
                        if (string.Equals(prop.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (row[column] != DBNull.Value)
                            {
                                prop.SetValue(obj, Convert.ChangeType(row[column], prop.PropertyType));
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return obj;
        }

        public async Task<ARMAPIDetails> GetAPIObject(SQLResult apiDetails)
        {
            return DataRowToARMAPIDetailsObject(apiDetails.data.Rows[0]);
        }



        public async Task<APIResult> ExecuteAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj, bool validateSecret = true)
        {
            APIResult apiResult = new APIResult();
            if (validateSecret)
            {
                string apiCallTime = _common.DecryptSecret(inputAPI.SecretKey, apiObj.SecretKey);
                if (string.IsNullOrEmpty(apiCallTime))
                {
                    apiResult.error = "API authentication failed due to invalid secret.";
                    return apiResult;
                }
                if (!_common.IsValidAPITime(apiCallTime))
                {
                    apiResult.error = "API authentication failed due to timeout.";
                    return apiResult;
                }
            }
            switch (apiObj.ApiType.ToUpper())
            {
                case "GET FROM CUSTOM DATA SOURCE":
                    foreach (string dataSource in apiObj.ObjName.Split(","))
                    {
                        var sqlResult = await ExecuteGetSQLDataAPI(inputAPI, apiObj, dataSource);
                        if (!string.IsNullOrEmpty(sqlResult.error))
                        {
                            apiResult.error = sqlResult.error;
                            break;
                        }
                        else
                        {
                            apiResult.data.Add(dataSource, (object)sqlResult.data);
                        }
                    }
                    return apiResult;
                case "SUBMIT DATA":
                    var submitResult = await ExecuteSubmitDataAPI(inputAPI, apiObj);
                    return submitResult;
                case "GET IVIEW PARAMETERS":
                    var ivParamsResult = await ExecuteGetReportParamsAPI(inputAPI, apiObj);
                    return ivParamsResult;
                case "GET IVIEW":
                    var ivResult = await ExecuteGetReportAPI(inputAPI, apiObj);
                    return ivResult;
                case "GET PRINT FORM":
                    var printResult = await ExecutePrintFormAPI(inputAPI, apiObj);
                    return printResult;
                case "EXECUTE SCRIPT":
                    var executescriptResult = await ExecuteScriptAPI(inputAPI, apiObj);
                    return executescriptResult;

                default:
                    apiResult.error = "Invalid API type.";
                    return apiResult;
            }
        }


        private async Task<APIResult> ExecuteScriptAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            APIResult result = new APIResult();
            if (inputAPI.ExecuteScript is null)
            {
                result.error = "Input Json is not having 'executescript' node.";
                return result;
            }
            string executeScriptResult = System.Text.Json.JsonSerializer.Serialize(inputAPI.ExecuteScript);
            JObject jsonObject = JObject.Parse(executeScriptResult);
            AddAxpertNodesToJson(ref jsonObject, inputAPI, apiObj);
            string apiInput = JsonConvert.SerializeObject(new { executescript = jsonObject });
            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/executescript";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPIResult(apiResult);
        }




        private async Task<APIResult> ExecuteGetSQLDataAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj, string sqlName)
        {
            APIResult result = new APIResult();
            string sqlDataJson = System.Text.Json.JsonSerializer.Serialize(inputAPI.GetSqlData);
            JObject sqlDataObj = JObject.Parse(sqlDataJson);
            AddAxpertNodesToJson(ref sqlDataObj, inputAPI, apiObj);
            _common.UpdateOrAddJsonKey(ref sqlDataObj, "sqlname", sqlName);
            if (inputAPI.SQLParams == null)
            {
                inputAPI.SQLParams = new Dictionary<string, string>();
            }
            string apiInput = JsonConvert.SerializeObject(new { getsqldata = sqlDataObj, sqlparams = inputAPI.SQLParams });

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBMenuRest.dll/datasnap/rest/TASBMenuRest/GetSqldata";

            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPISQLResult(apiResult);
        }

        private async Task<APIResult> ExecuteSubmitDataAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            APIResult result = new APIResult();
            if (inputAPI.SubmitData is null)
            {
                result.error = "Input Json is not having 'submitdata' node.";
                return result;
            }
            string submitDataJson = System.Text.Json.JsonSerializer.Serialize(inputAPI.SubmitData);
            JObject submitDataObj = JObject.Parse(submitDataJson);
            AddAxpertNodesToJson(ref submitDataObj, inputAPI, apiObj);

            string apiInput = JsonConvert.SerializeObject(new { submitdata = submitDataObj });

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);

            string apiUrl = AxpertWebScriptsURL + "/ASBRapidSaveRest.dll/datasnap/rest/TASBRapidSaveRest/submitdata";
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPIResult(apiResult);
        }

        private async Task<APIResult> ExecuteGetReportParamsAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            APIResult result = new APIResult();
            if (inputAPI.GetReportParams is null)
            {
                result.error = "Input Json is not having 'getreportparams' node.";
                return result;
            }

            string reportParamsJson = System.Text.Json.JsonSerializer.Serialize(inputAPI.GetReportParams);
            JObject reportParamsObj = JObject.Parse(reportParamsJson);
            AddAxpertNodesToJson(ref reportParamsObj, inputAPI, apiObj);

            string apiInput = JsonConvert.SerializeObject(new { getreportparams = reportParamsObj });

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBIviewRest.dll/datasnap/rest/TASBIViewREST/GetReportParams";

            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPIResult(apiResult);
        }

        private async Task<APIResult> ExecuteGetReportAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            APIResult result = new APIResult();
            if (inputAPI.GetReport is null)
            {
                result.error = "Input Json is not having 'getreport' node.";
                return result;
            }

            string reportJson = System.Text.Json.JsonSerializer.Serialize(inputAPI.GetReport);
            JObject reportObj = JObject.Parse(reportJson);
            AddAxpertNodesToJson(ref reportObj, inputAPI, apiObj);

            string apiInput = JsonConvert.SerializeObject(new { getreport = reportObj });

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBIviewRest.dll/datasnap/rest/TASBIViewREST/GetReport";

            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPIResult(apiResult);
        }

        private async Task<APIResult> ExecutePrintFormAPI(ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            APIResult result = new APIResult();
            if (inputAPI.GetPrintForm is null)
            {
                result.error = "Input Json is not having 'getprintform' node.";
                return result;
            }

            string printJson = System.Text.Json.JsonSerializer.Serialize(inputAPI.GetPrintForm);
            JObject printObj = JObject.Parse(printJson);
            AddAxpertNodesToJson(ref printObj, inputAPI, apiObj);

            if (!string.IsNullOrEmpty(apiObj.PrintForm))
            {
                _common.UpdateOrAddJsonKey(ref printObj, "printform", apiObj.PrintForm);
            }

            string apiInput = JsonConvert.SerializeObject(new { getprintform = printObj });

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{inputAPI.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(inputAPI.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/getprintform";

            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            return ParseAxpertRestAPIResult(apiResult);
        }

        private APIResult ParseAxpertRestAPIResult(ARMResult armResult) {
            APIResult result = new APIResult();
            result.data.Add("success", armResult.result["success"]);
            JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
            if (resultJsonObj?["status"] == null) {
                resultJsonObj = (JObject)resultJsonObj?["result"]?[0];
            }
            if (!(resultJsonObj?["status"]?.ToString().ToLower() == "true" || resultJsonObj?["status"]?.ToString().ToLower() == "success")) {
                result.error = resultJsonObj?["result"]?.ToString();
                return result;
            }
            foreach (var property in resultJsonObj.Properties())
            {
                if(property.Name.ToLower() != "status")
                    result.data.Add(property.Name, property.Value);
            }
            return result;

        }


        private APIResult ParseAxpertRestAPISQLResult(ARMResult armResult)
        {
            Console.WriteLine("ARMResult:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(armResult, Newtonsoft.Json.Formatting.Indented));

            APIResult result = new APIResult();
            result.data.Add("success", armResult.result["success"]);

            JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());

            string status = resultJsonObj["status"]?.ToString().ToLower();
            if (!(status == "true" || status == "success"))
            {
                result.error = resultJsonObj.ToString();
                return result;
            }
            JArray fields = (JArray)resultJsonObj["result"]["result"]["fields"];
            JArray rows = (JArray)resultJsonObj["result"]["result"]["row"];
            result.data.Add("fields", fields);
            result.data.Add("rows", rows);

            return result;
        }



        private void AddAxpertNodesToJson(ref JObject jsonObject, ARMPublishedAPI inputAPI, ARMAPIDetails apiObj)
        {
            _common.AddJsonKey(ref jsonObject, "username", apiObj.Uname);
            _common.UpdateOrAddJsonKey(ref jsonObject, "project", inputAPI.Project);
            _common.UpdateOrAddJsonKey(ref jsonObject, "name", apiObj.ObjName);

            AxpertRestAPIToken axpertToken = new AxpertRestAPIToken(jsonObject["username"].ToString());
            _common.RemoveJsonKey(ref jsonObject, "username");
            _common.UpdateOrAddJsonKey(ref jsonObject, "token", axpertToken.token);
            _common.UpdateOrAddJsonKey(ref jsonObject, "seed", axpertToken.seed);
            _common.UpdateOrAddJsonKey(ref jsonObject, "userauthkey", axpertToken.userAuthKey);
        }

        public async Task<SQLResult> GetDataSourceData(string appName, string datasource, Dictionary<string, string> sqlParams)
        {
            SQLResult sqlresult = new SQLResult();
            string sql = await GetDataSourceSQLQuery(appName, datasource);

            if (string.IsNullOrEmpty(sql))
            {
                sqlresult.error = Constants.RESULTS.NO_RECORDS.ToString();
                return sqlresult;
            }
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            try
            {
                sql = sql.Replace(":", "@");
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });

                if (sqlParams == null)
                {
                    sqlresult.data = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                    return sqlresult;
                }
                else
                {
                    var (paramNames, paramTypes, paramValues) = GetSQLParams(sqlParams, sql);
                    sqlresult.data = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                    return sqlresult;
                }
            }
            catch (Exception ex)
            {
                sqlresult.error = ex.Message;
                return sqlresult;
            }
        }




        public async Task<string> GetDataSourceSQLQuery(string appName, string dataSource)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string selectSql = Constants_SQL.GETDATASOURCESSQL.ToString();
            string sql = "";
            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { dataSource.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(selectSql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            var dt = await dbHelper.ExecuteQueryAsync(selectSql, connectionString, paramName, paramType, paramValue);
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
        private string GetParametersFromSQL(string sql)
        {
            var regex = new Regex(@"@(?<Parameter>\w+)");
            var matchCollection = regex.Matches(sql);
            var parameters = matchCollection.Cast<Match>().Select(x => x.Groups["Parameter"].Value);
            return string.Join(",", parameters);
        }

        private (string[] paramNames, DbType[] paramTypes, object[] paramValues) GetSQLParams(Dictionary<string, string> sqlParams, string sql)
        {
            var paramNames = new List<string>();
            var paramTypes = new List<DbType>();
            var paramValues = new List<object>();

            var sqlParamNames = GetParametersFromSQL(sql);

            foreach (var sqlParam in sqlParams)
            {
                var paramName = sqlParam.Key.Split("~")[0];

                if (sqlParamNames.Contains(paramName))
                {
                    paramNames.Add("@" + paramName);
                    paramTypes.Add(DbType.String);
                    paramValues.Add(sqlParam.Value);
                }
            }

            return (paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());


        }







    }
}




