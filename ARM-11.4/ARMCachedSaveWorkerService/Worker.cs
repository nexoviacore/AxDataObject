using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ARMCommon.Services;
using System.Text;
using System.Xml;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

public class Worker : BackgroundService
{
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly IConfiguration _config;
    private readonly IRabbitMQConsumer _consumer;
    private readonly Utils _common;
    public AppSettingsService _appSettingsService;
    public AppSettingsHelper _appSettingsHelper;
    public Worker(IConfiguration config, IRabbitMQConsumer consumer, Utils common)
    {
        _config = config;
        _consumer = consumer;
        _common = common;
        _appSettingsService = new AppSettingsService();
        _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
        _common._appSettingsHelper = _appSettingsHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string queueName = _config["AppConfig:QueueName"];
        try
        {
            async Task<string> OnConsuming(string message)
            {
                try
                {
                    return await CachedSaveNew(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{ex}\n\n");
                    return "error";
                }
            }
            _consumer.DoConsumeWorkerServiceStable(queueName, OnConsuming, stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            _exeDir = Path.Combine(_exeDir, _logFileName);
            File.AppendAllText(_exeDir, $"{ex}\n\n" + Environment.NewLine);
        }
    }
    public JObject XmlToJObject(string xml)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        string json = JsonConvert.SerializeXmlNode(doc);
        var parsed = JObject.Parse(json);
        var axpropsObj = parsed["axprops"] as JObject ?? new JObject();
        RemoveAttributes(axpropsObj);
        return axpropsObj;
    }

    private void RemoveAttributes(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            var props = obj.Properties().Where(p => p.Name.StartsWith("@")).ToList();
            foreach (var p in props)
                p.Remove();
            foreach (var child in obj.Properties())
                RemoveAttributes(child.Value);
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token)
                RemoveAttributes(item);
        }
    }
    private async Task<JObject> GetRedisJson(string key, string field, string _exeDir)
    {
        //string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GLOBALVARS.txt");
        //if (File.Exists(jsonFilePath))
        //{
        //    string fileValue = await File.ReadAllTextAsync(jsonFilePath);
        //    if (string.IsNullOrWhiteSpace(fileValue))
        //    {
        //        return new JObject();
        //    }
        //    try
        //    {
        //        return JObject.Parse(fileValue);
        //    }
        //    catch (JsonReaderException ex)
        //    {
        //        return new JObject();
        //    }
        //}
        try
        {
            var value = await _common._redisCache.HashGetAsync(key, field);
            //return !string.IsNullOrEmpty(value) ? JObject.Parse(value) : new JObject();
            if (string.IsNullOrEmpty(value))
            {
                File.AppendAllText(_exeDir, "Error:Empty json:" + Environment.NewLine + value + Environment.NewLine + "ARMSessionId:" + key);
                return new JObject();
            }
            return JObject.Parse(value);
        }
        catch (Exception ex)
        {
            File.AppendAllText(_exeDir, "Error:GetRedisJson json parse:" + Environment.NewLine + ex.Message + Environment.NewLine + "ARMSessionId:" + key);
            return new JObject();
        }
    }
    private async Task<string> GetRedisString(string key, string field)
    {
        var value = await _common._redisCache.HashGetAsync(key, field);
        return !string.IsNullOrEmpty(value) ? value.ToString() : "";
    }
    private async Task<string> GetRedisSXML(string proj, string transid, string roles, string lng)
    {
        RedisValue value = await _common._redisCache.StringGetAsync(proj + "-" + transid + "-" + roles + "-" + lng);
        if (!value.HasValue)
            return string.Empty;
        byte[] bytes = (byte[])value;
        string xml = ExtractXml(bytes);
        return xml;
    }
    public static string ExtractXml(byte[] bytes)
    {
        string text = Encoding.UTF8.GetString(bytes);

        int start = text.IndexOf("<root", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        int end = text.IndexOf("</root>", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            return null;

        end += "</root>".Length;

        return text.Substring(start, end - start);
    }
    private async Task<string> CachedSaveNew(string queueRequest)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string _logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, _logFileName);
        string statuskey = string.Empty;
        try
        {
            //File.AppendAllText(_exeDir, "CachedSave called:" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + Environment.NewLine);
            JObject saveObj = JObject.Parse(queueRequest);
            string queueData = saveObj["queuedata"]?.ToString();
            JObject saveData;
            if (!string.IsNullOrEmpty(queueData))
            {
                queueData = queueData.Replace("\r\n”}", "”}").Replace("\\”", "”").Trim('"');
                saveData = JsonConvert.DeserializeObject<JObject>(queueData);
                if (saveData?["payload"] != null)
                    saveData = (JObject)saveData["payload"];
            }
            else
            {
                saveData = saveObj;
            }
            var savedataToken = saveData["_parameters"]?[0] != null ? saveData["_parameters"]?[0] : (saveData["isaxput"]?.ToString() != null ? saveData : null);
            if (savedataToken == null)
                return JsonConvert.SerializeObject(new { error = "Invalid savedata" });
            var _savedataToken = saveData["_parameters"]?[0]?["savedata"];
            if (_savedataToken != null && _savedataToken["transid"] != null)
            {
                return await ProcessSingleSave(saveData, _exeDir);
            }

            string ARMSessionId = savedataToken?["ARMSessionId"]?.ToString();
            if (ARMSessionId == null || ARMSessionId == string.Empty)
            {
                File.AppendAllText(_exeDir, "Error: ARMSessionId should not be empty" + Environment.NewLine);
                return JsonConvert.SerializeObject(new { error = "ARMSessionId should not be empty" });
            }
            statuskey = savedataToken?["statuskey"]?.ToString();
            string isAxPut = savedataToken?["isaxput"]?.ToString();
            if (isAxPut != null && isAxPut.ToLower() == "true")
            {
                string mobile = savedataToken?["mobile"]?.ToString();
                string axm_queueid = savedataToken?["axm_queueid"]?.ToString();
                if ((mobile != null && mobile.ToLower() == "true") || (axm_queueid != null && axm_queueid != ""))
                {
                    return await ProcessMultiSaveFromMobileAxPut(saveData, ARMSessionId, _exeDir, statuskey);
                }
                else
                {
                    return await ProcessMultiSaveAxPut(saveData, ARMSessionId, _exeDir, statuskey);
                }
            }
            else
            {
                string strTotMesg = string.Empty;
                int transPros = 0;
                try
                {
                    if (!string.IsNullOrEmpty(statuskey))
                    {
                        int transCount = ((JObject)savedataToken).Properties().Count(p => p.Name.StartsWith("transaction"));
                        strTotMesg = "Total transactions: " + transCount;
                    }
                }
                catch (Exception ex) { }

                List<object> results = new List<object>();
                List<string> sheetNames = new List<string>();
                string VirtualUploadFilePath = string.Empty;
                string VirtualDownloadFilePath = string.Empty;
                string filename = string.Empty;

                string project = savedataToken?["project"] != null ? savedataToken?["project"]?.ToString() : (savedataToken?["axpapp"] != null ? savedataToken?["axpapp"]?.ToString() : "");   //savedataToken?["project"]?.ToString();
                string username = savedataToken?["username"]?.ToString();
                string sessionId = savedataToken?["sessionid"]?.ToString();
                if (sessionId == null || sessionId == string.Empty)
                    sessionId = ARMSessionId;
                string trace = savedataToken?["trace"]?.ToString();
                var _appSettingsService = new AppSettingsService();
                var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                _common._appName = project;
                _common._appSettingsHelper = _appSettingsHelper;
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];
                var armSettings = _appSettingsHelper.GetARMSettings(project);
                string ScriptsPath = armSettings.ScriptsPath;
                if (sessionId != null)
                    ScriptsPath = Path.Combine(ScriptsPath, "log", sessionId);
                else
                    ScriptsPath = Path.Combine(ScriptsPath, "log", "CachedSave");
                if (!Directory.Exists(ScriptsPath))
                    Directory.CreateDirectory(ScriptsPath);
                if (ScriptsPath != "")
                {
                    string logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                    _exeDir = Path.Combine(ScriptsPath, logFileName);
                }

                var redisConnected = await _common.OpenRedisConnectionAsync();
                if (!redisConnected)
                {
                    //await File.AppendAllTextAsync(logPath, "Error: Redis not connecting" + Environment.NewLine);
                    File.AppendAllText(_exeDir, "Error: Redis not connecting" + Environment.NewLine + " RedisInfo:" + _common._redisCache.RedisSettings.Host + " " + _common._redisCache.RedisSettings.Port + " " + _common._redisCache.RedisSettings.DecryptedPwd);
                    return JsonConvert.SerializeObject(new { error = "Redis not connecting" });
                }
                JObject globalvars = await GetRedisJson(ARMSessionId, "GLOBALVARS", _exeDir);
                if (globalvars == null || globalvars.ToString() == "{}")
                {
                    //await File.AppendAllTextAsync(logPath, "Error: Globalvars are empty, please check ARMSessionId and try again" + Environment.NewLine);
                    File.AppendAllText(_exeDir, "Error: Globalvars are empty, please check ARMSessionId and try again" + Environment.NewLine);
                    return JsonConvert.SerializeObject(new { error = "Globalvars are empty, please check ARMSessionId and try again" });
                }

                if (globalvars.TryGetValue("virtualuploadfilepath", StringComparison.OrdinalIgnoreCase, out JToken uploadToken))
                    VirtualUploadFilePath = uploadToken?.ToString();
                if (globalvars.TryGetValue("virtualdownloadfilepath", StringComparison.OrdinalIgnoreCase, out JToken downloadToken))
                    VirtualDownloadFilePath = downloadToken?.ToString();
                var _axprop = _common.GetAxProps(project);
                JObject axprop = XmlToJObject(_axprop.Result);
                var axapps = _appSettingsHelper.GetAppConnection(project);
                JObject dbConfig = new JObject
                {
                    ["type"] = axapps.Type,
                    ["structurl"] = axapps.StructUrl,
                    ["db"] = axapps.Db,
                    ["driver"] = axapps.Driver,
                    ["version"] = axapps.Version,
                    ["dbcon"] = axapps.DbCon,
                    ["dbuser"] = axapps.DbUser,
                    ["pwd"] = "",//axapps.Pwd,
                    ["dataurl"] = axapps.DataUrl
                };
                string callerid = savedataToken?["callerid"]?.ToString();
                if (callerid == null)
                    callerid = "";
                string cachedsavefromimport = savedataToken?["cachedsavefromimport"]?.ToString();
                if (cachedsavefromimport == "true")
                {
                    string dataupdate = savedataToken?["dataupdate"]?.ToString();
                    if (dataupdate == null)
                        dataupdate = "";
                    string primarykey = savedataToken?["primarykey"]?.ToString();
                    if (primarykey == null)
                        primarykey = "";
                    string sXML = string.Empty;
                    string _thsTransid = string.Empty;
                    string _thisproject = project;
                    string dbName = GetDataBase(connectionString, dbType, configcon);
                    if (dbName != null && dbName != string.Empty)
                        _thisproject = _thisproject + "~" + dbName;
                    foreach (JProperty formNode in savedataToken.Children<JProperty>())
                    {
                        if (!formNode.Name.StartsWith("transaction"))
                            continue;
                        JObject txnData = formNode.Value as JObject;
                        if (txnData == null)
                            continue;
                        if (txnData["transid"]?.ToString() != "" && _thsTransid != txnData["transid"]?.ToString())
                        {
                            _thsTransid = txnData["transid"]?.ToString();
                            string LANGUAGE = await GetRedisString(ARMSessionId, "LANGUAGE");
                            string USER_ROLES = await GetRedisString(ARMSessionId, "USER_ROLES");
                            string lng = LANGUAGE.Substring(0, 3);
                            string uRoles = GetAxRoleRedis(USER_ROLES);
                            sXML = await GetRedisSXML(_thisproject, txnData["transid"]?.ToString(), uRoles, lng);
                        }
                        string strsheetNames = string.Empty;
                        filename = savedataToken?["importfile"]?.ToString();
                        JArray recdataArray = txnData["recdata"] as JArray;
                        foreach (JObject rec in recdataArray)
                        {
                            foreach (JProperty prop in rec.Properties())
                            {
                                if (prop.Value is JArray arr)
                                {
                                    foreach (JObject item in arr)
                                    {
                                        var name = item["sheetname"]?.ToString();
                                        var rowno = item["columns"]?["axp_xlsheetrowno"]?.ToString();
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            string value = $"{name}~{rowno}";
                                            if (string.IsNullOrEmpty(strsheetNames))
                                                strsheetNames = value;
                                            else
                                                strsheetNames += "?" + value;
                                        }
                                    }
                                }
                            }
                        }
                        JObject savedata = new JObject
                        {
                            ["cachedsave"] = (cachedsavefromimport == "true" ? "false" : "true"),
                            ["axpapp"] = project,
                            ["appsessionkey"] = "",
                            ["transid"] = txnData["transid"]?.ToString(),
                            ["s"] = sessionId,
                            ["username"] = username,
                            ["password"] = "",
                            ["afiles"] = txnData["afiles"]?.ToString() ?? "",
                            ["trace"] = trace,
                            ["dataupdate"] = dataupdate,
                            ["primarykey"] = primarykey,
                            ["recordid"] = txnData["recordid"]?.ToString() ?? "0",
                            ["changedrows"] = txnData["changedrows"] as JObject ?? new JObject(),
                            ["recdata"] = txnData["recdata"] as JArray ?? new JArray()
                        };
                        string transid = txnData["transid"]?.ToString();
                        JObject singleSaveData = new JObject
                        {
                            ["_parameters"] = new JArray(
                                new JObject
                                {
                                    ["savedata"] = savedata,
                                    [project] = dbConfig,
                                    ["axprops"] = axprop,
                                    ["globalvars"] = globalvars,
                                    ["uservars"] = new JObject()
                                }
                            )
                        };
                        string debugJson = JsonConvert.SerializeObject(singleSaveData, Newtonsoft.Json.Formatting.Indented);
                        string result = await ProcessMultiSave(singleSaveData, sXML, _exeDir);
                        results.Add(JsonConvert.DeserializeObject(result));
                        if (!string.IsNullOrEmpty(statuskey))
                        {
                            transPros++;
                            string strTotMesgNew = Environment.NewLine + "Processed transactions: " + transPros;
                            strTotMesgNew = strTotMesg + strTotMesgNew;
                            SetRedisKey(statuskey, strTotMesgNew);
                        }
                        sheetNames.Add(strsheetNames);
                    }
                }
                else
                {
                    string sequenceno = string.Empty;
                    string sXML = string.Empty;
                    string _thsTransid = string.Empty;
                    string _thisproject = project;
                    string dbName = GetDataBase(connectionString, dbType, configcon);
                    if (dbName != null && dbName != string.Empty)
                        _thisproject = _thisproject + "~" + dbName;
                    foreach (JProperty formNode in savedataToken.Children<JProperty>())
                    {
                        if (!formNode.Name.StartsWith("transaction"))
                            continue;
                        JObject txnData = formNode.Value as JObject;
                        if (txnData == null)
                            continue;
                        if (txnData["transid"]?.ToString() != "" && _thsTransid != txnData["transid"]?.ToString())
                        {
                            _thsTransid = txnData["transid"]?.ToString();
                            string LANGUAGE = await GetRedisString(ARMSessionId, "LANGUAGE");
                            string USER_ROLES = await GetRedisString(ARMSessionId, "USER_ROLES");
                            string lng = LANGUAGE.Substring(0, 3);
                            string uRoles = GetAxRoleRedis(USER_ROLES);
                            sXML = await GetRedisSXML(_thisproject, txnData["transid"]?.ToString(), uRoles, lng);
                        }
                        JObject savedata = new JObject
                        {
                            ["cachedsave"] = "false",// txnData["cachedsave"]?.ToString() ?? "",
                            ["axpapp"] = project,
                            ["appsessionkey"] = "",
                            ["transid"] = txnData["transid"]?.ToString(),
                            ["s"] = sessionId,
                            ["username"] = username,
                            ["password"] = "",
                            ["afiles"] = txnData["afiles"]?.ToString() ?? "",
                            ["trace"] = trace,
                            ["keyfield"] = txnData["keyfield"]?.ToString() ?? "",
                            ["keyvalue"] = txnData["keyvalue"]?.ToString() ?? "",
                            ["recordid"] = txnData["recordid"]?.ToString() ?? "0",
                            ["changedrows"] = txnData["changedrows"] as JObject ?? new JObject(),
                            ["recdata"] = txnData["recdata"] as JArray ?? new JArray()
                        };
                        var returnField = txnData["returnfield"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(returnField))
                        {
                            savedata.Property("recordid")?.AddAfterSelf(new JProperty("returnfield", returnField));
                        }
                        string transid = txnData["transid"]?.ToString();
                        sequenceno = txnData["sequenceno"]?.ToString();
                        JObject singleSaveData = new JObject
                        {
                            ["_parameters"] = new JArray(
                                new JObject
                                {
                                    ["savedata"] = savedata,
                                    [project] = dbConfig,
                                    ["axprops"] = axprop,
                                    ["globalvars"] = globalvars,
                                    ["uservars"] = new JObject()
                                }
                            )
                        };
                        string debugJson = JsonConvert.SerializeObject(singleSaveData, Newtonsoft.Json.Formatting.Indented);
                        string result = await ProcessMultiSave(singleSaveData, sXML, _exeDir);
                        results.Add(JsonConvert.DeserializeObject(result));
                        if (!string.IsNullOrEmpty(statuskey))
                        {
                            transPros++;
                            string strTotMesgNew = Environment.NewLine + "Processed transactions: " + transPros;
                            strTotMesgNew = strTotMesg + strTotMesgNew;
                            SetRedisKey(statuskey, strTotMesgNew);
                        }
                        if (!string.IsNullOrEmpty(callerid) && !string.IsNullOrEmpty(sequenceno))
                        {
                            string _thisresult = result;
                            string existKeyVal = await ReadRedisKeyAsync(project + "-" + callerid + "-" + sequenceno);
                            if (!string.IsNullOrEmpty(existKeyVal))
                                _thisresult += "," + existKeyVal;
                            SetRedisKey(project + "-" + callerid + "-" + sequenceno, _thisresult, 1800);
                        }
                        JObject keyInfo = GetKeyField(result);
                        string _returnField = keyInfo["returnfield"]?.ToString();
                        string _returnFieldVal = keyInfo["returnfieldval"]?.ToString();
                        AxSaveAPIResult(_exeDir, dbType, connectionString, callerid, sequenceno, transid, _returnField, _returnFieldVal, debugJson, result, username);
                    }
                }

                if (results.Count > 0)
                {
                    string _dFilePath = string.Empty;
                    if (cachedsavefromimport == "true")
                        _dFilePath = UpdateExcelWithResultsImport(VirtualUploadFilePath, VirtualDownloadFilePath, filename, results, sheetNames, _exeDir);
                    else if (string.IsNullOrEmpty(callerid))
                        _dFilePath = UpdateTextWithResults(VirtualDownloadFilePath, results, _exeDir);
                    try
                    {
                        StringBuilder msgBuilder = new StringBuilder();
                        var firstItem = JObject.Parse(results[0].ToString());
                        string tstructCap = string.Empty;
                        string transid = string.Empty;

                        string signalrUrl = _appSettingsHelper.GetARMNotificationUrl(project);
                        signalrUrl += "/api/v1/SendSignalR";
                        bool success = false;
                        bool error = false;
                        foreach (var item in results)
                        {
                            JObject obj = JObject.Parse(item.ToString());
                            string _tstructCap = obj["tstructCap"]?.ToString();
                            tstructCap = _tstructCap + ",";
                            transid = obj["transid"]?.ToString() + ",";
                            bool _status = (bool)obj["status"];
                            JObject objmsg = JObject.Parse(obj["msg"].ToString());
                            if (objmsg["result"][0]["error"] != null && _status)
                            {
                                error = true;
                                msgBuilder.Append(_tstructCap + " " + objmsg["result"][0]["error"]["msg"].ToString() + Environment.NewLine);
                            }
                            else if (objmsg["result"][0]["message"] != null && _status)
                            {
                                success = true;
                                msgBuilder.Append(objmsg["result"][0]["message"][0]["msg"].ToString() + " Recordid:" + objmsg["result"][0]["message"][0]["recordid"].ToString() + Environment.NewLine);
                            }
                        }
                        string finalMsg = msgBuilder.ToString().TrimEnd(',', ' ');
                        string notiTitle = "Multi transaction saved successfully";
                        if (success && error)
                            notiTitle = "Multi Transaction saved successfully with error";
                        else if (!success && error)
                            notiTitle = "Multi Transaction save- error";
                        string _srPayload = JsonConvert.SerializeObject(new[] { new { notifytype = "cachedsave", type = "TStruct", icon = "", title = notiTitle, message = finalMsg, dt = DateTime.Now.ToString(), link = new { t = "e", name = _dFilePath, p = "", act = "open", axmsgid = "" } } });
                        if (!string.IsNullOrEmpty(finalMsg))
                        {
                            _dFilePath = _dFilePath.Replace(@"\", "\\\\");
                            string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG_REQPAYLOAD;
                            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            //finalMsg = finalMsg.Replace("'", "''");
                            finalMsg = CheckSpecialChars(finalMsg);
                            finalMsg = finalMsg.Replace(Environment.NewLine, " | ");
                            sql = string.Format(sql, currentTime, "cachedsave", username, username, "cached save", "cached save", "cached save -" + transid, transid, notiTitle, finalMsg, _dFilePath, "", "", "e");
                            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                            await SendSignalRMessage(signalrUrl, project, username, _srPayload);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(_exeDir, "Error result parse: " + JsonConvert.SerializeObject(results) + " Exception:" + ex + Environment.NewLine);
                    }
                }
                if (!string.IsNullOrEmpty(statuskey))
                    DeleteRedisKey(statuskey);
                return JsonConvert.SerializeObject(results);
            }
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(statuskey))
                DeleteRedisKey(statuskey);
            File.AppendAllText(_exeDir, "Fatal Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new
            {
                error = ex.Message
            });
        }
    }
    private async Task<string> ProcessSingleSave(JObject saveData, string logPath)
    {
        string tstructCap = string.Empty, transid = string.Empty, signalrUrl = string.Empty, project = string.Empty, username = string.Empty;
        try
        {
            string url = saveData["url"]?.ToString();
            string method = saveData["method"]?.ToString();
            if (string.IsNullOrEmpty(method))
                method = "POST";
            project = saveData["_parameters"]?[0]?["savedata"]?["axpapp"]?.ToString();
            username = saveData["_parameters"]?[0]?["savedata"]?["username"]?.ToString();
            transid = saveData["_parameters"]?[0]?["savedata"]?["transid"]?.ToString();
            tstructCap = saveData["_parameters"]?[0]?["savedata"]?["tstructcap"]?.ToString();
            bool trace = saveData["_parameters"]?[0]?["savedata"]?["trace"]?.ToString() == "true";
            string payload = JsonConvert.SerializeObject(saveData);
            string _errorPayload = saveData["_parameters"]?[0].ToString();
            var _appSettingsService = new AppSettingsService();
            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appName = project;
            _common._appSettingsHelper = _appSettingsHelper;
            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);
            string connectionString = configcon["ConnectionString"];
            string dbType = configcon["DBType"];

            string armScriptUrl = _appSettingsHelper.GetARMScriptsURL(project);
            signalrUrl = _appSettingsHelper.GetARMNotificationUrl(project);
            signalrUrl += "/api/v1/SendSignalR";
            if (string.IsNullOrEmpty(url))
            {
                url = $"{armScriptUrl.TrimEnd('/')}/ASBTStructRest.dll/datasnap/rest/TASBTstruct/savedata";
            }
            if (trace)
            {
                //await File.AppendAllTextAsync(logPath, $"URL: {url}\nPayload: {payload}\n");
                File.AppendAllText(logPath, $"URL: {url}\nPayload: {payload}\n");
            }
            ARMResult apiResult = (method == "POST") ? await POSTData(url, payload, "application/json", "") : await new API().GetData(url);
            string msg = string.Empty;
            string axmsgid = string.Empty;
            string _srPayload = string.Empty;
            try
            {
                bool iserror = false;
                JObject apimessage = JObject.Parse(apiResult.result["message"].ToString());
                bool _status = (bool)apiResult.result["success"];
                if (apimessage["result"][0]["error"] != null && _status)
                {
                    string timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss"); // 12 digits
                    string rnd = new Random().Next(0, 1000).ToString("D3");
                    axmsgid = timestamp + rnd.ToString();

                    iserror = true;
                    msg = apimessage["result"][0]["error"]["msg"].ToString();
                    msg = msg.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
                    _srPayload = "[{\"notifytype\":\"cachedsave\",\"type\":\"TStruct\",\"icon\":\"\",\"title\":\"Error Alert: " + tstructCap + " Not Saved\",\"message\":\"" + msg + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"t\",\"name\":\"" + transid + "\",\"p\":\"\",\"act\":\"open\",\"axmsgid\":\"" + axmsgid + "\"}}]";
                    //_srPayload = "[{\"notifytype\":\"cachedsave\",\"type\":\"TStruct\",\"icon\":\"\",\"title\":\"Error Alert: " + tstructCap + " Not Saved\",\"message\":\"" + msg + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"\",\"name\":\"\",\"p\":\"\",\"act\":\"open\",\"axmsgid\":\"\"}}]";
                }
                else if (apimessage["result"][0]["message"] != null && _status)
                {
                    msg = apimessage["result"][0]["message"][0]["msg"].ToString();
                    string recId = apimessage["result"][0]["message"][0]["recordid"].ToString();
                    _srPayload = "[{\"notifytype\":\"cachedsave\",\"type\":\"TStruct\",\"icon\":\"\",\"title\":\"" + tstructCap + " saved successfully\",\"message\":\"" + msg + " recordid:" + recId + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"\",\"name\":\"\",\"p\":\"\",\"act\":\"load\",\"axmsgid\":\"\"}}]";
                }
                if (_srPayload != string.Empty)
                {
                    try
                    {
                        string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG_REQPAYLOAD;
                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        if (iserror)
                        {
                            string _payload = _errorPayload;
                            JObject obj = JObject.Parse(_payload);
                            obj["globalvars"] = new JObject();
                            _errorPayload = obj.ToString(Newtonsoft.Json.Formatting.None);
                            sql = string.Format(sql, currentTime, "cachedsave", username, username, "cached save", "cached save", "cached save -" + transid, transid, msg.Replace("'", "''"), tstructCap, "act=open", axmsgid, _errorPayload, "t" + transid);
                            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
                        }
                        else
                        {
                            sql = string.Format(sql, currentTime, "cachedsave", username, username, "cached save", "cached save", "cached save -" + transid, transid, msg.Replace("'", "''"), tstructCap, "", "", "", "");
                            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
                        }
                    }
                    catch (Exception) { }

                    await SendSignalRMessage(signalrUrl, project, username, _srPayload);
                }
            }
            catch (Exception ex)
            {
                //await File.AppendAllTextAsync(logPath, "Error result parse: " + JsonConvert.SerializeObject(apiResult.result) + " Exception:" + ex + Environment.NewLine);
                File.AppendAllText(logPath, "Error result parse: " + JsonConvert.SerializeObject(apiResult.result) + " Exception:" + ex + Environment.NewLine);
            }
            return JsonConvert.SerializeObject(new
            {
                status = apiResult.result["success"],
                msg = apiResult.result["message"]
            });
        }
        catch (Exception ex)
        {
            //await File.AppendAllTextAsync(logPath, "Error: " + ex + Environment.NewLine);
            File.AppendAllText(logPath, "Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new { error = ex.Message });
        }
    }
    private async Task<string> ProcessMultiSave(JObject saveData, string sXML, string logPath)
    {
        string tstructCap = string.Empty, transid = string.Empty, project = string.Empty, username = string.Empty;
        try
        {
            string url = string.Empty;// saveData["url"]?.ToString();
            string method = saveData["method"]?.ToString();
            if (string.IsNullOrEmpty(method))
                method = "POST";
            project = saveData["_parameters"]?[0]?["savedata"]?["axpapp"]?.ToString();
            username = saveData["_parameters"]?[0]?["savedata"]?["username"]?.ToString();
            transid = saveData["_parameters"]?[0]?["savedata"]?["transid"]?.ToString();
            tstructCap = saveData["_parameters"]?[0]?["savedata"]?["tstructcap"]?.ToString();
            bool trace = saveData["_parameters"]?[0]?["savedata"]?["trace"]?.ToString() == "true";
            string payload = JsonConvert.SerializeObject(saveData);

            string armScriptUrl = _appSettingsHelper.GetARMScriptsURL(project);
            string proj_BaseRestPath = _config["AppConfig:" + project + "_ASBTStructRest_URL"];
            string selectedUrl = !string.IsNullOrEmpty(proj_BaseRestPath) ? proj_BaseRestPath : armScriptUrl;

            url = $"{selectedUrl.TrimEnd('/')}/ASBTStructRest.dll/datasnap/rest/TASBTstruct/savedata";
            if (trace)
            {
                //await File.AppendAllTextAsync(logPath, $"URL: {url}\nPayload: {payload}\n");
                File.AppendAllText(logPath, $"URL: {url}\nPayload: {payload}\n");
            }
            ARMResult apiResult = (method == "POST") ? await POSTData(url, payload, "application/json", sXML) : await new API().GetData(url);
            return JsonConvert.SerializeObject(new
            {
                status = apiResult.result["success"],
                msg = apiResult.result["message"],
                tstructCap = tstructCap,
                transid = transid
            });
        }
        catch (Exception ex)
        {
            //await File.AppendAllTextAsync(logPath, "Error: " + ex + Environment.NewLine);
            File.AppendAllText(logPath, "Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new { error = ex.Message });
        }
    }
    public string UpdateExcelWithResultsImport(string uploadFolder, string downloadFolder, string fileName, List<object> results, List<string> sheetNames, string _exeDir)
    {
        try
        {
            string downloadPath = Path.Combine(downloadFolder, fileName);
            string uploadPath = Path.Combine(uploadFolder, fileName);

            string fullUploadPath = Path.GetFullPath(uploadPath);
            string fullDownloadPath = Path.GetFullPath(downloadPath);

            //Console.WriteLine($"uploadPath: {uploadPath}{Environment.NewLine}" + $"fullUploadPath: {fullUploadPath}{Environment.NewLine}" + $"downloadFolder: {downloadFolder}{Environment.NewLine}" + $"fullDownloadPath: {fullDownloadPath}{Environment.NewLine}" + $"fullDownloadPath: {fileName}{Environment.NewLine} ");

            if (!fullUploadPath.Equals(fullDownloadPath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(uploadPath))
                {
                    string dir = Path.GetDirectoryName(downloadPath);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    //Console.WriteLine($"Upload Exists: {File.Exists(uploadPath)}{Environment.NewLine}" + $"Upload Path: {uploadPath}{Environment.NewLine}" + $"Download Path: {downloadPath}{Environment.NewLine}");

                    File.Copy(uploadPath, downloadPath, true);

                    //Console.WriteLine($"After Copy Exists: {File.Exists(downloadPath)}{Environment.NewLine}");
                }
            }
            //else
            //{
            //    Console.WriteLine($"Equals else:");
            //}
            //if (!File.Exists(downloadPath))
            //{
            //    Console.WriteLine($"Destination file does not exist after copy: {downloadPath}");
            //    //File.AppendAllText(_exeDir, $"Destination file does not exist after copy: {downloadPath}");
            //}
            using (var workbook = new XLWorkbook(downloadPath))
            {
                var resultColumnCache = new Dictionary<string, int>();

                for (int i = 0; i < results.Count && i < sheetNames.Count; i++)
                {
                    JObject obj;

                    try
                    {
                        obj = JObject.Parse(Convert.ToString(results[i]));
                    }
                    catch
                    {
                        continue;
                    }

                    string message = GetMessage(obj);

                    foreach (string entry in sheetNames[i].Split('?'))
                    {
                        string[] parts = entry.Split('~');

                        if (parts.Length < 3)
                            continue;

                        string sheet = parts[0] + "~" + parts[1];

                        int row;
                        if (!int.TryParse(parts[2], out row))
                            continue;

                        if (!workbook.Worksheets.Contains(sheet))
                            continue;

                        var ws = workbook.Worksheet(sheet);

                        if (!resultColumnCache.ContainsKey(sheet))
                        {
                            int resultCol = 0;

                            foreach (var cell in ws.Row(1).CellsUsed())
                            {
                                if (cell.GetString()
                                    .Equals("Result", StringComparison.OrdinalIgnoreCase))
                                {
                                    resultCol = cell.Address.ColumnNumber;
                                    break;
                                }
                            }

                            if (resultCol == 0)
                            {
                                var lastCol = ws.LastColumnUsed();

                                resultCol = lastCol != null
                                    ? lastCol.ColumnNumber() + 1
                                    : 1;

                                ws.Cell(1, resultCol).Value = "Result";
                                ws.Cell(1, resultCol).Style.Font.Bold = true;
                            }

                            resultColumnCache[sheet] = resultCol;
                        }

                        int colIndex = resultColumnCache[sheet];

                        var resultCell = ws.Cell(row, colIndex);

                        resultCell.Value = message;

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            string lowerMsg = message.ToLowerInvariant();

                            bool isError =
                                lowerMsg.Contains("error") ||
                                lowerMsg.Contains("invalid") ||
                                lowerMsg.Contains("failed") ||
                                lowerMsg.Contains("failure");

                            resultCell.Style.Font.FontColor =
                                isError ? XLColor.Red : XLColor.Green;
                        }
                    }
                }

                workbook.Save();
            }

            return fullDownloadPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Excel update error: uploadPath:" + uploadFolder + " downloadFolder:" + downloadFolder + " fileName:" + fileName + " Exception:" + ex + Environment.NewLine);
            return string.Empty;
        }
    }
    private string GetMessage(JObject obj)
    {
        bool status = obj["status"]?.ToObject<bool>() ?? false;
        var msgToken = obj["msg"];
        if (msgToken == null) return "";
        JObject msgObj;
        try
        {
            msgObj = JObject.Parse(msgToken.ToString());
        }
        catch
        {
            return "Invalid msg format";
        }
        var result = msgObj["result"]?[0];
        if (result == null) return "";
        if (result["error"] != null && status)
            return result["error"]?["msg"]?.ToString();
        if (result["message"] != null && status)
            return result["message"]?[0]?["msg"]?.ToString();
        return "Failed";
    }

    public string UpdateTextWithResults(string downloadFolder, List<object> results, string _exeDir)
    {
        try
        {
            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            string fileName = "CachedSaveResult-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            string downloadPath = Path.Combine(downloadFolder, fileName);

            using (var writer = new StreamWriter(downloadPath, false, Encoding.UTF8))
            {
                int transactionNo = 1;

                foreach (var result in results)
                {
                    string message = "Unknown Result";

                    try
                    {
                        JObject obj = result as JObject ??
                                      JObject.Parse(Convert.ToString(result));

                        message = GetMessage(obj);
                    }
                    catch
                    {
                        try
                        {
                            JObject obj = result as JObject ??
                                          JObject.Parse(Convert.ToString(result));

                            bool status = obj["status"] != null &&
                                          Convert.ToBoolean(obj["status"]);

                            if (status)
                            {
                                message = "Success";
                            }
                            else
                            {
                                message = Convert.ToString(obj["msg"]);
                            }
                        }
                        catch
                        {
                            message = Convert.ToString(result);
                        }
                    }

                    writer.WriteLine(
                        string.Format("Transaction{0} : {1}",
                        transactionNo,
                        message));

                    transactionNo++;
                }
            }

            return downloadPath;
        }
        catch (Exception ex)
        {
            File.AppendAllText(_exeDir, "Excel update error: downloadFolder:" + downloadFolder + " Exception:" + ex + Environment.NewLine);
            return string.Empty;
        }
    }
    private async Task SendSignalRMessage(string signalrUrl, string project, string clientId, string message)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var singalRMessage = new
            {
                project = project,
                UserId = clientId,
                Message = message
            };
            API _api = new API();
            ARMResult armresult = await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
        }
    }
    private static readonly HttpClient client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    public async Task<ARMResult> POSTData(string url, string body, string mediaType, string sXML)
    {
        try
        {
            using (var cts = new CancellationTokenSource())
            {
                if (sXML != string.Empty)
                {
                    JObject json = JObject.Parse(body);
                    JArray parameters = (JArray)json["_parameters"];
                    parameters.Add(sXML);
                    body = json.ToString(Newtonsoft.Json.Formatting.None);
                }

                var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, mediaType), cts.Token).ConfigureAwait(false);
                string content = await response.Content.ReadAsStringAsync();
                return new ARMResult(response.IsSuccessStatusCode, content);
            }
        }
        catch (TaskCanceledException ex)
        {
            return new ARMResult(false, "Request cancelled/timeout: " + ex.Message);
        }
        catch (Exception ex)
        {
            return new ARMResult(false, ex.Message);
        }
    }

    private async Task<string> ProcessMultiSaveFromMobileAxPut(JObject saveData, string ARMSessionId, string _exeDir, string statuskey)
    {
        try
        {
            List<object> results = new List<object>();
            List<string> axm_recids = new List<string>();
            var savedataToken = saveData["_parameters"]?[0];
            string project = savedataToken?["project"]?.ToString();
            string axm_queueid = savedataToken?["axm_queueid"]?.ToString();
            string username = savedataToken?["username"]?.ToString();
            string sessionId = savedataToken?["sessionid"]?.ToString();
            if (sessionId == null || sessionId == string.Empty)
                sessionId = ARMSessionId;
            string trace = savedataToken?["trace"]?.ToString();

            var _appSettingsService = new AppSettingsService();
            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appName = project;
            _common._appSettingsHelper = _appSettingsHelper;
            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);
            string connectionString = configcon["ConnectionString"];
            string dbType = configcon["DBType"];
            var armSettings = _appSettingsHelper.GetARMSettings(project);
            string ScriptsPath = armSettings.ScriptsPath;
            if (sessionId != null)
                ScriptsPath = Path.Combine(ScriptsPath, "log", sessionId);
            else
                ScriptsPath = Path.Combine(ScriptsPath, "log", "CachedSave");
            if (!Directory.Exists(ScriptsPath))
                Directory.CreateDirectory(ScriptsPath);
            if (ScriptsPath != "")
            {
                string logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                _exeDir = Path.Combine(ScriptsPath, logFileName);
            }
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (!redisConnected)
            {
                File.AppendAllText(_exeDir, "Error: Redis not connecting" + Environment.NewLine);
                return JsonConvert.SerializeObject(new { error = "Redis not connecting" });
            }
            string callerid = savedataToken?["callerid"]?.ToString();
            if (callerid == null)
                callerid = "";
            string sequenceno = string.Empty;
            JArray dataArray = savedataToken["data"] as JArray;
            string ArmUrl = _appSettingsHelper.GetARMUrl(project);
            ArmUrl = $"{ArmUrl.TrimEnd('/')}/AxTstructData/api/v1/AxPut";
            if (dataArray != null)
            {
                string strTotMesg = string.Empty;
                int transPros = 0;
                try
                {
                    if (!string.IsNullOrEmpty(statuskey))
                    {
                        int count = dataArray.Children<JObject>().Count();
                        strTotMesg = "Total transactions: " + count;
                    }
                }
                catch (Exception ex) { }

                string ARM_Token = savedataToken["ARMToken"]?.ToString();
                foreach (JObject txnData in dataArray.Children<JObject>())
                {
                    string axm_recid = txnData["axm_recid"]?.ToString();
                    JObject saveDataObject = new JObject
                    {
                        ["ARMSessionId"] = savedataToken["ARMSessionId"]?.ToString(),
                        ["project"] = project,
                        ["username"] = username,
                        ["trace"] = savedataToken["trace"] ?? false,
                        ["validateonly"] = savedataToken["validateonly"] ?? false,
                        ["axclient_dateformat"] = savedataToken["axclient_dateformat"]?.ToString(),
                        ["millisecsintimestamp"] = savedataToken["millisecsintimestamp"] ?? true,
                        ["data"] = new JArray(txnData)
                    };
                    string transid = txnData["transid"]?.ToString();
                    sequenceno = txnData["sequenceno"]?.ToString();
                    string debugJson = JsonConvert.SerializeObject(saveDataObject, Newtonsoft.Json.Formatting.Indented);
                    string result = await CallAxPut(saveDataObject, ARM_Token, ArmUrl, _exeDir);
                    results.Add(JsonConvert.DeserializeObject(result));
                    if (!string.IsNullOrEmpty(statuskey))
                    {
                        transPros++;
                        string strTotMesgNew = Environment.NewLine + "Processed transactions: " + transPros;
                        strTotMesgNew = strTotMesg + strTotMesgNew;
                        SetRedisKey(statuskey, strTotMesgNew);
                    }
                    axm_recids.Add(axm_recid);

                    if (!string.IsNullOrEmpty(callerid) && !string.IsNullOrEmpty(sequenceno))
                    {
                        string _thisresult = result;
                        string existKeyVal = await ReadRedisKeyAsync(project + "-" + callerid + "-" + sequenceno);
                        if (!string.IsNullOrEmpty(existKeyVal))
                            _thisresult += "," + existKeyVal;
                        SetRedisKey(project + "-" + callerid + "-" + sequenceno, _thisresult, 1800);
                    }
                    try
                    {
                        string _keyfield = txnData["keyfield"]?.ToString();
                        string _keyvalue = txnData["keyvalue"]?.ToString();
                        if (!string.IsNullOrEmpty(_keyfield) && !string.IsNullOrEmpty(_keyvalue))
                        {
                            AxSaveAPIResult(_exeDir, dbType, connectionString, callerid, sequenceno, transid, _keyfield, _keyvalue, debugJson, result, username);
                        }
                        else
                        {
                            JObject keyInfo = GetKeyFieldAxPut(result);
                            string _returnField = keyInfo["returnfield"]?.ToString();
                            string _returnFieldVal = keyInfo["returnfieldval"]?.ToString();
                            AxSaveAPIResult(_exeDir, dbType, connectionString, callerid, sequenceno, transid, _returnField, _returnFieldVal, debugJson, result, username);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(_exeDir, "Error: AxSaveAPIResult " + ex.Message + Environment.NewLine);
                    }
                }
            }

            if (results.Count > 0)
            {
                try
                {
                    var failedList = new JArray();
                    var successList = new JArray();
                    bool success = false;
                    bool error = false;
                    int failedCount = 0;
                    int SuccessCount = 0;
                    for (int i = 0; i < results.Count; i++)
                    {
                        JObject obj = JObject.Parse(results[i].ToString());
                        JObject objmsg = JObject.Parse(obj["msg"].ToString());
                        string recid = axm_recids[i];
                        bool _status = (bool)obj["status"];
                        if (_status && objmsg["result"]["message"].ToString() != "success")
                        {
                            failedCount++;
                            error = true;
                            failedList.Add(new JObject
                            {
                                ["axm_recid"] = recid,
                                ["status"] = "failed",
                                ["error_message"] = objmsg["result"]["data"]?.ToString()
                            });
                        }
                        else if (_status && objmsg["result"]["message"].ToString() == "success")
                        {
                            SuccessCount++;
                            success = true;
                            successList.Add(new JObject
                            {
                                ["axm_recid"] = recid,
                                ["status"] = "success",
                                ["success_message"] = objmsg["result"]["data"]?.ToString()
                            });
                        }
                        else if (!_status)
                        {
                            failedCount++;
                            error = true;
                            failedList.Add(new JObject
                            {
                                ["axm_recid"] = recid,
                                ["status"] = "failed",
                                ["error_message"] = objmsg
                            });
                        }
                    }
                    string finalStatus = "success";
                    if (success && error)
                        finalStatus = "partial_success";
                    else if (!success && error)
                        finalStatus = "error";
                    var dataObj = new JObject
                    {
                        ["axm_queueid"] = axm_queueid,
                        ["status"] = finalStatus,
                        ["message"] = "CachedSave processed",
                        ["failed_transactions"] = failedList.ToString(),
                        ["success_transactions"] = successList.ToString()
                    };
                    var dataObjFCM = new JObject
                    {
                        ["axm_queueid"] = axm_queueid,
                        ["status"] = finalStatus,
                        ["message"] = "CachedSave processed",
                        ["failed_transactions"] = failedCount.ToString(),
                        ["success_transactions"] = SuccessCount.ToString()
                    };
                    string sql = "select firebase_id from ax_mobilenotify where username='" + username + "' and projectname='" + project + "'";
                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                    var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
                    string firebaseid = string.Empty;
                    if (result != null && result.Rows.Count > 0)
                    {
                        firebaseid = result.Rows[0]["firebase_id"].ToString();
                    }
                    var payloadObj = new JObject
                    {
                        ["message"] = new JObject
                        {
                            ["token"] = new JArray(firebaseid),
                            ["data"] = dataObj
                        }
                    };
                    var payloadObjFcm = new JObject
                    {
                        ["message"] = new JObject
                        {
                            ["token"] = new JArray(firebaseid),
                            ["data"] = dataObjFCM
                        }
                    };
                    string payloadJsonFcm = JsonConvert.SerializeObject(payloadObjFcm);
                    string payloadJson = JsonConvert.SerializeObject(payloadObj);
                    try
                    {
                        string sqlAm = Constants_SQL.INSERT_TO_AXACTIVEMESG_REQPAYLOAD;
                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        payloadJson = payloadJson.Replace("'", "''");
                        payloadJson = payloadJson.Replace(Environment.NewLine, " | ");
                        sqlAm = string.Format(sqlAm, currentTime, axm_queueid, username, username, "cached save", "cached save", "cached save -" + axm_queueid, "", finalStatus, payloadJson, "", "", "", "e");
                        IDbHelper dbHelperAm = DBHelper.CreateDbHelper(dbType);
                        await dbHelperAm.ExecuteQueryAsync(sqlAm, connectionString);
                    }
                    catch (Exception ex) { }
                    var signalrUrl = _appSettingsHelper.GetInternalResources(project);
                    API _api = new API();
                    ARMResult apiResult = await _api.POSTData(signalrUrl.AxFCMSendMsgURL, payloadJsonFcm, "application/json");
                    if (trace == "true")
                        File.AppendAllText(_exeDir, "SendFCMNotification parse: " + JsonConvert.SerializeObject(apiResult) + " payload:" + JsonConvert.SerializeObject(payloadJson) + Environment.NewLine);
                    if (!string.IsNullOrEmpty(statuskey))
                        DeleteRedisKey(statuskey);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(statuskey))
                        DeleteRedisKey(statuskey);
                    File.AppendAllText(_exeDir, "Error result parse: " + JsonConvert.SerializeObject(results) + " Exception:" + ex + Environment.NewLine);
                }
            }
            return JsonConvert.SerializeObject(results);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(statuskey))
                DeleteRedisKey(statuskey);
            File.AppendAllText(_exeDir, "Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new
            {
                error = ex.Message
            });
        }
    }
    private async Task<string> ProcessMultiSaveAxPut(JObject saveData, string ARMSessionId, string _exeDir, string statuskey)
    {
        try
        {
            List<object> results = new List<object>();
            List<string> axm_recids = new List<string>();
            var savedataToken = saveData["_parameters"]?[0] != null ? saveData["_parameters"]?[0] : (saveData["isaxput"]?.ToString() != null ? saveData : null);
            string project = savedataToken?["project"]?.ToString();
            string username = savedataToken?["username"]?.ToString();
            string sessionId = savedataToken?["sessionid"]?.ToString();
            if (sessionId == null || sessionId == string.Empty)
                sessionId = ARMSessionId;
            string trace = savedataToken?["trace"]?.ToString();

            var _appSettingsService = new AppSettingsService();
            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appName = project;
            _common._appSettingsHelper = _appSettingsHelper;
            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);
            string connectionString = configcon["ConnectionString"];
            string dbType = configcon["DBType"];
            var armSettings = _appSettingsHelper.GetARMSettings(project);
            string ScriptsPath = armSettings.ScriptsPath;
            if (sessionId != null)
                ScriptsPath = Path.Combine(ScriptsPath, "log", sessionId);
            else
                ScriptsPath = Path.Combine(ScriptsPath, "log", "CachedSave");
            if (!Directory.Exists(ScriptsPath))
                Directory.CreateDirectory(ScriptsPath);
            if (ScriptsPath != "")
            {
                string logFileName = "CachedSave-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                _exeDir = Path.Combine(ScriptsPath, logFileName);
            }
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (!redisConnected)
            {
                File.AppendAllText(_exeDir, "Error: Redis not connecting" + Environment.NewLine);
                return JsonConvert.SerializeObject(new { error = "Redis not connecting" });
            }
            string VirtualDownloadFilePath = string.Empty;
            var fileConfig = _appSettingsHelper.GetFileConfig(project);
            if (fileConfig != null)
            {
                VirtualDownloadFilePath = fileConfig.FileDownloadPath;
            }
            string callerid = savedataToken?["callerid"]?.ToString();
            if (callerid == null)
                callerid = "";
            string sequenceno = string.Empty;

            JArray dataArray = savedataToken["data"] as JArray;
            string ArmUrl = _appSettingsHelper.GetARMUrl(project);
            ArmUrl = $"{ArmUrl.TrimEnd('/')}/AxTstructData/api/v1/AxPut";
            if (dataArray != null)
            {
                string strTotMesg = string.Empty;
                int transPros = 0;
                try
                {
                    if (!string.IsNullOrEmpty(statuskey))
                    {
                        int count = dataArray.Children<JObject>().Count();
                        strTotMesg = "Total transactions: " + count;
                    }
                }
                catch (Exception ex) { }

                string ARM_Token = savedataToken["ARMToken"]?.ToString();
                foreach (JObject txnData in dataArray.Children<JObject>())
                {
                    string axm_recid = txnData["axm_recid"]?.ToString();
                    JObject saveDataObject = new JObject
                    {
                        ["ARMSessionId"] = savedataToken["ARMSessionId"]?.ToString(),
                        ["project"] = project,
                        ["username"] = username,
                        ["trace"] = savedataToken["trace"] ?? false,
                        ["validateonly"] = savedataToken["validateonly"] ?? false,
                        ["axclient_dateformat"] = savedataToken["axclient_dateformat"]?.ToString(),
                        ["millisecsintimestamp"] = savedataToken["millisecsintimestamp"] ?? true,
                        ["data"] = new JArray(txnData)
                    };
                    string transid = txnData["transid"]?.ToString();
                    sequenceno = txnData["sequenceno"]?.ToString();
                    string debugJson = JsonConvert.SerializeObject(saveDataObject, Newtonsoft.Json.Formatting.Indented);
                    string result = await CallAxPut(saveDataObject, ARM_Token, ArmUrl, _exeDir);
                    results.Add(JsonConvert.DeserializeObject(result));
                    if (!string.IsNullOrEmpty(statuskey))
                    {
                        transPros++;
                        string strTotMesgNew = Environment.NewLine + "Processed transactions: " + transPros;
                        strTotMesgNew = strTotMesg + strTotMesgNew;
                        SetRedisKey(statuskey, strTotMesgNew);
                    }
                    axm_recids.Add(axm_recid);

                    if (!string.IsNullOrEmpty(callerid) && !string.IsNullOrEmpty(sequenceno))
                    {
                        string _thisresult = result;
                        string existKeyVal = await ReadRedisKeyAsync(project + "-" + callerid + "-" + sequenceno);
                        if (!string.IsNullOrEmpty(existKeyVal))
                            _thisresult += "," + existKeyVal;
                        SetRedisKey(project + "-" + callerid + "-" + sequenceno, _thisresult, 1800);
                    }
                    try
                    {
                        string _keyfield = txnData["keyfield"]?.ToString();
                        string _keyvalue = txnData["keyvalue"]?.ToString();
                        if (!string.IsNullOrEmpty(_keyfield) && !string.IsNullOrEmpty(_keyvalue))
                        {
                            AxSaveAPIResult(_exeDir, dbType, connectionString, callerid, sequenceno, transid, _keyfield, _keyvalue, debugJson, result, username);
                        }
                        else
                        {
                            JObject keyInfo = GetKeyFieldAxPut(result);
                            string _returnField = keyInfo["returnfield"]?.ToString();
                            string _returnFieldVal = keyInfo["returnfieldval"]?.ToString();
                            AxSaveAPIResult(_exeDir, dbType, connectionString, callerid, sequenceno, transid, _returnField, _returnFieldVal, debugJson, result, username);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(_exeDir, "Error: AxSaveAPIResult " + ex.Message + Environment.NewLine);
                    }
                }
            }

            if (results.Count > 0)
            {
                string _dFilePath = string.Empty;
                if (string.IsNullOrEmpty(callerid))
                    _dFilePath = UpdateTextWithResults(VirtualDownloadFilePath, results, _exeDir);
                try
                {
                    StringBuilder msgBuilder = new StringBuilder();
                    var firstItem = JObject.Parse(results[0].ToString());
                    string tstructCap = string.Empty;
                    string transid = string.Empty;


                    string signalrUrl = _appSettingsHelper.GetARMNotificationUrl(project);
                    signalrUrl += "/api/v1/SendSignalR";
                    bool success = false;
                    bool error = false;
                    foreach (var item in results)
                    {
                        JObject obj = JObject.Parse(item.ToString());
                        string _tstructCap = obj["tstructCap"]?.ToString();
                        tstructCap = _tstructCap + ",";
                        transid = obj["transid"]?.ToString() + ",";
                        bool _status = (bool)obj["status"];
                        JObject objmsg = JObject.Parse(obj["msg"].ToString());
                        if (_status && objmsg["result"]["message"].ToString() != "success")
                        {
                            error = true;
                            msgBuilder.Append(_tstructCap + " " + objmsg["result"]["data"]?.ToString() + Environment.NewLine);
                        }
                        else if (_status && objmsg["result"]["message"].ToString() == "success")
                        {
                            success = true;
                            msgBuilder.Append(objmsg["result"]["data"][0]["message"].ToString() + " Recordid:" + objmsg["result"]["data"][0]["recordid"].ToString() + Environment.NewLine);
                        }
                        else if (!_status)
                        {
                            error = true;
                            msgBuilder.Append(_tstructCap + " " + objmsg["result"]["message"].ToString() + Environment.NewLine);
                        }
                    }
                    string finalMsg = msgBuilder.ToString().TrimEnd(',', ' ');
                    string notiTitle = "Multi transaction saved successfully";
                    if (success && error)
                        notiTitle = "Multi Transaction saved successfully with error";
                    else if (!success && error)
                        notiTitle = "Multi Transaction save- error";

                    string _srPayload = JsonConvert.SerializeObject(new[] { new { notifytype = "cachedsave", type = "TStruct", icon = "", title = notiTitle, message = finalMsg, dt = DateTime.Now.ToString(), link = new { t = "e", name = _dFilePath, p = "", act = "open", axmsgid = "" } } });
                    if (!string.IsNullOrEmpty(finalMsg))
                    {
                        _dFilePath = _dFilePath.Replace(@"\", "\\\\");
                        string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG_REQPAYLOAD;
                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        //finalMsg = finalMsg.Replace("'", "''");
                        finalMsg = CheckSpecialChars(finalMsg);
                        finalMsg = finalMsg.Replace(Environment.NewLine, " | ");
                        sql = string.Format(sql, currentTime, "cachedsave", username, username, "cached save", "cached save", "cached save -" + transid, transid, notiTitle, finalMsg, _dFilePath, "", "", "e");


                        IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                        var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                        await SendSignalRMessage(signalrUrl, project, username, _srPayload);
                    }
                    if (!string.IsNullOrEmpty(statuskey))
                        DeleteRedisKey(statuskey);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(statuskey))
                        DeleteRedisKey(statuskey);
                    File.AppendAllText(_exeDir, "Error result parse: " + JsonConvert.SerializeObject(results) + " Exception:" + ex + Environment.NewLine);
                }
            }
            return JsonConvert.SerializeObject(results);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(statuskey))
                DeleteRedisKey(statuskey);
            File.AppendAllText(_exeDir, "Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new
            {
                error = ex.Message
            });
        }
    }

    public string CheckSpecialChars(string str)
    {
        if (str == null)
            str = "";
        str = Regex.Replace(str, "&", "&amp;");
        str = Regex.Replace(str, "<", "&lt;");
        str = Regex.Replace(str, ">", "&gt;");
        str = Regex.Replace(str, "'", "&apos;");
        str = Regex.Replace(str, "\"", "&quot;");
        return str;
    }
    private async Task<string> CallAxPut(JObject saveData, string ARM_Token, string ArmUrl, string logPath)
    {
        try
        {
            string transid = saveData["data"][0]?["transid"]?.ToString();
            string tstructCap = saveData["data"][0]?["tstructcap"]?.ToString();
            if (string.IsNullOrEmpty(tstructCap))
                tstructCap = transid;
            string payload = JsonConvert.SerializeObject(saveData);
            ARMResult apiResult = await POSTDataNew(ArmUrl, payload, "application/json", ARM_Token);
            return JsonConvert.SerializeObject(new
            {
                status = apiResult.result["success"],
                msg = apiResult.result["message"],
                tstructCap = tstructCap,
                transid = transid
            });
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, "Error: " + ex + Environment.NewLine);
            return JsonConvert.SerializeObject(new { error = ex.Message });
        }
    }
    public async Task<ARMResult> POSTDataNew(string url, string body, string mediaType, string ARM_Token)
    {
        try
        {
            using (var cts = new CancellationTokenSource())
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(body, Encoding.UTF8, mediaType);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ARM_Token);
                HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                string content = await response.Content.ReadAsStringAsync();
                return new ARMResult(response.IsSuccessStatusCode, content);
            }
        }
        catch (TaskCanceledException ex)
        {
            return new ARMResult(false, "Request cancelled/timeout: " + ex.Message);
        }
        catch (Exception ex)
        {
            return new ARMResult(false, ex.Message);
        }
    }
    public async void SetRedisKey(string rkey, string rvalue, int expiryTIme = 0)
    {
        var redisConnected = await _common.OpenRedisConnectionAsync();
        if (redisConnected)
        {
            string thisKey = rkey + "-" + "\u2663";
            if (expiryTIme == 0)
                await _common._redisCache.StringSetAsync(thisKey, rvalue);
            else
                await _common._redisCache.StringSetAsync(thisKey, rvalue, expiryTIme);
        }
    }
    public async Task<string> ReadRedisKeyAsync(string rkey)
    {
        var redisConnected = await _common.OpenRedisConnectionAsync();
        if (!redisConnected)
            return null;
        string thisKey = rkey + "-\u2663";
        RedisValue value = await _common._redisCache.StringGetAsync(thisKey);
        return value.HasValue ? value.ToString() : null;
    }

    public async void DeleteRedisKey(string rKey)
    {
        var redisConnected = await _common.OpenRedisConnectionAsync();
        if (redisConnected)
        {
            string thisKey = rKey + "-" + "\u2663";
            await _common._redisCache.KeyDeleteAsync(thisKey);
        }
    }

    private string GetAxRoleRedis(string AxRole)
    {
        string axroles = string.Empty;
        try
        {
            var LstRoles = AxRole.Split(',').OrderBy(x => x).ToList();
            axroles = string.Join(",", LstRoles);
            return axroles;
        }
        catch (Exception ex)
        {
            return axroles;
        }
    }

    private string GetDataBase(string connectionString, string dbType, Dictionary<string, string> configcon)
    {
        string dbName = string.Empty;
        Dictionary<string, string> connectionStringParts = new Dictionary<string, string>();
        string[] parts = connectionString.Split(';');
        if (dbType.ToLower() == "postgresql" || dbType.ToLower() == "postgre")
        {
            if (configcon.ContainsKey("Driver") && configcon["Driver"] == "ado")
            {
                dbName = configcon["DBUser"];
            }
            else
            {
                dbName = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : string.Empty;
                if (dbName == string.Empty)
                    dbName = "axpertdb";
            }
        }
        return dbName;
    }

    private async void AxSaveAPIResult(string _exeDir, string dbType, string connectionString, string CallerId, string CallSequence, string Transid, string Keyfield, string KeyValue, string InputJSON, string ResultString, string RequestedBy)
    {
        try
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string inputJsonStr = InputJSON.Replace("'", "''");
            //string resultStr = ResultString.Replace("'", "''");
            string resultStr = CheckSpecialChars(ResultString);
            resultStr = resultStr.Replace(Environment.NewLine, " | ");
            string sql = string.Empty;
            if (dbType.ToUpper() == "ORACLE")
                sql = $@"INSERT INTO AxSaveAPIResult(CallerId,CallSequence,Transid,Keyfield,KeyValue,InputJSON,ResultString,RequestDatetime,RequestedBy) VALUES('{CallerId}','{CallSequence}','{Transid}',{(string.IsNullOrEmpty(Keyfield) ? "NULL" : $"'{Keyfield}'")},{(string.IsNullOrEmpty(KeyValue) ? "NULL" : $"'{KeyValue}'")},'{inputJsonStr}','{resultStr}', TO_TIMESTAMP('{currentTime}', 'YYYY-MM-DD HH24:MI:SS.FF3'),'{RequestedBy}');";
            else
                sql = $@"INSERT INTO AxSaveAPIResult(CallerId,CallSequence,Transid,Keyfield,KeyValue,InputJSON,ResultString,RequestDatetime,RequestedBy) VALUES('{CallerId}','{CallSequence}','{Transid}',{(string.IsNullOrEmpty(Keyfield) ? "NULL" : $"'{Keyfield}'")},{(string.IsNullOrEmpty(KeyValue) ? "NULL" : $"'{KeyValue}'")},'{inputJsonStr}','{resultStr}','{currentTime}','{RequestedBy}');";
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
        }
        catch (Exception ex)
        {
            File.AppendAllText(_exeDir, "Error AxSaveAPIResult: Exception:" + ex + Environment.NewLine);
        }
    }
    private JObject GetKeyField(string resjson)
    {
        try
        {
            JObject obj = JObject.Parse(resjson);
            string msgJson = obj["msg"]?.ToString();
            if (string.IsNullOrWhiteSpace(msgJson))
                return new JObject();
            JObject msgObj = JObject.Parse(msgJson);
            string recordId = msgObj["result"]?[0]?["message"]?[0]?["recordid"]?.ToString();
            if (string.IsNullOrWhiteSpace(recordId))
                return new JObject();
            string returnField = string.Empty;
            string returnFieldVal = string.Empty;
            foreach (var part in recordId.Split(','))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length == 2)
                {
                    if (kv[0] == "returnfield")
                        returnField = kv[1];
                    else if (kv[0] == "returnfieldval")
                        returnFieldVal = kv[1];
                }
            }
            if (string.IsNullOrEmpty(returnField) && string.IsNullOrEmpty(returnFieldVal))
                return new JObject();
            return new JObject
            {
                ["returnfield"] = returnField,
                ["returnfieldval"] = returnFieldVal
            };
        }
        catch
        {
            return new JObject
            {
                ["returnfield"] = "",
                ["returnfieldval"] = ""
            };
        }
    }

    private JObject GetKeyFieldAxPut(string resjson)
    {
        try
        {
            JObject obj = JObject.Parse(resjson);
            string msgJson = obj["msg"]?.ToString();
            if (string.IsNullOrWhiteSpace(msgJson))
            {
                return new JObject
                {
                    ["returnfield"] = "",
                    ["returnfieldval"] = ""
                };
            }
            JObject msgObj = JObject.Parse(msgJson);
            JToken data = msgObj["result"]?["data"]?[0];
            return new JObject
            {
                ["returnfield"] = data?["returnfield"]?.ToString() ?? "",
                ["returnfieldval"] = data?["returnfieldval"]?.ToString() ?? ""
            };
        }
        catch
        {
            return new JObject
            {
                ["returnfield"] = "",
                ["returnfieldval"] = ""
            };
        }
    }
}
