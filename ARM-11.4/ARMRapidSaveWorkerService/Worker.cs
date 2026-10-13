using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using ARMCommon.Interface;
using Newtonsoft.Json.Linq;
using ARMCommon.Services;
using System.Net.Http;
using System.Text;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using System.Configuration;

public class Worker : BackgroundService
{
    private static readonly HttpClient httpClient = new HttpClient();
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
                    return await RapidSaveWorkerService(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "RapidSaveWorkerService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{ex}\n\n");
                    return "error";
                }
            }
            _consumer.DoConsumeWorkerService(queueName, OnConsuming);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "RapidSaveWorkerService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            _exeDir = Path.Combine(_exeDir, _logFileName);
            File.AppendAllText(_exeDir, $"{ex}\n\n" + Environment.NewLine);
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
        }
        catch (Exception ex)
        {
            //Write("Error on service stop: " + ex.Message);
        }
    }
    private async Task<string> RapidSaveWorkerService(string request)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string _logFileName = "RapidSaveWorkerService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, _logFileName);

        JObject saveObj = JObject.Parse(request);
        string queueData = string.Empty;
        JObject saveData;
        queueData = GetTokenIgnoreCase(saveObj, "queuedata")?.ToString();
        if (queueData != null)
        {
            string queueDataEscape = queueData.Replace("\r\n", "").Trim('"');
            saveData = JsonConvert.DeserializeObject<JObject>(queueDataEscape);
        }
        else
        {
            saveData = saveObj;
        }
        JObject rapidsave = (JObject)saveData["payload"]?["rapidsave"];
        if (rapidsave != null && saveData != null)
        {
            try
            {
                string project = rapidsave["project"]?.ToString();
                string s = rapidsave["s"].ToString();
                string sttrace = rapidsave["trace"].ToString();
                bool _trace = sttrace == "true" ? true : false;

                var _appSettingsService = new AppSettingsService();
                var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                _common._appName = project;
                _common._appSettingsHelper = _appSettingsHelper;
                var armSettings = _appSettingsHelper.GetARMSettings(project);
                string ScriptsPath = armSettings.ScriptsPath;
                if (s != null)
                    ScriptsPath += "log\\" + s + "\\";
                else
                    ScriptsPath = Path.Combine(ScriptsPath, "log", "RapidSaveImport");
                if (!Directory.Exists(ScriptsPath))
                    Directory.CreateDirectory(ScriptsPath);
                if (ScriptsPath != "")
                {
                    string logFileName = "RapidSaveWorkerService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                    _exeDir = Path.Combine(ScriptsPath, logFileName);
                }


                string signalrUrl = saveData["signalrurl"].ToString();
                string username = rapidsave["username"]?.ToString();
                string impFileWithPath = rapidsave["impfilewithpath"]?.ToString();
                string impFileWithSummary = rapidsave["impfilewithsummary"]?.ToString();
                string apiDesc = rapidsave["apidesc"]?.ToString();
                string mapcolumns = rapidsave["mapcolumns"]?.ToString();
                string hasheader = rapidsave["hasheader"]?.ToString();
                string xlsheetnames = rapidsave["xlsheetnames"]?.ToString();
                string method = saveData["method"]?.ToString();
                string submitdataapiurl = saveData["url"]?.ToString();

                bool trace = rapidsave["trace"].ToString() == "true" ? true : false;

                if (trace && username == "")
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                    File.AppendAllText(_exeDir, "Error: 'username' is required. " + Environment.NewLine);
                }

                JObject resultJson = new JObject
                {
                    ["rapidsave"] = new JObject
                    {
                        ["project"] = rapidsave["project"],
                        ["s"] = rapidsave["s"],
                        ["userauthkey"] = rapidsave["userauthkey"],
                        ["token"] = rapidsave["token"],
                        ["seed"] = rapidsave["seed"],
                        ["transid"] = rapidsave["transid"],
                        ["keyfield"] = rapidsave["keyfield"],
                        ["returnfailedrecords"] = rapidsave["returnfailedrecords"],
                        ["trace"] = rapidsave["trace"],
                        ["impfilename"] = rapidsave["impfilename"],
                        ["impfilewithpath"] = impFileWithPath,
                        ["impfilewithsummary"] = impFileWithSummary,
                        ["impreccount"] = rapidsave["impreccount"],
                        ["isxlimport"] = rapidsave["isxlimport"],
                        ["dataarray"] = rapidsave["dataarray"],
                        ["impinitiatedon"] = rapidsave["impinitiatedon"],
                        ["xlimportbatchid"] = rapidsave["xlimportbatchid"],
                        ["xlimportbatchcount"] = rapidsave["xlimportbatchcount"],
                        ["xlimportbatchtotal"] = rapidsave["xlimportbatchtotal"],
                        ["mapcolumns"] = rapidsave["mapcolumns"],
                        ["hasheader"] = rapidsave["hasheader"],
                        ["xlsheetnames"] = rapidsave["xlsheetnames"]
                    },
                    ["axprops"] = saveData["payload"]?["axprops"].ToString(),
                    ["axapps"] = saveData["payload"]?["axapps"].ToString(),
                    ["globalvars"] = saveData["payload"]?["globalvars"].ToString(),
                    ["uservars"] = saveData["payload"]?["uservars"].ToString()
                };

                string payload = JsonConvert.SerializeObject(resultJson, Formatting.Indented);
                string mediaType = "application/json";
                API _api = new API();
                string _errorMessage = "";
                try
                {
                    ApiRequest apiRequest = new ApiRequest();
                    apiRequest.StartTime = DateTime.Now;
                    ARMResult apiResult;
                    if (method.ToUpper() == "POST")
                    {
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                            File.AppendAllText(_exeDir, "url called: " + submitdataapiurl + Environment.NewLine);
                            File.AppendAllText(_exeDir, "payload called: " + payload + Environment.NewLine);
                        }
                        apiResult = await _api.POSTData(submitdataapiurl, payload, mediaType);
                    }
                    else
                    {
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                            File.AppendAllText(_exeDir, "url called: " + submitdataapiurl + Environment.NewLine);
                            File.AppendAllText(_exeDir, "payload called: " + payload + Environment.NewLine);
                        }
                        apiResult = await _api.GetData(submitdataapiurl);
                    }
                    try
                    {
                        if (apiResult != null && (apiResult.result["message"].ToString().IndexOf("configured HttpClient.Timeout") > -1 || apiResult.result["message"].ToString().IndexOf("Gateway Time-out") > -1))
                            _errorMessage = "configured HttpClient.Timeout";
                    }
                    catch (Exception) { }
                    if (trace)
                    {
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                        File.AppendAllText(_exeDir, "API Result: " + JsonConvert.SerializeObject(apiResult) + Environment.NewLine);
                    }
                    var result = JsonConvert.SerializeObject(ParseAxpertRestAPIResult(apiResult));
                    if (trace)
                    {
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                        File.AppendAllText(_exeDir, "After API Result parse: " + result + Environment.NewLine);
                    }
                    apiRequest.Project = project;
                    apiRequest.Url = submitdataapiurl;
                    apiRequest.Method = method;
                    apiRequest.RequestString = JsonConvert.SerializeObject(new { submitdata = payload });
                    if (Convert.ToBoolean(apiResult.result["success"]))
                    {
                        apiRequest.Status = "Success";
                    }
                    else
                    {
                        apiRequest.Status = "Fail";
                    }
                    apiRequest.Response = apiResult.result["message"].ToString();
                    apiRequest.EndTime = DateTime.Now;
                    apiRequest.APIDesc = apiDesc;

                    if (string.IsNullOrEmpty(apiRequest.Project))
                    {
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                            File.AppendAllText(_exeDir, $"Error: Project details are missing in JSON. Can't write logs to 'axapijobdetails' table." + Environment.NewLine);
                        }
                        string errorMsg = $"Error: Project details are missing in JSON. Can't write logs to 'axapijobdetails' table.";
                        string _Payload = "[{\"notifytype\":\"rapidsave Push to Queue - error\",\"type\":\"rapidsave\",\"icon\":\"\",\"title\":\"RapidSave Push to Queue rest api - error\",\"message\":\"" + errorMsg + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                        SendSignalRMessage(signalrUrl, project, username, _Payload);
                    }
                    else
                    {
                        await LogAPICall(apiRequest);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    var errResult = JsonConvert.SerializeObject(ex);
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                    File.AppendAllText(_exeDir, $"Error: {errResult}" + Environment.NewLine);

                    if (_errorMessage == "")
                    {
                        string errorMsg = $"Error: Data import is failed, please check the trace for proper error information.";
                        string _Payload = "[{\"notifytype\":\"rapidsave Push to Queue - error\",\"type\":\"rapidsave\",\"icon\":\"\",\"title\":\"RapidSave Push to Queue rest api - error\",\"message\":\"" + errorMsg + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                        SendSignalRMessage(signalrUrl, project, username, _Payload);
                    }
                    return "error";
                }
            }
            catch (Exception ex)
            {
                var errResult = JsonConvert.SerializeObject(ex);
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
                File.AppendAllText(_exeDir, $"Error: {errResult}" + Environment.NewLine);
                return "error";
            }
        }
        else
        {
            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            //exeDir = Path.Combine(exeDir, "RapidSaveWorkerService.log");
            File.AppendAllText(_exeDir, "RapidSaveWorkerService input is not proper." + Environment.NewLine);
            return "error";
        }
    }

    async Task<bool> LogAPICall(ApiRequest apiRequest)
    {
        var _appSettingsService = new AppSettingsService();
        var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
        Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(apiRequest.Project);
        string connectionString = configcon["ConnectionString"];
        string dbType = configcon["DBType"];
        try
        {
            string sql = Constants_SQL.INSERT_TO_APILOG;
            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            if (dbType == "oracle")
            {
                string oraclecurrentTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string startTime = apiRequest.StartTime?.ToString("yyyy-MM-dd HH:mm:ss");
                string endTime = apiRequest.EndTime?.ToString("yyyy-MM-dd HH:mm:ss");

                sql = string.Format(@"INSERT INTO axapijobdetails (jobid, requestid, url, method, requeststr, params, header, responsestr, status, starttime, endtime, context, servicename) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', {9}, {10}, '{11}', '{12}')",
                    oraclecurrentTime,
                    oraclecurrentTime,
                    apiRequest.Url,
                    apiRequest.Method,
                    apiRequest.RequestString,
                    apiRequest.ParameterString,
                    apiRequest.HeaderString,
                    apiRequest.Response,
                    apiRequest.Status,
                    startTime != null ? $"TO_DATE('{startTime}', 'YYYY-MM-DD HH24:MI:SS')" : "NULL",
                    endTime != null ? $"TO_DATE('{endTime}', 'YYYY-MM-DD HH24:MI:SS')" : "NULL",
                    "RMQ",
                    apiRequest.APIDesc ?? "ARMCachedSaveService"
                );
            }

            else
            {
                sql = string.Format(sql, currentTime, currentTime, apiRequest.Url, apiRequest.Method,
                    apiRequest.RequestString, apiRequest.ParameterString, apiRequest.HeaderString,
                    apiRequest.Response, apiRequest.Status,
                    apiRequest.StartTime?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    apiRequest.EndTime?.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    "RMQ", apiRequest.APIDesc ?? "ARMCachedSaveService");
            }
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    static APIResult ParseAxpertRestAPIResult(ARMResult armResult)
    {
        APIResult result = new APIResult();
        result.data.Add("success", armResult.result["success"]);
        JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
        if (!(resultJsonObj["status"]?.ToString().ToLower() == "true" || resultJsonObj["status"]?.ToString().ToLower() == "success"))
        {
            result.error = resultJsonObj["result"].ToString();
            return result;
        }
        foreach (var property in resultJsonObj.Properties())
        {
            if (property.Name.ToLower() != "status")
                result.data.Add(property.Name, property.Value);
        }
        return result;
    }
    static JToken GetTokenIgnoreCase(JObject jObject, string propertyName)
    {
        var property = jObject.Properties().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        return property?.Value;
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
}

