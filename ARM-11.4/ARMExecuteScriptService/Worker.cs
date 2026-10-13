using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using ARMCommon.Interface;
using Org.BouncyCastle.Asn1.Ocsp;
using Newtonsoft.Json.Linq;
using ARMCommon.Services;
using System.IO.Pipelines;
using static Org.BouncyCastle.Math.EC.ECCurve;

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IRabbitMQConsumer _consumer;
    private readonly Utils _common;

    public Worker(IConfiguration config, IRabbitMQConsumer consumer, Utils common)
    {
        _config = config;
        _consumer = consumer;
        _common = common;
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
                    return await ExecuteScriptApi(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "ExecuteScriptApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{ex}\n\n");
                    return "error";
                }
            }
            _consumer.DoConsumeWorkerService(queueName, OnConsuming);
            //_consumer.DoConsume(queueName, OnConsuming);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "ExecuteScriptApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            _exeDir = Path.Combine(_exeDir, _logFileName);
            File.AppendAllText(_exeDir, $"{ex}\n\n");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
    private void Write(string msg)
    {
        Console.WriteLine($"{DateTime.Now} - {msg}");
    }
    private void OnProcessExit(object sender, EventArgs e)
    {
        try
        {
            var context = new DataContext(_config);
            Assembly assembly = Assembly.GetExecutingAssembly();

            string serviceName = Process.GetCurrentProcess().ProcessName;
            string exeName = assembly.Location;
            string exePath = Path.GetDirectoryName(exeName);
            string hostName = Dns.GetHostName();
            IPAddress ipv4 = Dns.GetHostAddresses(hostName).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            int pid = Process.GetCurrentProcess().Id;
            var appConfig = _config.GetSection("AppConfig").Get<Dictionary<string, object>>();
            string otherInfo = JsonConvert.SerializeObject(appConfig);

            var log = new ARMServiceLogs()
            {
                ServiceName = serviceName,
                Server = ipv4?.ToString(),
                Folder = exePath,
                InstanceID = pid,
                OtherInfo = otherInfo,
                Status = "Stopped",
                LastOnline = DateTime.Now,
                StartOnTime = File.GetLastWriteTime(exeName),
                IsMailSent = true
            };

            context.Add(log);
            context.SaveChanges();

            //Write("Service Stopped.");
        }
        catch (Exception ex)
        {
            Write("Error on service stop: " + ex.Message);
        }
    }
    private async Task<string> ExecuteScriptApi(string request)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string _logFileName = "ExecuteScriptApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, _logFileName);

        var scriptObj = new Dictionary<string, Dictionary<string, string>>();
        var executeScriptObj = new Dictionary<string, string>();

        JObject jsonObjExcel = JObject.Parse(request);
        string DATA = jsonObjExcel["queuedata"]?.ToString();
        JObject jsonObjExcelData = JObject.Parse(DATA);

        if (jsonObjExcelData["executescript"] != null)
        {
            string projectName = jsonObjExcelData["executescript"]["project"].ToString();
            string s = jsonObjExcelData["executescript"]["s"].ToString();
            var _appSettingsService = new AppSettingsService();
            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appName = projectName;
            _common._appSettingsHelper = _appSettingsHelper;
            string signalrUrl = _appSettingsHelper.GetARMUrl(projectName) + "/ARM_APIs/api/v1/SendSignalR";
            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(projectName);
            string connectionString = configcon["ConnectionString"];
            string dbType = configcon["DBType"];
            var armSettings = _appSettingsHelper.GetARMSettings(projectName);
            string ScriptsPath = armSettings.ScriptsPath;
            if (s != null)
                ScriptsPath += "log\\" + s + "\\";
            else
                ScriptsPath = Path.Combine(ScriptsPath, "log", "ExecuteScript");
            if (!Directory.Exists(ScriptsPath))
                Directory.CreateDirectory(ScriptsPath);
            if (ScriptsPath != "")
            {
                string logFileName = "ExecuteScriptApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                _exeDir = Path.Combine(ScriptsPath, logFileName);
            }

            string username = jsonObjExcelData["executescript"]["username"].ToString();
            string scriptcaption = jsonObjExcelData["executescript"]["scriptcaption"].ToString();
            string formName = jsonObjExcelData["executescript"]["name"].ToString();
            string scriptqueuename = jsonObjExcelData["executescript"]["scriptqueuename"].ToString();
            bool trace = jsonObjExcelData["executescript"]["trace"].ToString() == "true" ? true : false;

            if (trace)
            {
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
                File.AppendAllText(_exeDir, "ExecuteScriptApi start: ");
            }

            Dictionary<string, string> connectionStringParts = new Dictionary<string, string>();
            string[] parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    string[] keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        connectionStringParts[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }
            string _RKey = string.Empty;
            if (dbType.ToLower() == "postgresql" || dbType.ToLower() == "postgre")
            {
                if (configcon.ContainsKey("Driver") && configcon["Driver"] == "ado")
                {
                    string _DBUser = configcon["DBUser"];
                    _RKey = _DBUser + "-" + formName + "-execScript-" + username + "-" + scriptqueuename;
                }
                else
                {
                    string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                    string dbusername = connectionStringParts.ContainsKey("Username") ? connectionStringParts["Username"] : (connectionStringParts.ContainsKey("Uid") ? connectionStringParts["Uid"] : "Not found");
                    _RKey = dbusername + "~" + database + "-" + formName + "-execScript-" + username + "-" + scriptqueuename;
                }
            }
            else if (dbType.ToLower() == "oracle" || dbType.ToLower() == "ms sql" || dbType.ToLower() == "mssql")
            {
                string database = connectionStringParts.ContainsKey("User Id") ? connectionStringParts["User Id"] : "Not found";
                _RKey = database + "-" + formName + "-execScript-" + username + "-" + scriptqueuename;
            }
            else
            {
                string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                _RKey = database + "-" + formName + "-execScript-" + username + "-" + scriptqueuename;
            }

            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                await _common._redisCache.StringSetAsync(_RKey, "requestinprocess");
            }
            else
            {
                if (trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
                    File.AppendAllText(_exeDir, "Redis connection is not proper.");
                }
            }

            executeScriptObj["project"] = projectName;
            string scriptName = jsonObjExcelData["executescript"]["script"].ToString();
            string pageCaption = jsonObjExcelData["executescript"]["pageCaption"].ToString();


            AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(username);
            UpdateOrAddJsonKey(ref jsonObjExcelData, "userauthkey", axpertRestAPIToken.userAuthKey);
            UpdateOrAddJsonKey(ref jsonObjExcelData, "seed", axpertRestAPIToken.seed);
            UpdateOrAddJsonKey(ref jsonObjExcelData, "token", axpertRestAPIToken.token);

            string armScriptsURL = _common.GetARMScriptsURL(projectName);
            string apiUrl = armScriptsURL + "/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/executescript";

            string apiInput = JsonConvert.SerializeObject(jsonObjExcelData);
            var api = new API();
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            APIResult finalResult = _common.ParseAxpertRestAPIResult(apiResult);
            try
            {
                var jsonString = JsonConvert.SerializeObject(finalResult);
                var obj = JsonConvert.DeserializeObject<JObject>(jsonString);
                string restresult = "";
                if (!string.IsNullOrEmpty(obj["error"]?.ToString()))
                    restresult = obj["error"]?.ToString();
                else
                    restresult = obj["data"]?["result"]?.ToString();
                //restresult = restresult?.Replace("'" + scriptName + "'", "'" + scriptcaption + "'") ?? "";
                //restresult = restresult?.Replace("'", "") ?? "";
                string rawMessage = restresult;
                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(restresult);
                    var msg = json["message"]?[0]?["msg"]?.ToString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        rawMessage = msg;
                    }
                }
                catch
                {
                }
                if (rawMessage.Contains("`"))
                {
                    var _parts = rawMessage.Split('`');
                    if (_parts.Length > 1)
                        rawMessage = _parts[1];
                }
                rawMessage = rawMessage.Replace("'" + scriptName + "'", "'" + scriptcaption + "'");
                rawMessage = rawMessage.Replace("'", "");
                rawMessage = rawMessage.Trim();
                restresult = rawMessage;

                string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG;
                string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                sql = string.Format(sql, currentTime, "Execute Script Push to Queue", username, username, "executescript", "Execute Script Push to Queue", "ScriptName-" + scriptName, formName, pageCaption, restresult, "");
                if (trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
                    File.AppendAllText(_exeDir, "Inser Query:" + sql);
                }
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
                if (trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
                    File.AppendAllText(_exeDir, "Connection String:" + connectionString);
                    File.AppendAllText(_exeDir, "DBType:" + dbType);
                }
                string _Payload = "[{\"notifytype\":\"Execute Script Push to Queue\",\"type\":\"executescript\",\"icon\":\"\",\"title\":\"Execute Script Push to Queue\",\"message\":\"" + restresult + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                if (redisConnected)
                {
                    await _common._redisCache.KeyDeleteAsync(_RKey);
                }
                SendSignalRMessage(signalrUrl, projectName, username, _Payload);
                Console.WriteLine("After notification:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                return "Success";
            }
            catch (Exception ex)
            {
                if (redisConnected)
                {
                    await _common._redisCache.KeyDeleteAsync(_RKey);
                }
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
                File.AppendAllText(_exeDir, $"After rest call {ex}\n\n");

                string _Payload = "[{\"notifytype\":\"Execute Script Push to Queue - error\",\"type\":\"executescript\",\"icon\":\"\",\"title\":\"Execute Script Push to Queue - error\",\"message\":\"" + ex.Message + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                SendSignalRMessage(signalrUrl, projectName, username, _Payload);
            }
            return JsonConvert.SerializeObject(finalResult);
        }
        else
        {
            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            //exeDir = Path.Combine(exeDir, "ExecuteScriptApi.log");
            File.AppendAllText(_exeDir, "ExecuteScriptPushToQueue input is not proper.");
            return "error";
        }
    }
    static void UpdateOrAddJsonKey(ref JObject jsonObject, string key, string value)
    {
        if (jsonObject["executescript"] is JObject executescript)
        {
            executescript[key] = value;
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
            //Console.WriteLine("SignalR URL:" + signalrUrl);
            //Console.WriteLine("SignalR messgage:" + JsonConvert.SerializeObject(singalRMessage));
        }
    }
}
