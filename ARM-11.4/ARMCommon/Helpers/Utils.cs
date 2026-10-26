using AgileConnect.EncrDecr.cs;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Data;
using System.Data.Odbc;
using System.Text;
using System.Security.Cryptography;
using static ARMCommon.Helpers.Constants;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Data.Common;
using System.Collections;
using System.Globalization;

namespace ARMCommon.Helpers
{
    public class Utils
    {
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IARMLogTraceService _traceService;
        public Dictionary<string, string> _dbConnections = new Dictionary<string, string>();
        public Dictionary<string, string> _dbVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object> _sessionVars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> _globalVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IDbHelper _dbHelper;
        public string _appName;
        public string _armSessionId;
        public string _schemaName;
        public string _schemaNameALC;
        public string _dbType = string.Empty;
        public IRedisCacheHelper _redisCache;
        public IAppSettingsHelper _appSettingsHelper;
        public Dictionary<string, string> _dbConfig;
        public bool _milliSecsInTimeStamp = false;
        public string ApiDateFormat = "dd/MM/yyyy";
        public string ApiTimeFormat = "HH:mm:ss";

        public Utils(IConfiguration config, DataContext context, IRedisHelper redis, IHttpContextAccessor httpContextAccessor, IARMLogTraceService traceService, IRedisCacheHelper redisCache, IAppSettingsHelper appSettingsHelper)
        {
            _config = config;
            _context = context;
            _redis = redis;
            _httpContextAccessor = httpContextAccessor;
            _traceService = traceService;
            _redisCache = redisCache;
            _appSettingsHelper = appSettingsHelper;
        }

        public Utils(IConfiguration config, DataContext context, IRedisHelper redis)
        {
            _config = config;
            _context = context;
            _redis = redis;
        }

        public Utils(IConfiguration config, IRedisCacheHelper redisCache, IAppSettingsHelper appSettingsHelper, IARMLogTraceService traceService)
        {
            _config = config;
            _traceService = traceService;
            _redisCache = redisCache;
            _appSettingsHelper = appSettingsHelper;
        }

        public Utils()
        {

        }

        public string GetEncryptedSecret(string secretKey)
        {
            AES aes = new AES();
            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return aes.EncryptString(currentTime, secretKey);
        }
        public string GetAppList()
        {
            var appList = _context.ARMApps.Select(u => u.AppName).ToList();

            string appnames = (string.Join(",", appList.Select(x => x.ToString()).ToArray()));

            return appnames;
        }
        public string GetDBConnections(string dbType, string dbuser, string password, string serverConnection, string sqlversion, string appname, string ConnectionName = "")
        {
            string connectionString = "";
            string database = dbuser;

            if (dbType.ToLower() == "oracle")
            {
                database = (ConnectionName != "" ? ConnectionName : appname);
                connectionString = @"Data Source=" + serverConnection + "/" + database + ";User Id=" + dbuser + ";Password=" + password + ";Pooling=False;";
                return connectionString;

            }


            else if (dbType.ToLower() == "postgresql" || dbType.ToLower() == "postgre")
            {
                if (serverConnection.Contains(":"))
                {
                    if (dbuser.Contains("\\") || database.Contains("\\"))
                    {

                        string[] userDtls = dbuser.Split('\\');
                        string[] databaseDtls = database.Split('\\');
                        if (userDtls.Length > 1 && userDtls[1] != "")
                            if ((userDtls.Length > 1 && userDtls[1] != "") && (databaseDtls.Length > 1 && databaseDtls[1] != ""))
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[1];


                            }
                            else
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[0];
                            }

                    }
                    else
                    {
                        database = ConnectionName;

                    }

                    //Default port for postgres is 5432. if it was changed we need to pass the port no seperately in the Conn Str. 
                    string serverPort = serverConnection.Substring(serverConnection.IndexOf(':') + 1);
                    connectionString = @"Server=" + serverConnection.Substring(0, serverConnection.IndexOf(':')) + "; Port=" + serverPort + "; Database=" + database + ";Username=" + dbuser + ";Pwd=" + password + ";Pooling=false;";
                    return connectionString;

                }
                else
                {
                    if (dbuser.Contains("\\") || database.Contains("\\"))
                    {
                        string[] userDtls = dbuser.Split('\\');
                        string[] databaseDtls = database.Split('\\');
                        if ((userDtls.Length > 1 && userDtls[1] != "") && (databaseDtls.Length > 1 && databaseDtls[1] != ""))
                        {
                            dbuser = userDtls[0];
                            database = databaseDtls[1];

                        }
                        else
                        {
                            dbuser = userDtls[0];
                            database = databaseDtls[0];

                        }

                    }
                    else
                    {
                        database = (ConnectionName != "" ? ConnectionName : appname);

                    }
                    connectionString = @"Server=" + serverConnection + ";Database=" + database + ";Username=" + dbuser + ";Pwd=" + password + ";Pooling=false;";
                    return connectionString;


                }


            }


            else if (dbType.ToLower() == "ms sql" || dbType.ToLower() == "mssql")
            {
                database = (ConnectionName != "" ? ConnectionName : appname);
                if (sqlversion.ToUpper() == "ABOVE 2012")
                    serverConnection = GetServerIpAndPort(serverConnection, dbuser, password, database);
                connectionString = @"Server=" + serverConnection + "; Database=" + database + "; User Id=" + dbuser + "; Password=" + password + ";";

                return connectionString;
            }

            else return connectionString;
        }

        public string GetRedisConnections(string host, string password)
        {


            var redisConfig = new
            {
                RedisIP = host,
                Password = password
            };
            var json = JsonConvert.SerializeObject(redisConfig);
            return json;


        }

        public async Task<bool> ModulePageAccess(string pagename, string usergroup)
        {
            try
            {
                var page = await _context.AxModulePages.FirstOrDefaultAsync(p => p.PageName == pagename);
                if (page == null)
                {
                    return false;
                }
                List<string> accessControl = page.AcessControl;
                //if (accessControl.Contains(usergroup))
                if (accessControl.Any(s => s.Equals(usergroup, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool HasNullOrEmptyValues(string json, params string[] fields)
        {
            JObject jObject = JsonConvert.DeserializeObject<JObject>(json);

            foreach (var field in fields)
            {
                if (jObject[field] == null || string.IsNullOrEmpty(jObject[field].ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string> GetDBConfiguration(string appName)
        {
            string ConnectionString = "";
            try
            {
                string key = $"{Constants.DB_PREFIX.ARMCONNECTIONSTRING.ToString()}_{appName}";

                if (_redis.KeyExists(key))
                {
                    await _redis.KeyDeleteAsync(key);
                }

                {
                    if (_dbConnections.ContainsKey(appName))
                    {
                        ConnectionString = GetARMDbConfiguration(appName);
                        _dbConnections[appName] = ConnectionString;
                    }
                    else
                    {
                        ConnectionString = GetARMDbConfiguration(appName);
                        _dbConnections.Add(appName, ConnectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return ConnectionString;
        }

        public async Task<Dictionary<string, string>> GetDBConfigurations(string appName)
        {
            if(_dbConfig == null)
                _dbConfig = _appSettingsHelper.GetDBConfigurations(appName);
            
            return _dbConfig;
        }

        public Dictionary<string, string> GetDBConfigurationSync(string appName)
        {
            if (_dbConfig == null)
                _dbConfig = _appSettingsHelper.GetDBConfigurations(appName);

            return _dbConfig;
        }

        //public async Task<Dictionary<string, string>> GetDBConfigurations(string appName)
        //{
        //    Dictionary<string, string> config = new Dictionary<string, string>();
        //    try
        //    {
        //        string key = $"{Constants.DB_PREFIX.ARMCONNECTIONSTRING.ToString()}_{appName}";
        //        if (_redis.KeyExists(key))
        //        {
        //            var tempConfig = _redis.StringGet(key);
        //            if (!string.IsNullOrEmpty(tempConfig))
        //            {
        //                config = JsonConvert.DeserializeObject<Dictionary<string, string>>(tempConfig);
        //                return config;
        //            }
        //        }

        //        string connectionString = _config[$"ConnectionStrings:{appName}_connectionstring"];
        //        if (string.IsNullOrEmpty(connectionString))
        //        {
        //            connectionString = GetARMDbConfiguration(appName);
        //        }

        //        string dbType = _config[$"ConnectionStrings:{appName}_dbtype"];
        //        if (string.IsNullOrEmpty(dbType))
        //        {
        //            var app = _context.ARMApps.FirstOrDefault(p => p.AppName.ToLower() == appName.ToLower());
        //            dbType = EncrDecr.Decrypt(app.DataBase, app.PrivateKey);
        //        }

        //        config.Add("ConnectionString", connectionString);
        //        config.Add("DBType", dbType);
        //        await _redis.StringSetAsync(key, JsonConvert.SerializeObject(config));

        //        if (_dbConnections.ContainsKey(appName))
        //        {
        //            _dbConnections[appName] = connectionString;
        //        }
        //        else
        //        {
        //            _dbConnections.Add(appName, connectionString);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exception
        //    }
        //    return config;
        //}

        public string GetServerIpAndPort(string deVersion, string uid, string pwd, string database)
        {
            string connStr = $"DSN={deVersion};Uid={uid};Pwd={pwd};";
            string connectionString = "";
            string dataSourceName = string.Empty;

            using (OdbcConnection odbcConn = new OdbcConnection(connStr))
            {
                try
                {
                    odbcConn.Open();
                    using (OdbcCommand cmd = odbcConn.CreateCommand())
                    {
                        cmd.CommandText = @"
                SELECT local_net_address, local_tcp_port 
                FROM sys.dm_exec_connections 
                WHERE session_id = @@SPID";

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string ip = reader["local_net_address"].ToString();
                                string port = reader["local_tcp_port"].ToString();
                                dataSourceName = $"{ip}\\{deVersion},{port}";
                            }
                        }
                    }
                    connectionString = dataSourceName;
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
                finally
                {
                    odbcConn.Close();
                }
            }

            return !string.IsNullOrEmpty(connectionString) ? connectionString : "Failed to get connection string.";
        }


        public string GetARMDbConfiguration(string appName)
        {
            var userGroup = _context.ARMApps.FirstOrDefault(p => p.AppName.ToLower() == appName.ToLower());
            if (userGroup == null)
                throw new KeyNotFoundException("App not found");
            var Key = userGroup.PrivateKey;
            if (Key != null)
            {
                try
                {
                    string ConnectionName = EncrDecr.Decrypt(userGroup.ConnectionName, Key);
                    string DataBase = EncrDecr.Decrypt(userGroup.DataBase, Key);
                    string DBVersion = EncrDecr.Decrypt(userGroup.DBVersion, Key);
                    string UserName = EncrDecr.Decrypt(userGroup.UserName, Key);
                    string Password = EncrDecr.Decrypt(userGroup.Password, Key);
                    string sqlversion = userGroup.Sqlversion;
                    string ARMAppsconnectionString = GetDBConnections(DataBase, UserName, Password, DBVersion, sqlversion, ConnectionName);
                    return ARMAppsconnectionString;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }
            else
            {
                return Constants.RESULTS.NO_RECORDS.ToString();
            }

        }

        public async Task<string> GetDBConfigurationBySessionId(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            string connectionstring = await GetDBConfiguration(dictSession["APPNAME"]);
            return connectionstring;
        }

        public async Task<string> GetAxpertWebURLBySessionId(string ARMSessionId)
        {
            string AxpertWeb_URL = "";
            try
            {
                var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
                var app = _context.ARMApps.Find(dictSession["APPNAME"]);
                if (app == null)
                    throw new KeyNotFoundException("App not found");
                AxpertWeb_URL = app.AxpertWebUrl;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return AxpertWeb_URL;
        }
        public async Task<string> AxpertWebScriptsURL(string appName)
        {
            return GetARMScriptsURL(appName);

            string AxpertWebScripts_URL = _config[$"ConnectionStrings:{appName}_scriptsurl"];
            if (!string.IsNullOrEmpty(AxpertWebScripts_URL))
                return AxpertWebScripts_URL;

            string key = $"ARM_{Constants.AXPERT.AXPERT_ARMWEBSCRIPTS_URL.ToString()}_{appName}";
            AxpertWebScripts_URL = await _redis.StringGetAsync(key);
            if (!string.IsNullOrEmpty(AxpertWebScripts_URL))
                return AxpertWebScripts_URL;

            try
            {
                var app = _context.ARMApps.Find(appName);
                if (app == null)
                    throw new KeyNotFoundException("App not found");
                AxpertWebScripts_URL = app.AxpertScriptsUrl;
                await _redis.StringSetAsync(key, AxpertWebScripts_URL);
            }
            catch (Exception ex)
            {
                return "";
            }
            return AxpertWebScripts_URL;
        }

        public string GetARMScriptsURL(string appName)
        {
            return _appSettingsHelper.GetARMScriptsURL(appName);
        }

        public async Task<string> GetARMWebScriptsURL(string appName)
        {
            return GetARMScriptsURL(appName);

            //string AxpertWebScripts_URL = _config[$"ConnectionStrings:{appName}_scriptsurl"];
            //if (!string.IsNullOrEmpty(AxpertWebScripts_URL))
            //    return AxpertWebScripts_URL;

            //string key = $"ARM_{Constants.AXPERT.AXPERT_ARMWEBSCRIPTS_URL.ToString()}_{appName}";
            //AxpertWebScripts_URL = await _redis.StringGetAsync(key);
            //if (!string.IsNullOrEmpty(AxpertWebScripts_URL))
            //    return AxpertWebScripts_URL;

            //try
            //{
            //    var app = _context.ARMApps.Find(appName);
            //    if (app == null)
            //        throw new KeyNotFoundException("App not found");
            //    AxpertWebScripts_URL = app.AxpertScriptsUrl;
            //    await _redis.StringSetAsync(key, AxpertWebScripts_URL);
            //}
            //catch (Exception ex)
            //{
            //    return "";
            //}
            //return AxpertWebScripts_URL;
        }

        public async Task<bool> DeleteAppSpecificKeys(string appName)
        {            
            string key = $"ARM_{Constants.AXPERT.AXPERT_WEB_URL.ToString()}_{appName}";
            await _redis.KeyDeleteAsync(key);

            key = $"ARM_{Constants.AXPERT.AXPERT_ARMWEBSCRIPTS_URL.ToString()}_{appName}";
            await _redis.KeyDeleteAsync(key);

            key = $"{Constants.DB_PREFIX.ARMCONNECTIONSTRING.ToString()}_{appName}";
            await _redis.KeyDeleteAsync(key);

            return true;
        }        

        public string GetDomainName(string appName)
        {
            string url = _appSettingsHelper.GetLicDomain(appName);
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(UriPartial.Authority); // returns scheme + host (and port if any)
            }
            return string.Empty;
        }

        public async Task<string> AxpertWebScriptsURLBySessionId(string ARMSessionId)
        {
            string AxpertWebScripts_URL = "";
            try
            {
                var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
                var app = _context.ARMApps.Find(dictSession["APPNAME"]);
                if (app == null)
                    throw new KeyNotFoundException("App not found");
                AxpertWebScripts_URL = app.AxpertScriptsUrl;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return AxpertWebScripts_URL;
        }

        public ARMResult GetOTPResult(string messagecode, string regId)
        {
            ARMResult result = new ARMResult();
            result.result.Add("message", messagecode);
            result.result.Add("regid", regId);
            result.result.Add("otplength", _config["OTPLength"]);
            result.result.Add("otpattemptsleft", _config["OTPMaximumattempt"]);
            return result;
        }
        public ARMResult OTPFailureResult(string messagecode, int totalAttempt)
        {
            ARMResult result = new ARMResult();
            result.result.Add("message", messagecode);
            result.result.Add("attemptsleft", totalAttempt - 1);
            return result;
        }

        public async Task<JObject> ParseRMQMessage(string message)
        {
            JObject messageData = JObject.Parse(message);
            string queueData = messageData["queuedata"].ToString();
            string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
            JObject messageDatas = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
            return messageDatas;
        }

        public string DecryptSecret(string cipherText, string key)
        {
            string result = "";
            AES aes = new AES();
            try
            {
                result = aes.DecryptString(cipherText, key);

            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public bool IsValidAPITime(string inputTime)
        {
            if (inputTime.Length != 14 || !DateTime.TryParseExact(inputTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out _))
            {
                return false;
            }

            DateTime inputDateTime = DateTime.ParseExact(inputTime, "yyyyMMddHHmmss", null);
            DateTime currentTime = DateTime.Now;

            if (inputDateTime > currentTime)
                return false;

            TimeSpan timeDifference = currentTime - inputDateTime;
            return timeDifference.TotalSeconds < 180;
        }

        public void UpdateOrAddJsonKey(ref JObject jsonObject, string key, JToken value)
        {
            bool keyExists = false;

            foreach (var property in jsonObject.Properties())
            {
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    property.Value = value;
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                jsonObject[key] = value;
            }
        }

        public void AddJsonKey(ref JObject jsonObject, string key, JToken value)
        {
            bool keyExists = false;
            foreach (var property in jsonObject.Properties())
            {
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                jsonObject[key] = value;
            }
        }

        public void RemoveJsonKey(ref JObject jsonObject, string key)
        {
            foreach (var property in jsonObject.Properties())
            {
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject.Remove(property.Name);
                    break;
                }
            }
        }

        public void UpdateOrAddJsonKeyToDataNode(ref JObject jsonObject, string key, JToken value)
        {
            bool keyExists = false;
            var dataNode = (JObject)jsonObject["dataarray"]?["data"];
            foreach (var property in dataNode.Properties())
            {
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    property.Value = value;
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                dataNode[key] = value;
            }

            jsonObject["dataarray"]["data"] = dataNode;

        }

        public async Task<Dictionary<string, object>> GetSessionData(string sessionId)
        {
            _armSessionId = sessionId;
            if (_sessionVars == null || _sessionVars.Count == 0)            
            {
                HashEntry[] hashEntries;
                var redisConnected = await OpenRedisConnectionAsync(sessionId);
                if (redisConnected)
                    hashEntries = await _redisCache.HashGetAllAsync(sessionId);
                else
                    throw new Exception("Redis connection could not be established");

                Dictionary<string, object> sessionVars = new Dictionary<string, object>();
                foreach (var entry in hashEntries)
                {
                    if(entry.Name == SESSION_DATA.GLOBALVARS.ToString())
                    {
                        var globalVars = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry.Value);
                        _globalVars = globalVars != null ? new Dictionary<string, string>(globalVars, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); ;                        
                    }
                    sessionVars.Add(entry.Name, entry.Value);
                }
                _sessionVars = sessionVars;

            }

            return _sessionVars;
        }

        public async Task<string> GetGlobalVariableType(string varName)
        {
            if (_sessionVars == null || _sessionVars.Count == 0 || _globalVars == null)
            {
                _sessionVars = await GetSessionData(_armSessionId);
            }

            try
            {
                if (_globalVars.ContainsKey(varName) && _globalVars.ContainsKey("appvartypes"))
                {
                    string appVarTypes = _globalVars["appvartypes"].ToString();
                    int i = 0;
                    int varIndex = -1;

                    foreach (var key in _globalVars.Keys)
                    {
                        if (key.ToLower() == varName.ToLower())
                        {
                            varIndex = i;
                            break;
                        }
                        i++;
                    }

                    return appVarTypes[varIndex].ToString();
                }                
            }
            catch { }
            return "c";
        }

        public string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

        public async Task<bool> IsValidRole(string sessionId, List<string> currentRoles, string sessionRolesStr = "")
        {
            if (string.IsNullOrEmpty(sessionRolesStr))
            {
                var roles = await _redis.HashGetAsync(sessionId, SESSION_DATA.USER_ROLES.ToString());
                if (!string.IsNullOrEmpty(roles))
                {
                    var sessionRoles = roles.Split(',').Select(r => r.Trim()).ToList();

                    // Check if all currentRoles are present in sessionRoles
                    return currentRoles.All(role => sessionRoles.Contains(role));
                }
            }
            else
            {
                var sessionRoles = sessionRolesStr.Split(',').Select(r => r.Trim()).ToList();

                // Check if all currentRoles are present in sessionRoles
                return currentRoles.All(role => sessionRoles.Contains(role));
            }

            return false;
        }

        public string GenerateRedisKeyString(string[] keyNodes)
        {
            string redisKey = "";
            if (keyNodes != null || keyNodes.Length > 0)
            {
                foreach (var keyNode in keyNodes)
                {
                    if (!string.IsNullOrEmpty(keyNode))
                        redisKey += $"-{keyNode}";
                }
            }
            if (redisKey.StartsWith("-"))
                redisKey = redisKey.Substring(1);
            return redisKey;
        }


        public async Task<bool> SetViewFilters(string armSessionId, Dictionary<string, string> globalVars)
        {
            Dictionary<string, string> viewFilters = new Dictionary<string, string>();
            foreach (var kvp in globalVars)
            {
                if (kvp.Key.ToLower().EndsWith("_filter"))
                {
                    string filter = kvp.Value;
                    filter = ReplaceSqlParameters(filter, globalVars);
                    viewFilters.Add(Regex.Replace(kvp.Key, "_filter", "", RegexOptions.IgnoreCase), filter);
                }
            }
            await _redis.HashSetAsync(armSessionId, Constants.SESSION_DATA.VIEWFILTERS.ToString(), JsonConvert.SerializeObject(viewFilters));

            return true;
        }

        public async Task<Dictionary<string, string>> GetViewFilters(string armSessionId)
        {
            var viewFilterStr = await _redis.HashGetAsync(armSessionId, Constants.SESSION_DATA.VIEWFILTERS.ToString());
            if (string.IsNullOrEmpty(viewFilterStr))
                return null;
            else
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(viewFilterStr);
        }

        public async Task<Dictionary<string, string>> GetViewFilterForTstruct(string armSessionId, string transId)
        {
            var viewFilterStr = await _redis.HashGetAsync($"{armSessionId}-VIEWFILTERS", transId);
            if (string.IsNullOrEmpty(viewFilterStr))
            {
                string globalVarsStr = await _redis.HashGetAsync(armSessionId, Constants.SESSION_DATA.GLOBALVARS .ToString());
                Dictionary<string, string> globalVars = JsonConvert.DeserializeObject<Dictionary<string, string>>(globalVarsStr);
                bool filterExists = false;
                string filter = "";
                foreach (var kvp in globalVars)
                {
                    if (kvp.Key.ToLower() == $"{transId.ToLower()}_filter" && !string.IsNullOrEmpty(kvp.Value))
                    {
                        filterExists = true;
                        filter = kvp.Value;
                        filter = ReplaceSqlParameters(filter, globalVars);

                        var expiryMins =_appSettingsHelper.GetARMTokenExpiryMinutes(_appName);
                        filterExists = await _redis.HashSetAsync($"{armSessionId}-VIEWFILTERS", transId, filter, expiryMins * 60);

                        break;
                    }
                }

                if (!filterExists)
                {
                    await _redis.HashSetAsync($"{armSessionId}-VIEWFILTERS", transId, "NO VIEWFILTERS");
                    return new Dictionary<string, string>() { { transId, "" } };
                }
                else
                    return new Dictionary<string, string>() { { transId, filter } };
            }
            else
            {
                if (viewFilterStr != "NO VIEWFILTERS")
                    return new Dictionary<string, string>() { { transId, viewFilterStr } };
                else
                    return new Dictionary<string, string>() { { transId, "" } };
            }
        }


        public async Task<Dictionary<string, string>> GetGlobalParams_Old(string armSessionId)
        {
            var globalParamsStr = await _redis.HashGetAsync(armSessionId, Constants.SESSION_DATA.GLOBALVARS.ToString());
            if (string.IsNullOrEmpty(globalParamsStr))
                return null;
            else
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(globalParamsStr);
        }

        public async Task<Dictionary<string, string>> GetGlobalParams(string armSessionId)
        {
            string globalParamsStr = "";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
                globalParamsStr = await _redisCache.HashGetAsync(armSessionId, Constants.SESSION_DATA.GLOBALVARS.ToString());
        
            if (string.IsNullOrEmpty(globalParamsStr))
                return null;
            else
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(globalParamsStr);
        }


        //public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
        //{
        //    Regex regex = new Regex(@"\:\w+");

        //    // Use StringBuilder for efficient string manipulation
        //    StringBuilder sb = new StringBuilder(sql);

        //    // Find all parameter matches in the SQL string
        //    MatchCollection matches = regex.Matches(sql);

        //    // Iterate over matches in reverse order to avoid index issues
        //    for (int i = matches.Count - 1; i >= 0; i--)
        //    {
        //        Match match = matches[i];
        //        string paramKey = match.Value.Substring(1); // Remove leading ':'

        //        // Check if the parameter key exists in the dictionary
        //        if (parameters.ContainsKey(paramKey))
        //        {
        //            // Replace the parameter in SQL with its corresponding value
        //            sb.Remove(match.Index, match.Length); // Remove original parameter
        //            sb.Insert(match.Index, "'" + parameters[paramKey] + "'"); // Insert replacement value
        //        }
        //    }

        //    return sb.ToString();
        //}

        public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
        {
            Regex regex = new Regex(@"\:\w+");
            StringBuilder sb = new StringBuilder(sql);
            MatchCollection matches = regex.Matches(sql);

            // Create a case-insensitive dictionary lookup
            var caseInsensitiveParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

            // Iterate in reverse to avoid messing up string indexes
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                string paramKey = match.Value.Substring(1); // Remove leading ':'

                if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                {
                    sb.Remove(match.Index, match.Length);
                    sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // Escape single quotes for SQL
                }
            }

            return sb.ToString();
        }
        public async Task<DataTable> HandleIncludeExcludeColsOld(DataTable dt, List<string> excludeCols, List<string> includeCols, List<string> maskedFields)
        {
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
 
            // ✅ Include all columns if "all"
            if (includeCols.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            }
            else
            {
                includeCols.AddRange(new List<string> { "transid", "recordid", "modifiedby", "modifiedon", "createdon", "createdby", "rno", "pageno" });
            }

            // ✅ No exclusion if "none"
            if (excludeCols.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludeCols.Clear();
            }            

            // ✅ Final columns to retain
            var finalColumns = includeCols
                .Where(col => !excludeCols.Contains(col, StringComparer.OrdinalIgnoreCase) && dt.Columns.Contains(col))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // ✅ Remove unwanted columns
            var columnsToRemove = dt.Columns.Cast<DataColumn>()
                .Where(c => !finalColumns.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.ColumnName)
                .ToList();

            foreach (var colName in columnsToRemove)
            {
                dt.Columns.Remove(colName);
            }

            // ✅ Make all columns writable
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ReadOnly)
                    col.ReadOnly = false;
            }

            // ✅ Parse masking rules
            var maskRules = maskedFields
                .Where(r => r.Contains("="))
                .Select(r => r.Split('='))
                .Where(r => r.Length == 2)
                .ToDictionary(r => r[0], r => r[1], StringComparer.OrdinalIgnoreCase);

            // ✅ Apply formatting and masking in-place
            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    object value = row[col];

                    if (value != DBNull.Value)
                    {
                        string colName = col.ColumnName;

                        if (maskRules.ContainsKey(colName))
                        {
                            row[col] = ApplyFieldMask(value.ToString(), maskRules[colName]);
                        }
                        else if (col.DataType == typeof(DateTime))
                        {
                            row[col] = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
            }

            return dt;
        }

        public async Task<DataTable> HandleIncludeExcludeCols(DataTable dt, List<string> excludeCols, List<string> includeCols, List<string> maskedFields)
        {
            DataTable newTable = new DataTable();
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            // ✅ If includeCols = ["all"], include all columns
            if (includeCols.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            }
            else
            {
                includeCols.AddRange(new List<string> { "transid", "recordid", "modifiedby", "modifiedon", "createdon", "createdby", "rno", "pageno" });
            }

            // ✅ If excludeCols = ["none"], no columns are excluded
            if (excludeCols.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludeCols.Clear();
            }
            excludeCols.Add("axrnum_forpagination");

            // ✅ Filter final list of columns to include
            var finalColumns = includeCols
                .Where(col => !excludeCols.Contains(col, StringComparer.OrdinalIgnoreCase) && dt.Columns.Contains(col))
                .ToList();

            // ✅ Clone column data types into new table
            foreach (string col in finalColumns)
            {
                DataColumn originalCol = dt.Columns[col];
                var colType = originalCol.DataType;
                if (colType == typeof(Int16) || colType == typeof(Int32) || colType == typeof(Int64) || colType == typeof(int) || colType == typeof(float) || colType == typeof(decimal) || colType == typeof(double))
                    colType = typeof(String);

                DataColumn newCol = new DataColumn(originalCol.ColumnName, colType);
                newCol.ReadOnly = false; // Make all columns writable
                newTable.Columns.Add(newCol);
            }            

            // Parse masking rules
            var maskRules = maskedFields
                .Where(r => r.Contains("="))
                .Select(r => r.Split('='))
                .Where(r => r.Length == 2)
                .ToDictionary(r => r[0], r => r[1]);

            // ✅ Copy filtered and masked data to new table
            foreach (DataRow row in dt.Rows)
            {
                DataRow newRow = newTable.NewRow();

                foreach (string col in finalColumns)
                {
                    object value = row[col];

                    if (value != DBNull.Value)
                    {
                        if (maskRules.ContainsKey(col))
                        {
                            newRow[col] = ApplyFieldMask(value.ToString(), maskRules[col]);
                        }
                        else if (dt.Columns[col].DataType == typeof(DateTime))
                        {
                            // Apply datetime formatting
                            newRow[col] = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            newRow[col] = value;
                        }
                    }
                    else
                    {
                        newRow[col] = DBNull.Value;
                    }
                }

                newTable.Rows.Add(newRow);
            }

            return newTable;
        }

        public async Task<DataTable> HandleIncludeExcludeCols(DataTable dt, APIResult accessResult)
        {
            var excludeCols = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
            var includeCols = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
            var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

            DataTable newTable = new DataTable();
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            // ✅ If includeCols = ["all"], include all columns
            if (includeCols.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            }
            else
            {
                includeCols.AddRange(new List<string> { "transid", "recordid", "modifiedby", "modifiedon", "createdon", "createdby", "rno", "pageno" });
            }

            // ✅ If excludeCols = ["none"], no columns are excluded
            if (excludeCols.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludeCols.Clear();
            }
            excludeCols.Add("axrnum_forpagination");

            // ✅ Filter final list of columns to include
            var finalColumns = includeCols
                .Where(col => !excludeCols.Contains(col, StringComparer.OrdinalIgnoreCase) && dt.Columns.Contains(col))
                .ToList();

            // ✅ Clone column data types into new table
            foreach (string col in finalColumns)
            {
                DataColumn originalCol = dt.Columns[col];
                var colType = originalCol.DataType;
                if (colType == typeof(Int16) || colType == typeof(Int32) || colType == typeof(Int64) || colType == typeof(int) || colType == typeof(float) || colType == typeof(decimal) || colType == typeof(double))
                    colType = typeof(String);

                DataColumn newCol = new DataColumn(originalCol.ColumnName, colType);
                newCol.ReadOnly = false; // Make all columns writable
                newTable.Columns.Add(newCol);
            }

            // Parse masking rules
            var maskRules = maskedFields
                .Where(r => r.Contains("="))
                .Select(r => r.Split('='))
                .Where(r => r.Length == 2)
                .ToDictionary(r => r[0], r => r[1]);

            // ✅ Copy filtered and masked data to new table
            foreach (DataRow row in dt.Rows)
            {
                DataRow newRow = newTable.NewRow();

                foreach (string col in finalColumns)
                {
                    object value = row[col];

                    if (value != DBNull.Value)
                    {
                        if (maskRules.ContainsKey(col))
                        {
                            newRow[col] = ApplyFieldMask(value.ToString(), maskRules[col]);
                        }
                        else if (dt.Columns[col].DataType == typeof(DateTime))
                        {
                            // Apply datetime formatting
                            newRow[col] = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            newRow[col] = value;
                        }
                    }
                    else
                    {
                        newRow[col] = DBNull.Value;
                    }
                }

                newTable.Rows.Add(newRow);
            }

            return newTable;
        }

        public async Task<Dictionary<string, Dictionary<string, Dictionary<string, string>>>> HandleIncludeExcludeCols(Dictionary<string, Dictionary<string, Dictionary<string, string>>> data,  APIResult accessResult, Dictionary<string, string> FieldWiseDCs)
        {
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            // Extract DC-level settings
            var excludedDCs = accessResult.data.ContainsKey("ExcludedDCs")
                ? accessResult.data["ExcludedDCs"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();
            var includedDCs = accessResult.data.ContainsKey("IncludedDCs")
                ? accessResult.data["IncludedDCs"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            // Extract field-level settings
            var excludedFields = accessResult.data.ContainsKey("ExcludedFields")
                ? accessResult.data["ExcludedFields"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();
            var includedFields = accessResult.data.ContainsKey("IncludedFields")
                ? accessResult.data["IncludedFields"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            var maskedFields = accessResult.data.ContainsKey("MaskedFields")
                ? accessResult.data["MaskedFields"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            //var encryptedFields = accessResult.data.ContainsKey("EncryptedFields")
            //    ? accessResult.data["EncryptedFields"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            //    : new List<string>();

            if (excludedDCs.Contains(none, StringComparer.OrdinalIgnoreCase))
                excludedDCs.Clear();

            if (includedDCs.Contains(all, StringComparer.OrdinalIgnoreCase) && includedFields.Contains(all, StringComparer.OrdinalIgnoreCase))
                includedDCs = data.Keys.ToList();

            if (excludedFields.Contains(none, StringComparer.OrdinalIgnoreCase))
                excludedFields.Clear();

            excludedFields.Add("axrnum_forpagination");

            // Parse masking rules
            var maskRules = maskedFields
                .Where(r => r.Contains("="))
                .Select(r => r.Split('='))
                .Where(r => r.Length == 2)
                .ToDictionary(r => r[0], r => r[1], StringComparer.OrdinalIgnoreCase);

            // Result dictionary
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            // Get DCs from includedFields 
            var fieldBasedDCs = FieldWiseDCs.Where(x => includedFields.Contains(x.Key, StringComparer.OrdinalIgnoreCase) 
            && !includedDCs.Contains(x.Value, StringComparer.OrdinalIgnoreCase)
            ).Select(x => x.Value);

            // Filter DCs
            var allowedDCs = data.Keys
                .Where(dc => (includedDCs.Count == 0 || includedDCs.Contains(dc, StringComparer.OrdinalIgnoreCase)))
                .Union(fieldBasedDCs, StringComparer.OrdinalIgnoreCase)
                .Where(dc => !excludedDCs.Contains(dc, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(dc => int.Parse(dc.ToLower().Replace("dc", "")))
                .ToList();

            // Process each DC individually
            foreach (var dcKey in allowedDCs)
            {
                result[dcKey] = new Dictionary<string, Dictionary<string, string>>();

                // Handle IncludedFields and ExcludedFields inside DC loop
                List<string> finalFields;

                if (includedFields.Contains(all, StringComparer.OrdinalIgnoreCase))
                {
                    // All fields from this DC
                    finalFields = data[dcKey]
                        .SelectMany(row => row.Value.Keys)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else
                {
                    // Only included fields + system fields
                    finalFields = new List<string> {
                        "axp_recid"
                    };

                    if (fieldBasedDCs.Any(dc => dc.Equals(dcKey, StringComparison.OrdinalIgnoreCase)))
                    {
                    finalFields.AddRange(new List<string>(includedFields));
                    }
                    else
                    {
                        // All fields from this DC
                        finalFields = data[dcKey]
                            .SelectMany(row => row.Value.Keys)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                    
                }

                // Apply excluded fields per DC
                finalFields = finalFields
                .Where(f =>
                {
                    //Direct exclusion check
                    if (excludedFields.Contains(f, StringComparer.OrdinalIgnoreCase))
                        return false;

                    //Check if it's a source ID for an excluded field
                    if (f.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase))
                    {
                        var prefix = f.Substring(0, f.Length - "_sourceid".Length);
                        if (excludedFields.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                            return false;
                    }

                    return true;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();  

                // Process each row
                foreach (var row in data[dcKey])
                {
                    var rowKey = row.Key;
                    var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var field in finalFields)
                    {
                        if (row.Value.ContainsKey(field))
                        {
                            if (row.Value.TryGetValue(field, out var value))
                            {
                                ////Encrypted fields handling
                                //if (encryptedFields.Contains(field))
                                //{
                                //    value = AXPEncryptData.CallDecryptData(value);
                                //}

                                ////Date fields handling
                                //if (DateTime.TryParse(value, out var dtValue))
                                //{
                                //    value = dtValue.ToString("dd/MM/yyyy HH:mm:ss");
                                //}

                                //Masked fields handling
                                if (maskRules.ContainsKey(field))
                                {
                                    value = ApplyFieldMask(value, maskRules[field]);
                                }

                                rowData[field] = value;
                            }
                            else
                            {
                                rowData[field] = string.Empty;
                            }
                        }
                    }

                    result[dcKey][rowKey] = rowData;
                }
            }

            return result;
        }

        //public async Task<DataTable> HandleIncludeExcludeCols(DataTable dt, List<string> excludeCols, List<string> includeCols, List<string> maskedFields)
        //{
        //    DataTable newTable = new DataTable();
        //    var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
        //    var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

        //    // ✅ If includeCols = ["all"], include all columns
        //    if (includeCols.Contains(all, StringComparer.OrdinalIgnoreCase))
        //    {
        //        includeCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        //    }
        //    else
        //    {
        //        includeCols.AddRange(new List<string> { "transid", "recordid", "modifiedby", "modifiedon", "createdon", "createdby", "rno", "pageno" });
        //    }

        //    // ✅ If excludeCols = ["none"], no columns are excluded
        //    if (excludeCols.Contains(none, StringComparer.OrdinalIgnoreCase))
        //    {
        //        excludeCols.Clear();
        //    }

        //    // ✅ Filter final list of columns to include
        //    var finalColumns = includeCols
        //        .Where(col => !excludeCols.Contains(col, StringComparer.OrdinalIgnoreCase) && dt.Columns.Contains(col))
        //        .ToList();

        //    // ✅ Add final columns to new table
        //    foreach (string col in finalColumns)
        //    {
        //        newTable.Columns.Add(col);
        //    }

        //    // Parse masking rules
        //    var maskRules = maskedFields
        //        .Where(r => r.Contains("="))
        //        .Select(r => r.Split('='))
        //        .Where(r => r.Length == 2)
        //        .ToDictionary(r => r[0], r => r[1]);

        //    // ✅ Copy filtered data to new table
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        DataRow newRow = newTable.NewRow();

        //        foreach (string col in finalColumns)
        //        {
        //            if (maskRules.ContainsKey(col) && row[col] != DBNull.Value)
        //            {
        //                newRow[col] = ApplyFieldMask(row[col].ToString(), maskRules[col]);
        //            }
        //            else
        //            {
        //                newRow[col] = row[col];
        //            }
        //        }

        //        newTable.Rows.Add(newRow);
        //    }

        //    return newTable;
        //}

        public string ApplyFieldMask(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return input;

            input = input.Trim();
            if (input.Length == 0)
                return input;

            // Case 1: Simple digit pattern (e.g., "999")
            if (pattern.All(c => c == '9'))
            {
                int showLength = Math.Min(pattern.Length, input.Length);
                return input.Substring(0, showLength) +
                       new string('.', Math.Max(0, input.Length - showLength));
            }

            // Case 2: Pattern with range separator (.. or ** or any repeated non-digit)
            var separator = GetRepeatedNonDigitSeparator(pattern);
            if (separator != null)
            {
                string[] parts = pattern.Split(new[] { separator }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    int prefixLength = parts[0].Count(c => c == '9');
                    int suffixLength = parts[1].Count(c => c == '9');

                    if (prefixLength + suffixLength > input.Length)
                        return input; // Pattern longer than input

                    char maskChar = separator[0]; // Use the first char of separator as mask character

                    return input.Substring(0, prefixLength) +
                           new string(maskChar, input.Length - prefixLength - suffixLength) +
                           input.Substring(input.Length - suffixLength);
                }
            }

            // Case 3: Prefix pattern (e.g., "99***")
            var prefixMatch = System.Text.RegularExpressions.Regex.Match(pattern, @"^(9+)([^9]+)$");
            if (prefixMatch.Success)
            {
                int showLength = prefixMatch.Groups[1].Length;
                char maskChar = prefixMatch.Groups[2].Value[0];
                return input.Substring(0, Math.Min(showLength, input.Length)) +
                       new string(maskChar, Math.Max(0, input.Length - showLength));
            }

            // Case 4: Suffix pattern (e.g., "***99")
            var suffixMatch = System.Text.RegularExpressions.Regex.Match(pattern, @"^([^9]+)(9+)$");
            if (suffixMatch.Success)
            {
                int showLength = suffixMatch.Groups[2].Length;
                char maskChar = suffixMatch.Groups[1].Value[0];
                return new string(maskChar, Math.Max(0, input.Length - showLength)) +
                       input.Substring(Math.Max(0, input.Length - showLength));
            }

            // Case 5: Mixed pattern with custom mask characters (e.g., "99XX99")
            if (pattern.Any(c => c != '9'))
            {
                StringBuilder result = new StringBuilder();
                int inputPos = 0;

                foreach (char c in pattern)
                {
                    if (inputPos >= input.Length)
                        break;

                    if (c == '9')
                    {
                        result.Append(input[inputPos++]);
                    }
                    else
                    {
                        result.Append(c);
                        inputPos++;
                    }
                }

                // If we have remaining characters, mask them with the last mask character found
                if (inputPos < input.Length && pattern.Any(p => p != '9'))
                {
                    char lastMaskChar = pattern.Last(p => p != '9');
                    result.Append(new string(lastMaskChar, input.Length - inputPos));
                }

                return result.ToString();
            }

            // Default: return original input if no pattern matched
            return input;
        }

        private string GetRepeatedNonDigitSeparator(string pattern)
        {
            for (int i = 0; i < pattern.Length - 1; i++)
            {
                if (!char.IsDigit(pattern[i]) && pattern[i] == pattern[i + 1])
                {
                    int start = i;
                    while (i < pattern.Length && pattern[i] == pattern[start])
                        i++;
                    return pattern.Substring(start, i - start);
                }
            }
            return null;
        }

        public static SecurityKey LoadKeyFromJwksFile(string jwksFilePath, string expectedKid)
        {
            var jwksJson = File.ReadAllText(jwksFilePath);
            var jwks = JsonDocument.Parse(jwksJson);
            var keys = jwks.RootElement.GetProperty("keys");

            foreach (var key in keys.EnumerateArray())
            {
                var kid = key.GetProperty("kid").GetString();
                if (kid == expectedKid)
                {
                    var n = Base64UrlEncoder.DecodeBytes(key.GetProperty("n").GetString());
                    var e = Base64UrlEncoder.DecodeBytes(key.GetProperty("e").GetString());

                    var rsaParameters = new RSAParameters
                    {
                        Modulus = n,
                        Exponent = e
                    };

                    var rsa = RSA.Create();
                    rsa.ImportParameters(rsaParameters);

                    return new RsaSecurityKey(rsa)
                    {
                        KeyId = kid
                    };
                }
            }

            throw new Exception("Key not found in JWKS.");
        }

        private string XmltoJSON(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc);

            var parsed = JObject.Parse(json);

            // Remove attributes starting with '@'
            foreach (var attr in parsed.First.First.Children<JProperty>().Where(p => p.Name.StartsWith("@")).ToList())
            {
                attr.Remove();
            }

            var jtoken = ((JContainer)parsed.First).First;
            return jtoken.ToString();
        }

        public async Task<string> GetXMLFromUrl(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode(); // Throws exception if not successful
                    return await response.Content.ReadAsStringAsync();
                }
            
             }
            catch (Exception ex)
            {
                throw new Exception("Not able to load url-" + url);
            }
        }

        public AppConnection GetAppConnection()
        {
            var conn = _appSettingsHelper.GetAppConnection(_appName);
            //_schemaName = conn.DbUser.Replace("\\", "~");
            if (string.IsNullOrEmpty(_schemaName))
            {
                Dictionary<string, string> config = GetDBConfigurationSync(_appName);
                _schemaName = config["DBUser"];
                _schemaNameALC = config["DBUserALC"];

                if (string.IsNullOrEmpty(_dbType) || _dbVars == null || _dbVars.Count == 0)
                {
                    _dbType = config["DBType"];
                    _dbVars = GetDBVarsList(_dbType);
                }
            }
            return conn;
        }

        public string GetSchemaName()
        {
            if (string.IsNullOrEmpty(_schemaName))
            {
                //var conn = _appSettingsHelper.GetAppConnection(_appName);
                //_schemaName = conn.DbUser.Replace("\\", "~");

                Dictionary<string, string> config = GetDBConfigurationSync(_appName);                
                _schemaName = config["DBUser"];
                _schemaNameALC = config["DBUserALC"];

                if (string.IsNullOrEmpty(_dbType) || _dbVars == null || _dbVars.Count == 0)
                {
                    _dbType = config["DBType"];
                    _dbVars = GetDBVarsList(_dbType);
                }
            }
            return _schemaName;
        }

        public async Task<string> GetAxApps(string appName)
        {
            //Get from redis
            var key = $"ARM-{appName}-AxAppsXML";
            var axApps = _redis.StringGet(key);
            if (!string.IsNullOrEmpty(axApps))
                return axApps;

            string url = GetARMScriptsURL(appName);
            if (string.IsNullOrEmpty(url) || url == "App not found")
            {
                return "Error: ARM Scripts URL is not configured in AgileConnect. Please check with administrator.";
            }

            axApps = await GetXMLFromUrl(url + "//axapps.xml");
            if (!string.IsNullOrEmpty(axApps))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(axApps);
                XmlNodeList pNode = doc.SelectNodes("/connections/" + appName);
                var xmlcontents = pNode[0].OuterXml;

                await _redis.StringSetAsync(key, xmlcontents);

                return xmlcontents;
            }
            else
            {
                return "Error: AxApps.xml is not available in scripts URL.";
            }
        }

        public async Task<string> GetSchemaName(string appName, string axAppsXML)
        {
            var appsJson = XmltoJSON(axAppsXML);
            JObject appsJObj = JObject.Parse(appsJson);
            var schemaName = "";
            if (appsJObj != null)
            {
                schemaName = appsJObj["dbuser"].ToString();
                schemaName = schemaName.Replace("\\", "~");
            }
            _schemaName = schemaName;
            return schemaName;
        }

        public async Task<string> GetAxProps(string appName)
        {
            try
            {
                //Get from redis
                var key = $"ARM-{appName}-AxPropsXML";
                var axProps = "";
                var redisConnected = await OpenRedisConnectionAsync();
                if (redisConnected)
                    axProps = _redisCache.StringGet(key);

                if (!string.IsNullOrEmpty(axProps))
                    return axProps;

                string url = GetARMScriptsURL(appName);
                if (string.IsNullOrEmpty(url) || url == "App not found")
                {
                    return "Error: ARM Scripts URL is not configured in AgileConnect. Please check with administrator.";
                }

                axProps = await GetXMLFromUrl(url + "//axprops.xml");
                if (!string.IsNullOrEmpty(axProps))
                {
                    await _redisCache.StringSetAsync(key, axProps);
                    return axProps;
                }
                else
                {
                    return "Error: AxProps.xml is not available in scripts URL.";
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //public async Task<T> DataRowMapper<T>(DataRow row)
        //{
        //    var dict = row.Table.Columns
        //        .Cast<DataColumn>()
        //        .ToDictionary(c => c.ColumnName.ToLowerInvariant(), c => row[c]);

        //    var json = System.Text.Json.JsonSerializer.Serialize(dict);

        //    var options = new System.Text.Json.JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
        //}

        public async Task<T> DataRowMapper<T>(DataRow row)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn col in row.Table.Columns)
            {
                var colName = col.ColumnName.ToLowerInvariant();
                var val = row[col];

                if (val == DBNull.Value || val == null)
                {
                    dict[colName] = null;
                }
                else
                {
                    dict[colName] = val;
                }
            }

            var json = System.Text.Json.JsonSerializer.Serialize(dict);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
            if (obj == null) return default!;

            // Check if T has a property named "SSOKeys" of type Dictionary<string, string>
            var ssoProp = typeof(T).GetProperty("SSOKeys");
            if (ssoProp != null && ssoProp.PropertyType == typeof(Dictionary<string, string>))
            {
                var ssoDict = (Dictionary<string, string>)ssoProp.GetValue(obj)! ?? new();

                foreach (DataColumn col in row.Table.Columns)
                {
                    string colName = col.ColumnName.ToLower();
                    if (colName.EndsWith("singleloginkey", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = row[col]?.ToString();
                        if (!string.IsNullOrEmpty(colName))
                        {
                            ssoDict[colName] = value;
                        }
                    }
                }

                ssoProp.SetValue(obj, ssoDict);
            }

            return obj;
        }

        public async Task<ScalarResult> InsertIntoAxpertLog(AxpertLog axpertLog, bool rollbackActiveTransactions = true)
        {
            try
            {
                if (_dbHelper == null)
                    await OpenDBConnectionAsync();

                if(rollbackActiveTransactions)
                    RollbackActiveTransactions();

                string sql = Constants_SQL.INSERT_INTO_AXPERTLOG;
                sql = sql.Replace("$STARTTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@starttime"));
                sql = sql.Replace("$ENDTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@endtime"));

                axpertLog.CallFinished = DateTime.Now;
                TimeSpan timeTaken = (TimeSpan)(axpertLog.CallFinished - axpertLog.CalledOn);
                axpertLog.TimeTaken = Convert.ToInt32(timeTaken.TotalMilliseconds);

                axpertLog.DbTimeTaken = Convert.ToInt32(_dbHelper.GetDBTimeTaken());
                axpertLog.DbConnTime = Convert.ToInt32(_dbHelper.GetDBConnectionTime());

                string[] paramName = { "@sessionid", "@username", "@starttime", "@endtime", "@structname", "@recordid", "@servicename", "@result", "@timetaken", "@dbtimetaken", "@dbconntime" , "@calldetails" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.Int64, DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.Int32, DbType.String };
                object[] paramValue = { axpertLog.SessionId ?? "", axpertLog.Username ?? "", axpertLog.CalledOn?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", axpertLog.CallFinished?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", axpertLog.StructName ?? "", axpertLog.RecordId ?? 0, axpertLog.ServiceName ?? "", axpertLog.ServiceResult ?? "", axpertLog.TimeTaken ?? 0, axpertLog.DbTimeTaken ?? 0, axpertLog.DbConnTime ?? 0, axpertLog.CallDetails ?? "" };

                return await _dbHelper.ExecuteScalarAsync(sql, paramName, paramType, paramValue);
            }
            catch { }
            return null;
        }

        public async Task<ScalarResult> LogSigninIntoAxAudit(AxAudit axAudit, bool rollbackActiveTransactions = true)
        {
            try
            {
                if (_dbHelper == null)
                    await OpenDBConnectionAsync();

                if (rollbackActiveTransactions)
                    RollbackActiveTransactions();

                string sql = Constants_SQL.INSERT_INTO_AXAUDIT;
                sql = sql.Replace("$LOGINTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@logintime"));
                sql = sql.Replace("$SERVERTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@servertime"));

                if (axAudit.Status == "Failed")
                {
                    sql = sql.Replace("$LOGOUTTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@logouttime"));
                    axAudit.NoLogout = "F";
                    axAudit.LogoutTime = DateTime.Now;
                }
                else
                {
                    sql = sql.Replace("$LOGOUTTIME$", "@logouttime");
                    axAudit.NoLogout = "T";
                    axAudit.LogoutTime = null;
                }

                string[] paramName = { "@sessionid", "@username", "@logintime", "@status", "@reason", "@ip", "@other", "@logouttime", "@nologout", "@servertime", };

                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.DateTime, DbType.String, DbType.String };

                object[] paramValue = { axAudit.SessionId ?? "", axAudit.Username ?? "", axAudit.LoginTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", axAudit.Status ?? "", axAudit.Reason ?? "", axAudit.IP ?? "", axAudit.Other ?? "", axAudit.LogoutTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", axAudit.NoLogout ?? "", axAudit.ServerTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "" };

                return await _dbHelper.ExecuteScalarAsync(sql, paramName, paramType, paramValue);
            }
            catch{}
            return null;
        }

        public async Task<ScalarResult> LogSignOutIntoAxAudit(AxAudit axAudit, bool rollbackActiveTransactions = true)
        {
            if (_dbHelper == null)
                await OpenDBConnectionAsync();

            if (rollbackActiveTransactions)
                RollbackActiveTransactions();

            string sql = Constants_SQL.UPDATE_AXAUDIT;
            sql = sql.Replace("$LOGOUTTIME$", _dbVars["dbdatestring"].Replace("$DATE$", "@logouttime"));

            string[] paramName = { "@logouttime", "@sessionid"};
            DbType[] paramType = { DbType.String, DbType.String};
            object[] paramValue = { axAudit.LogoutTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", axAudit.SessionId ?? ""};

            return await _dbHelper.ExecuteScalarAsync(sql, paramName, paramType, paramValue);
        }


        public string? GetPushToQueueUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return null;

            return $"{request.Scheme}://{request.Host}{request.PathBase}/api/v1/ARMPushToQueue";
        }

        public async Task<string> GetAxpertProperties(string appName, string type)
        {
            string result = "";
            if (type.ToUpper() == Constants.AXPERT_PROPERTIES.EMAIL.ToString())
            {
            //    var redisConnected = await OpenRedisConnectionAsync();
            //    if (redisConnected)
            //    {
            //        var keyParams = new string[] { appName, Constants.AXEMAILSMTP_CONN_KEY };
            //        var cacheKey = GenerateRedisKeyString(keyParams);
            //        var emailConfigData = await _redisCache.StringGetBinaryAsync(cacheKey);
            //        if (!string.IsNullOrEmpty(emailConfigData))
            //        {
            //            return emailConfigData;
            //        }
            //    }

                var emailOtpSettings = new EmailSettings();
                try
                {
                    emailOtpSettings = _appSettingsHelper.GetEmailSettings(appName);                    
                }
                catch{}

                emailOtpSettings.EmailBody = emailOtpSettings.EmailBody?.Replace("\\r\\n", "<br>") ?? "";
                emailOtpSettings.EmailBody = emailOtpSettings.EmailBody.Replace("\r\n", "<br>");
                emailOtpSettings.EmailBody = emailOtpSettings.EmailBody.Replace("\\n", "<br>");
                emailOtpSettings.EmailBody = emailOtpSettings.EmailBody.Replace("\n", "<br>");

                emailOtpSettings.SmsContent = emailOtpSettings.SmsContent?.Replace("\\r\\n", "\\n") ?? "";
                emailOtpSettings.SmsContent = emailOtpSettings.SmsContent.Replace("\r\n", "\\n");

                var emailProps = new Dictionary<string, object>
                    {
                        { "host", emailOtpSettings.Host ?? "" },
                        { "port", emailOtpSettings.Port.ToString() ?? "" },
                        { "user", emailOtpSettings.User ?? "" },
                        { "pwd", emailOtpSettings.Pwd  ?? ""},
                        { "arm_url", emailOtpSettings.Arm_Url ?? "" },
                        { "emailsubject", emailOtpSettings.EmailSubject ?? "" },
                        { "emailbody", emailOtpSettings.EmailBody },
                        { "smscontent", emailOtpSettings.SmsContent },
                        { "AxOTPAuth", (emailOtpSettings.AxOTPAuth?.ToString() == "T" || emailOtpSettings.AxOTPAuth?.ToString().ToLower() == "true") ? "true" : "false"},
                        { "AxOTPAuthCahrs", emailOtpSettings.AxOTPAuthCahrs?.ToString() ?? "4"},
                        { "AxOTPAuthExpiry", emailOtpSettings.AxOTPAuthExpiry?.Split(" ")?[0]?.ToString() ?? "1"},
                    };

                return JsonConvert.SerializeObject(emailProps);

            }
            else if (type.ToUpper() == Constants.AXPERT_PROPERTIES.PASSWORD.ToString())
            {
                var redisConnected = await OpenRedisConnectionAsync();
                if (redisConnected)
                {
                    var keyParams = new string[] { appName, Constants.AXPASSWORDPOL_CONN_KEY };
                    var cacheKey = GenerateRedisKeyString(keyParams);
                    var pwdData = await _redisCache.StringGetBinaryAsync(cacheKey);
                    if (!string.IsNullOrEmpty(pwdData))
                    {
                        return pwdData;
                    }
                }


                var propsResult = await GetAxpertPropertiesFromDB();                
                if (propsResult != null && string.IsNullOrEmpty(propsResult.error) && propsResult.data != null && propsResult.data.Rows.Count > 0)
                {
                    DataRow dr = propsResult.data.Rows[0];

                    var axpertProperties = await DataRowMapper<AxpertProperties>(propsResult.data.Rows[0]);

                    //axpertProperties.EmailBody = axpertProperties.EmailBody?.Replace("\\r\\n", "<br>") ?? "";
                    //axpertProperties.EmailBody = axpertProperties.EmailBody.Replace("\r\n", "<br>");
                    //axpertProperties.EmailBody = axpertProperties.EmailBody.Replace("\\n", "<br>");
                    //axpertProperties.EmailBody = axpertProperties.EmailBody.Replace("\n", "<br>");

                    //axpertProperties.SmsContent = axpertProperties.SmsContent?.Replace("\\r\\n", "\\n") ?? "";
                    //axpertProperties.SmsContent = axpertProperties.SmsContent.Replace("\r\n", "\\n");

                    //var emailProps = new Dictionary<string, object>
                    //    {
                    //        { "host", axpertProperties.SmtpHost ?? "" },
                    //        { "port", axpertProperties.SmtpPort ?.ToString() ?? "" },
                    //        { "user", axpertProperties.SmtpUser ?? "" },
                    //        { "pwd", axpertProperties.SmtpPwd  ?? ""},
                    //        { "arm_url", GetPushToQueueUrl() ?? "" },
                    //        { "emailsubject", axpertProperties.EmailSubject ?? "" },
                    //        { "emailbody", axpertProperties.EmailBody },
                    //        { "smscontent", axpertProperties.SmsContent },
                    //        { "AxOTPAuth", (axpertProperties.OtpAuth?.ToString() == "T") ? "true" : "false"},
                    //        { "AxOTPAuthCahrs", axpertProperties.OtpChars?.ToString() ?? "4"},
                    //        { "AxOTPAuthExpiry", axpertProperties.OtpExpiry?.Split(" ")?[0]?.ToString() ?? "1"},
                    //    };

                    //var emailKeyParams = new string[] { appName, Constants.AXEMAILSMTP_CONN_KEY };
                    //var emailCacheKey = GenerateRedisKeyString(emailKeyParams);

                    //string emailData = JsonConvert.SerializeObject(emailProps);
                    //_redis.AppName = appName;
                    //await _redis.StringSetBinaryAsync(emailCacheKey, emailData);


                    var pwdProps = new Dictionary<string, object>
                    {
                        { "pwdminchar", axpertProperties.PwdMinChar ?.ToString() ?? "" },
                        { "pwdmaxchar", axpertProperties.PwdMaxChar ?.ToString() ?? "" },
                        { "pwdalphanum", axpertProperties.PwdAlphaNum ?.ToString() ?? "" },
                        { "pwdcapchar", axpertProperties.PwdCapChar ?.ToString() ?? "" },
                        { "pwdsmallchar", axpertProperties.PwdSmallChar ?.ToString() ?? "" },
                        { "pwdnumchar", axpertProperties.PwdNumChar ?.ToString() ?? ""},
                        { "pwdsplchar",axpertProperties.PwdSplChar ?.ToString() ?? ""},
                        { "pwdaes",axpertProperties.PwdEncrypt ?.ToString() ?? ""}
                    };


                    var pwdKeyParams = new string[] { appName, Constants.AXPASSWORDPOL_CONN_KEY };
                    var pwdCacheKey = GenerateRedisKeyString(pwdKeyParams);

                    string pwdData = JsonConvert.SerializeObject(pwdProps);
                    await _redisCache.StringSetBinaryAsync(pwdCacheKey, pwdData);

                    return pwdData;

                }

                return result;
            }
            return result;
        }

        private async Task<SQLResult> GetAxpertPropertiesFromDB()
        {
            if (_dbHelper == null)
                await OpenDBConnectionAsync();

            return await _dbHelper.ExecuteSQLAsync(Constants_SQL.GET_AXPERT_PROPERTIES);
        }

        #region OTP Settings
        public string GenerateOTP(string otpChars = "4", bool isNumeric = true) 
        {
            string otp = string.Empty;
            if (isNumeric)
            {
                const string digits = "0123456789";
                int otpLength = int.Parse(otpChars);
                var otpBuilder = new StringBuilder();
                using (var rng = RandomNumberGenerator.Create())
                {
                    byte[] buffer = new byte[1];
                    while (otpBuilder.Length < otpLength)
                    {
                        rng.GetBytes(buffer);
                        int randomValue = buffer[0] % digits.Length;
                        if (buffer[0] < digits.Length * (256 / digits.Length))
                        {
                            otpBuilder.Append(digits[randomValue]);
                        }
                    }
                }
                otp = otpBuilder.ToString();
            }
            else
            {
                const string digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                int otpLength = int.Parse(otpChars);
                var otpBuilder = new StringBuilder();
                using (var rng = RandomNumberGenerator.Create())
                {
                    byte[] buffer = new byte[1];
                    while (otpBuilder.Length < otpLength)
                    {
                        rng.GetBytes(buffer);
                        int randomValue = buffer[0] % digits.Length;
                        if (buffer[0] < digits.Length * (256 / digits.Length))
                        {
                            otpBuilder.Append(digits[randomValue]);
                        }
                    }
                }
                otp = otpBuilder.ToString();
            }

            return otp;
        }


        public string MaskEmail(string email)
        {
            if (email != string.Empty)
            {
                int atIndex = email.IndexOf('@');
                if (atIndex > 0)
                {
                    string localPart = email.Substring(0, atIndex);
                    string domainPart = email.Substring(atIndex);

                    if (localPart.Length > 2)
                    {
                        string maskedLocalPart = localPart.Substring(0, 2) + new string('*', localPart.Length - 2);
                        return maskedLocalPart + domainPart;
                    }
                }
                return email;
            }
            else
                return string.Empty;
        }
        public string MaskMobileNumber(string mobileNo)
        {
            if (mobileNo != string.Empty)
            {
                int lastFourStartIndex = mobileNo.Length - 4;
                string maskedPart = new string('*', lastFourStartIndex - 3);
                return mobileNo.Substring(0, 3) + maskedPart + mobileNo.Substring(lastFourStartIndex);
            }
            else
                return string.Empty;
        }

        public async Task<bool> StoreOTPInCache(string sessionId, string otp, int expiry)
        {
            var redisConnected = await OpenRedisConnectionAsync(sessionId);
            if (redisConnected)
                return await _redisCache.StringSetAsync($"{sessionId}-OTP", otp, expiry);
            else
                return false;


        }
        public async Task<string> GetOTPFromCache(string sessionId)
        {
            var redisConnected = await OpenRedisConnectionAsync(sessionId);
            if (redisConnected)
                return await _redisCache.StringGetAsync($"{sessionId}-OTP");
            else
                return string.Empty;
        }

        public string GetServerIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }

            return "127.0.0.1"; // fallback
        }

        public async Task<Dictionary<string, string>> StoreSessionValuesOnSign(string sessionId, Dictionary<string, string> sessionValues)
        {
            if (sessionValues.Count > 0)
            {
                var expiryMins = _appSettingsHelper.GetARMTokenExpiryMinutes(_appName);

                var hashEntries = sessionValues.Select(pair => new HashEntry(pair.Key, pair.Value)).ToArray();
                var redisConnected = await OpenRedisConnectionAsync();
                if (redisConnected)               
                    await _redisCache.HashSetEntriesAsync(sessionId, hashEntries, expiryMins * 60);

                return sessionValues;
            }
            return sessionValues;
        }

        #endregion

        public DataTable ConvertNumericColumnsToString(DataTable originalTable)
        {
            DataTable newTable = new DataTable();

            // Step 1: Clone structure with adjusted column types
            foreach (DataColumn col in originalTable.Columns)
            {
                Type newColType = col.DataType;

                if (col.DataType == typeof(int) ||
                    col.DataType == typeof(Int16) ||
                    col.DataType == typeof(Int32) ||
                    col.DataType == typeof(Int64) ||
                    col.DataType == typeof(long) ||
                    col.DataType == typeof(float) ||
                    col.DataType == typeof(double) ||
                    col.DataType == typeof(decimal))
                {
                    newColType = typeof(string);
                }

                newTable.Columns.Add(col.ColumnName, newColType);
            }

            // Step 2: Copy and convert data row by row
            foreach (DataRow row in originalTable.Rows)
            {
                DataRow newRow = newTable.NewRow();

                foreach (DataColumn col in originalTable.Columns)
                {
                    object value = row[col];

                    if (value == DBNull.Value)
                    {
                        newRow[col.ColumnName] = DBNull.Value;
                        continue;
                    }

                    newRow[col.ColumnName] = value.ToString();
                }

                newTable.Rows.Add(newRow);
            }

            return newTable;
        }

        public async Task<string> GetPreviousAxpertLoggedUserInfo(string schemaName, string userName)
        {
            var key = $"{schemaName}-{Constants.AXLOGGEDUSER}";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
                return await _redisCache.HashGetBinaryAsync(key, userName);
            else 
                return string.Empty;
        }
        public async Task<string> GetPreviousARMLoggedUserInfo(string schemaName, string userName)
        {
            var key = $"{schemaName}-{Constants.AXLOGGEDUSER}-ARM";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
                return await _redisCache.HashGetAsync(key, userName);
            else
                return string.Empty;
        }

        public async Task<bool> DeletePreviousLoggedUserInfo(string schemaName, string userName)
        {
            var key = $"{schemaName}-{Constants.AXLOGGEDUSER}";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
            {
                await _redisCache.HashDeleteAsync(key, userName);
                return await DeletePreviousARMLoggedUserInfo(schemaName, userName);
            }
            else
                return false;

        }

        public async Task<bool> DeletePreviousARMLoggedUserInfo(string schemaName, string userName)
        {
            var previousARMSession = await GetPreviousARMLoggedUserInfo(schemaName, userName);
            if (!string.IsNullOrEmpty(previousARMSession))
            {
                var redisConnected = await OpenRedisConnectionAsync();
                if (redisConnected)
                {
                    await _redisCache.KeyDeleteAsync(previousARMSession);
                    await _redisCache.DeleteKeysByPrefixAsync(previousARMSession);
                    await _redisCache.KeyDeleteAsync("AXPERT-" + previousARMSession);

                    var key = $"{schemaName}-{Constants.AXLOGGEDUSER}-ARM";
                    await _redisCache.HashDeleteAsync(key, userName);
                }
            }

            return true;
        }

        public async Task<bool> SetARMLoggedUserInfo(string schemaName, string userName, string value)
        {
            var key = $"{schemaName}-{Constants.AXLOGGEDUSER}-ARM";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
                return await _redisCache.HashSetAsync(key, userName, value);
            else
                return false;
        }

        public async Task<bool> SetAxpertLoggedUserInfo(string schemaName, string userName, string value)
        {
            var key = $"{schemaName}-{Constants.AXLOGGEDUSER}";
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
                return await _redisCache.HashSetBinaryAsync(key, userName, value);
            else
                return false;        
        }
        public Dictionary<string, string> ValidateSQLAndExtractParams(string sqlText, string parameters, Dictionary<string, string> globalParams = null, Dictionary<string, string> sqlParams = null)
        {
            var matchedParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(parameters)) {
                if ((globalParams != null && globalParams.Count > 0) || (sqlParams != null && sqlParams.Count > 0))
                {                    
                    var allParams = parameters.Split(",").Distinct().ToList();

                    //Validate each SQL param exists in ParamsList
                    foreach (var param in allParams)
                    {
                        string key = param.TrimStart(':');
                        if (globalParams != null && globalParams.ContainsKey(key))
                        {
                            matchedParams[key] = globalParams[key];
                        }
                        else if (sqlParams != null && sqlParams.ContainsKey(key))
                        {
                            matchedParams[key] = sqlParams[key];
                        }
                        else 
                        {
                            throw new Exception($"Parameter '{param}' is missing in input params.");
                        }
                    }
                }
            }

            return matchedParams;
        }
        public Dictionary<string, string> ValidateSQLAndExtractParams(string sqlText, List<string> paramList = null)
        {
            var foundParams = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(sqlText))
                return foundParams;

            // Regex: match :paramName or @paramName (alphanumeric + underscore)
            var regex = new Regex(@"[:@](\w+)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(sqlText);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value; // param name without :/@

                if (!foundParams.ContainsKey(key))
                    foundParams[key] = match.Value; // keep original with prefix
            }

            // If paramList is provided, validate that all params are present
            if (paramList != null && paramList.Any())
            {
                foreach (var param in paramList)
                {
                    var cleanKey = param.TrimStart(':', '@');
                    if (!foundParams.ContainsKey(cleanKey))
                        throw new Exception($"Parameter '{param}' not found in SQL text.");
                }
            }

            return foundParams;
        }

        public async Task<(string sqlText, string[] paramNames, DbType[] paramTypes, object[] paramValues)> GetSQLParams(string sqlText, Dictionary<string, string> paramsList)
        {
            var paramNames = new List<string>();
            var paramTypes = new List<DbType>();
            var paramValues = new List<object>();
            var modifiedSqlText = sqlText;

            if (paramsList != null && paramsList.Count > 0)
            {
                foreach (var param in paramsList)
                {
                    string paramName = param.Key;
                    string paramValue = param.Value;
                    
                    // Get global variable type for this parameter
                    string globalVarType = await GetGlobalVariableType(paramName);
                    
                    if (globalVarType == "n")
                    {
                        paramTypes.Add(DbType.Int64);
                        paramValues.Add(Convert.ToInt64(paramValue));
                    }
                    else if (globalVarType == "d")
                    {
                        // Date type - replace in SQL text using regex for whole word match
                        string pattern = $@"\B:{paramName}\b";
                        if (_dbVars.ContainsKey("dbdatestring"))
                        {
                            string replacement = _dbVars["dbdatestring"].Replace("$DATE$", " :" + paramName + " ");
                            modifiedSqlText = Regex.Replace(modifiedSqlText, pattern, replacement);
                        }
                                                
                        paramTypes.Add(DbType.String);
                        paramValues.Add(paramValue);
                    }
                    else
                    {
                        paramTypes.Add(DbType.String);
                        paramValues.Add(paramValue);
                    }
                    
                    paramNames.Add("@" + paramName);
                }
            }

            return (modifiedSqlText, paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());
        }

        public (string sqlText, string[] paramNames, DbType[] paramTypes, object[] paramValues) GetSQLParams(string sqlText, List<string> paramsList = null, List<string> paramsValuesList = null, List<string> paramsTypeList = null)
        {
            var foundParams = new List<string>();

            // Regex: match :paramName or @paramName (alphanumeric + underscore)
            var regex = new Regex(@"[:@](\w+)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(sqlText);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value; // param name without :/@

                if (!foundParams.Contains(key, StringComparer.OrdinalIgnoreCase))
                    foundParams.Add(key);
            }

            var paramNames = new List<string>();
            var paramTypes = new List<DbType>();
            var paramValues = new List<object>();
            var modifiedSqlText = sqlText;

            if (!foundParams.Any())
                return (modifiedSqlText, paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());
            
            if (foundParams != null && paramsList != null && paramsValuesList != null && paramsTypeList != null)
            {
                if (paramsList.Count != paramsValuesList.Count || paramsList.Count != paramsTypeList.Count)
                    throw new ArgumentException("All parameter lists must have the same length.");

                foreach(var foundParam in foundParams)
                {
                    var paramIndex = paramsList.FindIndex(s => string.Equals(s, foundParam, StringComparison.OrdinalIgnoreCase));
                    if (paramIndex > -1)
                    {
                        string paramName = foundParam;
                        string paramValue = paramsValuesList[paramIndex];
                        string globalVarType = paramsTypeList[paramIndex]?.ToLowerInvariant();

                        switch (globalVarType)
                        {
                            case "n": // number
                                paramTypes.Add(DbType.Int64);
                                paramValues.Add(Convert.ToInt64(paramValue));
                                break;

                            case "d": // date
                                string pattern = $@"\B:{paramName}\b";
                                if (_dbVars.ContainsKey("dbdatestring"))
                                {
                                    string replacement = _dbVars["dbdatestring"].Replace("$DATE$", " :" + paramName + " ");
                                    modifiedSqlText = Regex.Replace(modifiedSqlText, pattern, replacement);
                                }

                                paramTypes.Add(DbType.String); // or DbType.Date depending on DB
                                paramValues.Add(paramValue);
                                break;

                            default: // string
                                paramTypes.Add(DbType.String);
                                paramValues.Add(paramValue);
                                break;
                        }

                        paramNames.Add("@" + paramName);
                    }
                }
            }

            return (modifiedSqlText, paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());
        }

        public async Task<bool>  OpenRedisConnectionAsync(string cacheKey = "")
        {
            bool result = false;
            try
            {
                if (_redisCache == null)
                    _redisCache = new RedisCacheHelper();

                if (_redisCache.Redis == null || !_redisCache.Redis.IsConnected)
                {
                    if (string.IsNullOrEmpty(_appName) && !string.IsNullOrEmpty(cacheKey))
                        _appName = GetAppNameFromCacheKey(cacheKey);

                    _redisCache.AppName = _appName;
                    _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(_appName);                    
                    _redisCache.Redis = await _redisCache.OpenConnectionAsync();

                    if (string.IsNullOrEmpty(_dbType) || _dbVars == null || _dbVars.Count == 0)
                    {
                        Dictionary<string, string> config = await GetDBConfigurations(_appName);
                        _dbType = config["DBType"];
                        _dbVars = GetDBVarsList(_dbType);
                    }
                }

                if (_redisCache.Redis.IsConnected)
                    return true;
            }
            catch(Exception ex)
            {
                if (_traceService != null && _traceService.IsTracingEnabled()) _traceService.LogMessage($"Error in Opening Redis Connection for application '{_appName}'. Error: {ex.Message}");
                return false;
            }

            return result;
        }

        public async Task<bool> CloseRedisConnectionAsync()
        {
            try
            {
                if (_redisCache != null)
                {
                    return await _redisCache.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public async Task<string> OpenDBConnectionAsync()
        {
            try
            {
                if (_traceService.IsTracingEnabled()) _traceService.LogMessage($"Initiating DB connection for application '{_appName}'");
                
                var _axPropsXML = await GetAxProps(_appName);

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"Reading AxProps XML: {_axPropsXML}");
                }

                Dictionary<string, string> config = await GetDBConfigurations(_appName);
                if(config == null)
                {
                    return $"Error: DB Connection details not available in Appsettings for accesscode {_appName}";
                }

                string connectionString = config["ConnectionString"];
                _dbType = config["DBType"];

                var dbHelper = new DBHelper();
                _dbHelper = dbHelper.GetDbHelper(_dbType, _traceService);
                _dbHelper.ConnectionString = connectionString;                
                await _dbHelper.OpenDBConnectionAsync();

                if(_dbVars == null || _dbVars.Count == 0)
                    _dbVars = GetDBVarsList(_dbType);

                string _dbUserName;
                string _dbName;
                _schemaName = config["DBUser"];
                _schemaNameALC = config["DBUserALC"];

                if (_schemaName.Split('~').Length == 2)
                {
                    _dbUserName = _schemaName.Split('~')[0];
                    _dbName = _schemaName.Split('~')[1];
                }
                else
                {
                    _dbUserName = _schemaName;
                    _dbName = _schemaName;
                }

                if (_dbHelper.GetDBType() == Constants.DBTYPE.POSTGRE.ToString())
                {
                    string searchPath = "";

                    if (!string.IsNullOrEmpty(_dbUserName))
                    {
                        if (!_dbVars.ContainsKey("postgre_search_path"))
                        {
                            string postgreSearchPath = "";
                            try
                            {
                                XDocument doc = XDocument.Parse(_axPropsXML);
                                XElement postgreSearchPathElement = doc.Root.Element(_dbUserName);

                                if (postgreSearchPathElement != null)
                                {
                                    postgreSearchPath = postgreSearchPathElement.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                //Do nothing
                            }

                            _dbVars.Add("postgre_search_path", postgreSearchPath);
                        }
                    }

                    if (_dbVars.ContainsKey("postgre_search_path") && !string.IsNullOrEmpty(_dbVars["postgre_search_path"]))
                    {
                        if (config.ContainsKey("Driver") && config["Driver"] == "ado")
                        {
                            searchPath = $"{_dbVars["postgre_search_path"]},public";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(_dbUserName))
                                searchPath = $"{_dbUserName},{_dbVars["postgre_search_path"]},public";
                            else if (!string.IsNullOrEmpty(_dbName))
                                searchPath = $"{_dbUserName},{_dbName},{_dbVars["postgre_search_path"]},public";
                            else
                                searchPath = $"{_dbUserName},{_dbVars["postgre_search_path"]},public";
                        }

                        await _dbHelper.ExecuteSQLAsync(Constants_SQL.SET_SEARCH_PATH.Replace("$SEARCHPATH$", searchPath));
                    }
                    
                }

                if (!_dbVars.ContainsKey("date"))
                {
                    _dbVars.Add("date", await GetCurrentDate());
                }

                return "";

            }
            catch (Exception ex)
            {
                if (_traceService.IsTracingEnabled()) _traceService.LogMessage($"Error in initiating DB connection for application '{_appName}'. Error: {ex.Message}");
                throw new Exception($"Error: {ex.Message}");
            }
        }

        public async Task CloseDBConnectionAsync()
        {
            if (_dbHelper != null)
            {
                RollbackActiveTransactions();
                await _dbHelper.CloseDBConnectionAsync();
            }

            await CloseRedisConnectionAsync();
        }

        public void RollbackActiveTransactions()
        {
            if (_dbHelper != null)
            {
                if (IsTransactionActive(_dbHelper.Transaction))
                {
                    _dbHelper.RollBackTransaction();
                }                
            }
        }

        public static bool IsTransactionActive(DbTransaction transaction)
        {
            try
            {
                if (transaction == null)
                    return false;

                if (transaction?.Connection == null)
                    return false;

                if (transaction?.Connection.State != System.Data.ConnectionState.Open)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetCurrentDate()
        {
            if (_dbVars.ContainsKey("datesql"))
            {
                string sql = _dbVars["datesql"];
                var dateResult = await _dbHelper.ExecuteSQLAsync(sql);
                if (dateResult != null && string.IsNullOrEmpty(dateResult.error) && dateResult.data != null && dateResult.data.Rows.Count > 0)
                {
                    DateTime dateValue = (DateTime)dateResult.data.Rows[0][0];
                    return dateValue.ToString("dd/MM/yyyy");
                }

                if (dateResult != null && !string.IsNullOrEmpty(dateResult.error))
                    return dateResult.error;
            }

            return "";
        }

        public Dictionary<string, string> GetDBVarsList(string dbType)
        {
            Dictionary<string, string> dbVars = new Dictionary<string, string>();
            switch (dbType.ToLower())
            {
                case "oracle":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " for update ");
                    dbVars.Add("sqlnull", "nvl");
                    dbVars.Add("nullcds", "select lpad(' ', 250, ' ') LVAL from dual");
                    dbVars.Add("dbdateformat", "dd-mmm-yyyy");
                    dbVars.Add("dbdatestring", "to_date( $DATE$,'dd/mm/yyyy hh24:mi:ss')");
                    dbVars.Add("datesql", "Select sysdate as sdt from dual");
                    dbVars.Add("dbdatestringwithms", "to_date( $DATE$,'dd/mm/yyyy hh24:mi:ss.ff3')");
                    break;
                case "postgre":
                case "postgres":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " for update ");
                    dbVars.Add("sqlnull", "coalesce");
                    dbVars.Add("nullcds", "SELECT LPAD('', 250, ' ') AS LVAL");
                    dbVars.Add("dbdateformat", "dd/mm/yyyy");
                    dbVars.Add("dbdatestring", "to_timestamp( $DATE$ ,'dd/mm/yyyy hh24:mi:ss')");
                    dbVars.Add("datesql", "Select now() as sdt");
                    dbVars.Add("dbdatestringwithms", "to_timestamp( $DATE$ ,'dd/mm/yyyy hh24:mi:ss.ms')");
                    break;
                case "ms sql":
                case "mssql":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " with(holdlock,rowlock) ");
                    dbVars.Add("sqlnull", "isnull");
                    dbVars.Add("nullcds", "select lval from dual with(nolock)");
                    dbVars.Add("dbdateformat", "mm/dd/yyyy");
                    dbVars.Add("dbdatestring", "CONVERT(DATETIME, $DATE$, 103)");
                    dbVars.Add("datesql", "SELECT GETDATE() as sdt");
                    dbVars.Add("dbdatestringwithms", "CONVERT(DATETIME2(3), $DATE$, 103)");
                    break;
                default:
                    throw new NotSupportedException("Invalid database type");
            }

            return dbVars;
        }        

        public APIResult ParseAxpertRestAPIResult(ARMResult armResult)
        {
            APIResult result = new APIResult();
            result.data.Add("success", armResult.result["success"]);
            JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
            if (resultJsonObj?["status"] == null)
            {
                resultJsonObj = (JObject)resultJsonObj?["result"]?[0];
            }
            if (!(resultJsonObj?["status"]?.ToString().ToLower() == "true" || resultJsonObj?["status"]?.ToString().ToLower() == "success"))
            {
                result.error = resultJsonObj?["result"]?.ToString();
                return result;
            }
            foreach (var property in resultJsonObj.Properties())
            {
                if (property.Name.ToLower() != "status")
                    result.data.Add(property.Name, property.Value);
            }
            return result;

        }
        private string GetAppNameFromCacheKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            // Find "-ARM-" or "ARM-" in a case-insensitive way
            int index = key.IndexOf("ARM-", StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                // Get the part after ARM-
                string[] splitRes = key.Split(new[] { "ARM-" }, StringSplitOptions.None);
                // For now, just return until next separator (if any)
                var parts = splitRes[1].Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[0] : string.Empty;
            }
            else
            {
                string[] splitRes = key.Split(new[] { "-" }, StringSplitOptions.None);
                return splitRes[0];
            }
        }

        public async Task<bool> StoreAxParserObjInCache(string sessionId, string axParserObj, int expiry)
        {
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _redisCache.StringSetAsync($"{sessionId}-AXPARSER", axParserObj, expiry);
            }
            else
                return false;

        }
        public async Task<string> GetAxParserObjFromCache(string sessionId)
        {
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _redisCache.StringGetAsync($"{sessionId}-AXPARSER");
            }
            return "";
        }

        public async Task<bool> SetStructDefObjToCache(string key, string structDef)
        {
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _redisCache.StringSetAsync(key, structDef);
            }
            else
                return false;

        }
        public async Task<string> GetStructDefObjFromCache(string key)
        {
            var redisConnected = await OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _redisCache.StringGetAsync(key);
            }
            return "";
        }

        public async Task<bool> SetPageTitleToCache(string structType, string structId, string structName)
        {
            var language = _sessionVars[SESSION_DATA.LANGUAGE.ToString()].ToString();
            if (language.Substring(0, 3).ToLower() == "eng")
                language = "eng";
            var pageTitleKeyParams = new string[] { GetSchemaName(), "axpagetitle", language };
            string pageTitlekey = GenerateRedisKeyString(pageTitleKeyParams);
            var pageTitles = await _redisCache.ObjectGetAsync(pageTitlekey);
            var pageTitleArr = new ArrayList();
            var value = structType + "♠" + structName + "♠" + structId;
            if (pageTitles != null)
            {
                pageTitleArr = (ArrayList)pageTitles;
                if (!pageTitleArr.Contains(value))
                {
                    pageTitleArr.Add(value);
                    var redisConnected = await OpenRedisConnectionAsync();
                    if (redisConnected)
                        await _redisCache.ObjectSetAsync(pageTitlekey, pageTitleArr);

                    return true;
                }
            }
            else
            {
                pageTitleArr.Add(value);
                var redisConnected = await OpenRedisConnectionAsync();
                if (redisConnected)
                    await _redisCache.ObjectSetAsync(pageTitlekey, pageTitleArr);

                return true;
            }
            return false;
        }

        public string ConvertDBDateToClientDateFormat(string customDataType, string value, string clientDateFormat)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(clientDateFormat) && clientDateFormat != "dd/MM/yyyy")
            {
                if (customDataType.ToUpper() == "DATE")
                {
                    DateTime dateValue = DateTime.ParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    value = dateValue.ToString(clientDateFormat, CultureInfo.InvariantCulture);
                }
                else if (customDataType.ToUpper() == "TIMESTAMP")
                {
                    if (_milliSecsInTimeStamp)
                    {
                        DateTime dateValue = DateTime.ParseExact(value, "dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                        value = dateValue.ToString($"{clientDateFormat} HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        DateTime dateValue = DateTime.ParseExact(value, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        value = dateValue.ToString($"{clientDateFormat} HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
            }

            return value;
        }

        public string ConvertClientDateToDBDateFormat(string customDataType, string value, string clientDateFormat)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(clientDateFormat) && clientDateFormat != "dd/MM/yyyy")
            {
                if (customDataType.ToUpper() == "DATE")
                {
                    DateTime dateValue = DateTime.ParseExact(value, clientDateFormat, CultureInfo.InvariantCulture);
                    if (dateValue.Year <= 1900)
                        throw new Exception("Timestamp/Date field value should be greater than year 1900");

                    value = dateValue.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                else if (customDataType.ToUpper() == "TIMESTAMP")
                {
                    if (value.IndexOf(" ") == -1)
                        value = value.Trim() + " 00:00:00";

                    if (_milliSecsInTimeStamp)
                    {
                        if (value.IndexOf(".") == -1)
                            value = value.Trim() + ".000";

                        DateTime dateValue = DateTime.ParseExact(value, $"{clientDateFormat} HH:mm:ss.fff", CultureInfo.InvariantCulture);

                        if (dateValue.Year <= 1900)
                            throw new Exception("Timestamp/Date field value should be greater than year 1900");

                        value = dateValue.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        DateTime dateValue = DateTime.ParseExact(value, $"{clientDateFormat} HH:mm:ss", CultureInfo.InvariantCulture);

                        if (dateValue.Year <= 1900)
                            throw new Exception("Timestamp/Date field value should be greater than year 1900");

                        value = dateValue.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
            }

            return value;
        }

        public async Task<bool> ValidateSession(string sessionId)
        {
            bool valid = false;
            try {
                await GetSessionData(sessionId);
                if (_sessionVars == null || _sessionVars.Count == 0)
                {
                    valid = false;                                      
                }
                else
                {
                    valid = true;
                }
            }
            catch { }
            finally
            {
                if (!valid)
                    await CloseRedisConnectionAsync();
            }
            return valid;
        }

        public string GetAxgApplicableDimensionsString()
        {
            string dimensionsString = string.Empty;

            if(_globalVars.Count == 0)
            {
                GetSessionData(_armSessionId).GetAwaiter().GetResult();
            }

            if (_globalVars.ContainsKey("axg_applicable_dimensions"))
            {
                string dimensions = _globalVars["axg_applicable_dimensions"];
                if (!string.IsNullOrEmpty(dimensions))
                {
                    foreach (string dimension in dimensions.Split(","))
                    {
                        if (_globalVars.ContainsKey(dimension.Trim()))
                        {
                            if (!string.IsNullOrEmpty(_globalVars[dimension.Trim()]))
                            {
                                var tempVal = $"{dimension.Trim()}='{_globalVars[dimension.Trim()]}'";
                                if (string.IsNullOrEmpty(dimensionsString))
                                    dimensionsString = tempVal;
                                else
                                    dimensionsString += "~~" + tempVal;
                            }
                        }
                    }
                }
            }
            else
                dimensionsString = "";

            if (string.IsNullOrEmpty(dimensionsString))
                dimensionsString = "NA";

            return dimensionsString;

        }

        public async Task RestoreSessionGlobalsToParser(IAxParser parser)
        {
            if (parser == null ||
                _globalVars == null ||
                _globalVars.Count == 0)
            {
                return;
            }

            foreach (var globalVar in _globalVars)
            {
                // Metadata, not an actual parser variable.
                if (globalVar.Key.Equals(
                        "appvartypes",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var variableType =
                    await GetGlobalVariableType(globalVar.Key);

                parser.RegisterVar(
                    globalVar.Key,
                    string.IsNullOrEmpty(variableType) ? "c" : variableType,
                    globalVar.Value ?? string.Empty);
            }
        }

    }
}

