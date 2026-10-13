using ARMCommon.Helpers;
using ARMCommon.Interface;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using static ARMCommon.Helpers.Constants;
using Constants = ARMCommon.Helpers.Constants;

namespace ARMCommon.Model
{
    public class ARMGetData : IGetData
    {
        private readonly IRedisHelper _redis;
        private readonly IPostgresHelper _postGres;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly DataContext _context;
        private readonly IAPI _api;
        private readonly IRabbitMQProducer _iMessageProducer;

        public ARMGetData(IRedisHelper redis, IPostgresHelper postGres, IConfiguration config, Utils common, DataContext context, IAPI api, IRabbitMQProducer iMessageProducer)
        {
            _redis = redis;
            _postGres = postGres;
            _config = config;
            _common = common;
            _context = context;
            _api = api;
            _iMessageProducer = iMessageProducer;
        }

        public async Task<bool> SaveResultToRedis(string datasource, string dataId, string data)
        {
            string Id = datasource + dataId.ToString();
            try
            {
                await _redis.StringSetAsync(Id, data);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<string> GetDataResponseFromRedis(ARMGetDataRequest data)
        {
            try
            {
                string redisdata = await GetDataResponseFromRedis(data);
                return redisdata;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
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
                    sqlresult = await dbHelper.ExecuteSQLAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                    return sqlresult;
                }
                else
                {
                    var (paramNames, paramTypes, paramValues) = GetSQLParams(sqlParams, sql);
                    sqlresult = await dbHelper.ExecuteSQLAsync(sql, connectionString, paramNames, paramTypes, paramValues);
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

        public async Task<Dictionary<string, string>> GetLoginUser(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            return dictSession;
        }

        public async Task<APIDefinitions> GetAPIDefination(string datasource, string appname)
        {
            return await _context.APIDefinitions.FirstOrDefaultAsync(p => p.DataSourceID.ToLower() == datasource.ToLower() && p.AppName.ToLower() == appname.ToLower());
        }
        public async Task<SQLDataSource> GetSQLDataSource(string datasource, string appname)
        {
            return await _context.SQLDataSource.FirstOrDefaultAsync(p => p.DataSourceID.ToLower() == datasource.ToLower() && p.AppName.ToLower() == appname.ToLower());
        }
        public async Task<SQLDataSource> GetSQLDataSourceFromRedis(string datasource, string appname)
        {
            string key = datasource + "~" + appname + "~" + Constants.KEY.METADATA.ToString();
            var redisData = await _redis.StringGetAsync(key);

            if (string.IsNullOrEmpty(redisData))
            {
                var getSQLDataSource = await GetSQLDataSource(datasource, appname);
                await _redis.StringSetAsync(key, JsonConvert.SerializeObject(getSQLDataSource));
                return getSQLDataSource;
            }
            else
            {
                var deserializedData = JsonConvert.DeserializeObject<SQLDataSource>(redisData);
                return deserializedData;
            }
        }
        public async Task<APIDefinitions> GetAPIDefinitionFromRedis(string datasource, string appname)
        {
            string key = datasource + "~" + appname + "~" + Constants.KEY.METADATA.ToString();
            var redisData = await _redis.StringGetAsync(key);

            if (string.IsNullOrEmpty(redisData))
            {
                var getAPIDefinition = await GetAPIDefination(datasource, appname);
                await _redis.StringSetAsync(key, JsonConvert.SerializeObject(getAPIDefinition));
                return getAPIDefinition;
            }
            else
            {
                var deserializedData = JsonConvert.DeserializeObject<APIDefinitions>(redisData);
                return deserializedData;
            }
        }
        public async Task<ARMResult> GetDataFromAPI(APIDefinitions apiDefinitions, Dictionary<string, string> apiParams, string RefreshQueueName)
        {
            ARMResult apiResult;
            string mediaType = "application/json";
            int refreshInterval = apiDefinitions.DataSyncInterval ?? 0;
            string payload = BindApiParamsToRequestBody(apiDefinitions.DataSourceFormat, apiParams);
            if (apiDefinitions.RequestType.ToLower() == Constants.HTTPMETHOD.HTTPPOST.ToString().ToLower())
            {
                ;
                apiResult = await _api.POSTData(apiDefinitions.DataSourceURL, payload, mediaType);

            }
            else
            {
                apiResult = await _api.GetData(apiDefinitions.DataSourceURL);

            }
            string key = GenerateKeyFromApidefinition(apiDefinitions.AppName, apiDefinitions.DataSourceID, apiDefinitions.DataSourceURL, apiDefinitions.RequestType, payload);
            _iMessageProducer.SendMessages(key, RefreshQueueName, false, refreshInterval);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", apiResult);
            apiResult = result;
            return apiResult;
        }

        public async Task<SQLResult> GetSQLData(SQLDataSource datasource, Dictionary<string, string> sqlParams, string RefreshQueueName, bool expiry = true)
        {
            SQLResult result = new SQLResult();
            try
            {
                string key = GenerateKeyFromSqlParams(datasource.AppName, datasource.DataSourceID, sqlParams);
                Dictionary<string, string> cache = new Dictionary<string, string>();
                if (datasource.iscached)
                {
                    cache = await _redis.HashGetAllDictAsync(key);
                }
                string validTillJson = cache.ContainsKey("valid_till") ? cache["valid_till"] : string.Empty;
                DateTime validTillFromRedis = !string.IsNullOrEmpty(validTillJson) ? JsonConvert.DeserializeObject<DateTime>(validTillJson) : DateTime.MinValue;
                int expirytime = 0;
                int refreshInterval = datasource.DataSyncInterval ?? 0;
                if (cache.Count == 0 || !IsDataValid(validTillFromRedis))
                {
                    if (expiry)
                    {
                        expirytime = datasource.expiry ?? 0;
                    }
                    var table = await GetSQLScriptResult(datasource.AppName, datasource.SQLScript, sqlParams);
                    if (string.IsNullOrEmpty(table.error) && datasource.iscached && table.data != null)
                    {


                        await _redis.HashSetAsync(key, "value", JsonConvert.SerializeObject(table), expirytime);
                        DateTime validTill = DateTime.Now.AddSeconds(refreshInterval);
                        await _redis.HashSetAsync(key, "valid_till", JsonConvert.SerializeObject(validTill));
                        _iMessageProducer.SendMessages(key, RefreshQueueName, false, refreshInterval);
                    }
                    return table;
                }
                else
                {
                    result.data = JsonConvert.DeserializeObject<DataTable>(cache["value"]);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.error = ex.Message;
                return result;
            }
        }

        public async Task<SQLResult> GetDropDownSQLData(SQLDataSource datasource, Dictionary<string, string> sqlParams, string RefreshQueueName, bool expiry = true)
        {
            SQLResult result = new SQLResult();
            try
            {
                var table = await GetSQLScriptResult(datasource.AppName, datasource.SQLScript, sqlParams);
                return table;
            }
            catch (Exception ex)
            {
                result.error = ex.Message;
                return result;
            }
        }

        private async Task<SQLResult> GetSQLScriptResult(string appname, string sqlScript, Dictionary<string, string> sqlParams)
        {
            SQLResult result = new SQLResult();
            Dictionary<string, string> config = await _common.GetDBConfigurations(appname);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string query = BindSqlParamsToStatement(sqlScript, sqlParams);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(query, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            result = await dbHelper.ExecuteSQLAsync(query, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return result;

        }
        //public string BindSqlParamsToStatement(string sqlStatement, Dictionary<string, string> sqlParams)
        //{
        //    var paramNames = GetParametersFromSQLParams(sqlStatement);

        //    foreach (var paramName in paramNames)
        //    {
        //        if (!sqlParams.ContainsKey(paramName))
        //        {
        //            throw new ArgumentException($"Parameter '{paramName}' does not exist in the sqlParams dictionary.");
        //        }

        //        var paramValue = sqlParams[paramName];
        //        var placeholder = $":{paramName}";

        //        sqlStatement = sqlStatement.Replace(placeholder, $"'{paramValue}'");
        //    }

        //    return sqlStatement;
        //}

        public string BindSqlParamsToStatement(string sqlStatement, Dictionary<string, string> sqlParams)
        {
            var paramNames = GetParametersFromSQLParams(sqlStatement);
            var modifiedSqlStatement = new StringBuilder();
            var insideQuotes = false;
            var paramNameBuilder = new StringBuilder();

            for (int i = 0; i < sqlStatement.Length; i++)
            {
                var c = sqlStatement[i];

                if (c == '\'')
                {
                    insideQuotes = !insideQuotes;
                    modifiedSqlStatement.Append(c);
                }
                else if (c == ':' && !insideQuotes && i + 1 < sqlStatement.Length && sqlStatement[i + 1] != '\'')
                {
                    var paramName = GetNextParameterName(sqlStatement, i + 1);

                    if (!sqlParams.ContainsKey(paramName))
                    {
                        throw new ArgumentException($"Parameter '{paramName}' does not exist in the sqlParams dictionary.");
                    }

                    var paramValue = sqlParams[paramName];
                    modifiedSqlStatement.Append($"'{paramValue}'");
                    i += paramName.Length; // Skip parameter name
                }
                else
                {
                    modifiedSqlStatement.Append(c);
                }
            }

            return modifiedSqlStatement.ToString();
        }

        private string GetNextParameterName(string sqlStatement, int startIndex)
        {
            var paramNameBuilder = new StringBuilder();

            for (int i = startIndex; i < sqlStatement.Length; i++)
            {
                var c = sqlStatement[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    paramNameBuilder.Append(c);
                }
                else
                {
                    break;
                }
            }

            return paramNameBuilder.ToString();
        }


        public string BindApiParamsToRequestBody(string requestBodyTemplate, Dictionary<string, string> apiParams)
        {
            var paramNames = GetParametersFromApiRequestBody(requestBodyTemplate);

            // Check if all parameter names in requestBodyTemplate exist in apiParams
            var missingParams = paramNames.Except(apiParams.Keys, StringComparer.OrdinalIgnoreCase);
            if (missingParams.Any())
            {
                throw new ArgumentException($"The following parameters are missing in the apiParams dictionary: {string.Join(", ", missingParams)}");
            }

            var mappedRequestBody = requestBodyTemplate;

            foreach (var paramName in paramNames)
            {
                var placeholder = $"{{{{{paramName}}}}}";
                var paramValue = apiParams[paramName];
                mappedRequestBody = ReplaceParameter(mappedRequestBody, paramName, paramValue);
            }

            return mappedRequestBody;
        }
        private string ReplaceParameter(string requestBodyTemplate, string paramName, string paramValue)
        {
            var regex = new Regex($@"{{{{\s*{paramName}\s*}}}}", RegexOptions.IgnoreCase);
            return regex.Replace(requestBodyTemplate, paramValue);
        }

        public List<string> GetParametersFromApiRequestBody(string requestBodyTemplate)
        {
            var parameters = new List<string>();
            var regex = new Regex(@"""([^""]+)""\s*:");
            var matches = regex.Matches(requestBodyTemplate);

            foreach (Match match in matches)
            {
                var parameter = match.Groups[1].Value.Trim();
                parameters.Add(parameter);
            }

            return parameters;
        }

        private List<string> GetParametersFromSQLParams(string sqlStatement)
        {
            var regex = new Regex(@"[:?](?<Parameter>\w+)");
            var matchCollection = regex.Matches(sqlStatement);
            var paramNames = matchCollection.Cast<Match>().Select(m => m.Groups["Parameter"].Value).ToList();
            return paramNames;
        }
        public string GenerateKeyFromSqlParams(string appname, string datasource, Dictionary<string, string> sqlParams)
        {
            var keyParts = new List<string> { $"{appname}~{Constants.SOURCETYPE.SQL.ToString()}~{datasource}~" };
            if (sqlParams != null)
            {
                var isFirstParam = true;

                foreach (var kv in sqlParams)
                {
                    var paramPart = $"{kv.Key}={kv.Value}";

                    if (isFirstParam)
                    {
                        keyParts[0] += paramPart;
                        isFirstParam = false;
                    }
                    else
                    {
                        keyParts.Add(paramPart);
                    }
                }
            }
            return string.Join("&", keyParts);
        }

        public string GenerateKeyFromApidefinition(string appname, string datasource, string url, string method, string requestBody)
        {
            var keyParts = new List<string>
        {
            $"{appname}~{Constants.SOURCETYPE.APIDEFINITION.ToString()}~{datasource}~{url}~{method}~"
        };



            if (string.Equals(method.ToLower(), Constants.HTTPMETHOD.HTTPPOST.ToString().ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                // Extract parameters from request body
                var parameters = ExtractParametersFromJson(requestBody);
                AddParametersToKeyParts(parameters, keyParts);
            }
            else if (string.Equals(method.ToLower(), Constants.HTTPMETHOD.HTTPGET.ToString().ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                // Extract parameters from request parameters
                var parameters = ExtractParametersFromUrl(url);
                AddParametersToKeyParts(parameters, keyParts);
            }



            return string.Join("&", keyParts);
        }
        private void AddParametersToKeyParts(Dictionary<string, string> parameters, List<string> keyParts)
        {
            var isFirstParam = true;



            foreach (var kvp in parameters)
            {
                var paramPart = $"{kvp.Key}={kvp.Value}";



                if (isFirstParam)
                {
                    keyParts[0] += $"{paramPart}";
                    isFirstParam = false;
                }
                else
                {
                    keyParts.Add(paramPart);
                }
            }
        }
        private Dictionary<string, string> ExtractParametersFromJson(string jsonString)
        {
            var parameters = new Dictionary<string, string>();



            try
            {
                // Parse the JSON string
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);



                foreach (var kvp in json)
                {
                    // Add each key-value pair to the parameters dictionary
                    parameters[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                // Handle other general exceptions
                // Log the exception or perform any other necessary error handling
                Console.WriteLine("An error occurred: " + ex.Message);



                // You can also throw a specific exception or return an error message
                throw new Exception("An error occurred while extracting parameters.");
            }



            return parameters;
        }
        private Dictionary<string, string> ExtractParametersFromUrl(string url)
        {
            var parameters = new Dictionary<string, string>();



            // Parse the URL to extract the query string
            var uri = new Uri(url);
            var queryString = uri.Query;



            if (!string.IsNullOrEmpty(queryString))
            {
                // Remove the leading "?" character from the query string
                queryString = queryString.TrimStart('?');



                // Split the query string into individual key-value pairs
                var queryParams = queryString.Split('&');



                foreach (var queryParam in queryParams)
                {
                    // Split each key-value pair into key and value
                    var kvp = queryParam.Split('=');
                    if (kvp.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(kvp[0]);
                        var value = Uri.UnescapeDataString(kvp[1]);



                        // Add the key-value pair to the parameters dictionary
                        parameters[key] = value;
                    }
                }
            }



            return parameters;
        }
        private bool IsDataValid(DateTime ValidTill)
        {
            DateTime currentTime = DateTime.Now;
            DateTime valid_Till = ValidTill; //This should come from cache
            if (valid_Till < currentTime)
            {
                return false;
            }
            return true;
        }
        public async Task<SQLResult> GetSQLDataFromRedis(string Appname, string datasource, Dictionary<string, string> sqlParams)
        {
            SQLResult result = new SQLResult();
            string key = GenerateKeyFromSqlParams(Appname, datasource, sqlParams);
            Dictionary<string, string> cache = await _redis.HashGetAllDictAsync(key);

            result = cache.ContainsKey("value")
                ? JsonConvert.DeserializeObject<SQLResult>(cache["value"])
                : result;

            return result;
        }
        public async Task<bool> ClearCacheData(string keys)
        {
            try
            {
                await _redis.DeleteKeysByPrefixAsync(keys);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> RefreshCacheData(string sessionId)
        {
            try
            {
                HashEntry[] hashEntries = await _redis.HashGetAllAsync(sessionId);

                foreach (var entry in hashEntries)
                {
                    string field = entry.Name;
                    string value = entry.Value;
                    string newSessionId = $"{REDIS_PREFIX.ARMSESSION.ToString()}-{Guid.NewGuid().ToString()}";
                    await _redis.HashSetAsync(newSessionId, field, value);
                }

                await _redis.KeyDeleteAsync(sessionId);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> SendToDataService(ARMGetDataRequest data)
        {
            try
            {
                var id = Guid.NewGuid();
                string dataId = data.datasource + id.ToString();
                var loginuser = await GetLoginUser(data.ARMSessionId);
                var message = new
                {
                    appname = loginuser["APPNAME"],
                    Datasources = data.datasource,
                    Id = id

                };
                _iMessageProducer.SendMessages(JsonConvert.SerializeObject(message), data.RefreshQueueName);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<SQLResult> GetDataFromDatasource(ARMGetDataRequest data)
        {

            Dictionary<string, object> sessionVars = await _common.GetSessionData(data.ARMSessionId);

            var sqlReult = new SQLResult();
            if (sessionVars == null || sessionVars.Count == 0)
            {
                sqlReult.success = false;
                sqlReult.error = $"Session data is not available.";
                return sqlReult;
            }

            data.AppName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            data.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (data.GlobalParams == null || data.GlobalParams.Count == 0)
            {
                data.GlobalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(data.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GETDATASOURCESSQL.ToString();
            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { data.datasource.ToLower() };

            if (data.sqlParams != null && data.sqlParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> param in data.sqlParams)
                {
                    if (data.GlobalParams.ContainsKey(param.Key))
                        data.GlobalParams[param.Key] = param.Value;
                    else
                        data.GlobalParams.Add(param.Key, param.Value);
                }

            }

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, null, data.GlobalParams);
            return await GetJsonFromDataSet(dsResult);

        }
        
        public async Task<SQLDataSetResult> GetDataFromADS(ARMGetDataRequest data)
        {

            Dictionary<string, object> sessionVars = await _common.GetSessionData(data.ARMSessionId);

            var sqlReult = new SQLDataSetResult();
            if (sessionVars == null || sessionVars.Count == 0)
            {
                sqlReult.error = $"Session data is not available.";
                return sqlReult;
            }

            data.AppName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            data.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (data.GlobalParams == null || data.GlobalParams.Count == 0)
            {
                data.GlobalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(data.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GETDATASOURCESSQL.ToString();
            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { data.datasource.ToLower() };

            if (data.sqlParams != null && data.sqlParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> param in data.sqlParams)
                {
                    if (data.GlobalParams.ContainsKey(param.Key))
                        data.GlobalParams[param.Key] = param.Value;
                    else
                        data.GlobalParams.Add(param.Key, param.Value);
                }

            }

            return await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, null, data.GlobalParams);

        }

        private async Task<SQLResult> GetJsonFromDataSet(SQLDataSetResult dsResult)
        {
            var sqlResult = new SQLResult();
            if (!string.IsNullOrEmpty(dsResult.error))
            {
                sqlResult.error = dsResult.error;
            }
            else
            {

                List<Task<string>> jsonTasks = new List<Task<string>>();
                foreach (DataTable dt in dsResult.DataSet.Tables)
                {
                    jsonTasks.Add(Task.Run(() => JsonConvert.SerializeObject(dt)));
                }

                sqlResult.data.Columns.Add("data_json", typeof(string));

                string[] jsonResults = await Task.WhenAll(jsonTasks);
                foreach (string json in jsonResults)
                {
                    sqlResult.data.Rows.Add(json);
                }
            }

            return sqlResult;
        }


        private async Task<SQLDataSetResult> GetDataSet(string dbType, string sql, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteSQLsFromSQLAsync(sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
        }


    }
}


