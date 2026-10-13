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
                    return await ConvertXLtoJSONWorkerServiceApi(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "ConvertXLtoJsonApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{ex}\n\n" + Environment.NewLine);
                    return "error";
                }
            }
            _consumer.DoConsumeWorkerService(queueName, OnConsuming);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "ConvertXLtoJsonApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
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

            Write("Service Stopped.");
        }
        catch (Exception ex)
        {
            Write("Error on service stop: " + ex.Message);
        }
    }
    private async Task<string> ConvertXLtoJSONWorkerServiceApi(string request)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string _logFileName = "ConvertXLtoJsonApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, _logFileName);

        JObject jsonObj = JObject.Parse(request);
        string DATA = jsonObj["queuedata"]?.ToString();
        JObject jsonObjConvertxltojson = JObject.Parse(DATA);

        if (jsonObjConvertxltojson["convertxltojson"] != null)
        {
            string project = jsonObjConvertxltojson["convertxltojson"]["project"].ToString();
            string s = jsonObjConvertxltojson["convertxltojson"]["s"].ToString();
            string trace = jsonObjConvertxltojson["convertxltojson"]["trace"].ToString();
            bool _trace = jsonObjConvertxltojson["convertxltojson"]["trace"].ToString() == "true" ? true : false;

            var _appSettingsService = new AppSettingsService();
            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appName = project;
            _common._appSettingsHelper = _appSettingsHelper;
            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);
            string connectionString = configcon["ConnectionString"];
            string dbType = configcon["DBType"];

            var armSettings = _appSettingsHelper.GetARMSettings(project);
            string ScriptsPath = armSettings.ScriptsPath;
            if (s != null)
                ScriptsPath += "log\\" + s + "\\";
            else
                ScriptsPath = Path.Combine(ScriptsPath, "log", "convertxltojson");
            if (!Directory.Exists(ScriptsPath))
                Directory.CreateDirectory(ScriptsPath);
            if (ScriptsPath != "")
            {
                string logFileName = "ConvertXLtoJsonApi-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                _exeDir = Path.Combine(ScriptsPath, logFileName);
            }

            string redisserver = jsonObjConvertxltojson["convertxltojson"]["redisserver"].ToString();
            string redisport = jsonObjConvertxltojson["convertxltojson"]["redisport"].ToString();
            string redispwd = jsonObjConvertxltojson["convertxltojson"]["redispwd"].ToString();
            string transid = jsonObjConvertxltojson["convertxltojson"]["transid"].ToString();
            string keyfield = jsonObjConvertxltojson["convertxltojson"]["keyfield"].ToString();
            string importfile = jsonObjConvertxltojson["convertxltojson"]["importfile"].ToString();
            string sheetnames = jsonObjConvertxltojson["convertxltojson"]["sheetnames"].ToString();
            string mapcolumns = jsonObjConvertxltojson["convertxltojson"]["mapcolumns"].ToString();
            string hasheader = jsonObjConvertxltojson["convertxltojson"]["hasheader"].ToString();
            string userName = jsonObjConvertxltojson["convertxltojson"]["username"].ToString();
            string ARMSessionId = jsonObjConvertxltojson["convertxltojson"]["ARMSessionId"].ToString();

            string ARMToken = jsonObjConvertxltojson["convertxltojson"]["ARMToken"].ToString();
            string dataupdate = jsonObjConvertxltojson["convertxltojson"]["dataupdate"].ToString();
            string primarykey = jsonObjConvertxltojson["convertxltojson"]["primarykey"].ToString();

            string signalrUrl = jsonObjConvertxltojson["convertxltojson"]["signalrurl"].ToString();
            string apiUrl = jsonObjConvertxltojson["convertxltojson"]["armscripturl"].ToString();
            string isaxput = jsonObjConvertxltojson["convertxltojson"]?["isaxput"]?.ToString() == "True" ? "true" : "false";
            string axprops = jsonObjConvertxltojson["axprops"].ToString();
            string axapps = jsonObjConvertxltojson["axapps"].ToString();
            string globalvars = jsonObjConvertxltojson["globalvars"].ToString();
            string uservars = jsonObjConvertxltojson["uservars"].ToString();

            if (userName == "")
            {
                if (_trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
                    File.AppendAllText(_exeDir, "Error: 'username' is required. " + Environment.NewLine);
                }
                return "error";
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
                    _RKey = _DBUser + "-" + transid + "-gblImport-" + userName + "-AxConvertXLtoJSONQueue";
                }
                else
                {
                    string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                    string dbusername = connectionStringParts.ContainsKey("Username") ? connectionStringParts["Username"] : (connectionStringParts.ContainsKey("Uid") ? connectionStringParts["Uid"] : "Not found");
                    _RKey = dbusername + "~" + database + "-" + transid + "-gblImport-" + userName + "-AxConvertXLtoJSONQueue";
                }
            }
            else if (dbType.ToLower() == "oracle" || dbType.ToLower() == "ms sql" || dbType.ToLower() == "mssql")
            {
                string database = connectionStringParts.ContainsKey("User Id") ? connectionStringParts["User Id"] : "Not found";
                _RKey = database + "-" + transid + "-gblImport-" + userName + "-AxConvertXLtoJSONQueue";
            }
            else
            {
                string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                _RKey = database + "-" + transid + "-gblImport-" + userName + "-AxConvertXLtoJSONQueue";
            }

            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                await _common._redisCache.StringSetAsync(_RKey, "requestinprocess");
            }
            else
            {
                if (_trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
                    File.AppendAllText(_exeDir, "Redis connection is not proper." + Environment.NewLine);
                }
            }
            AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(userName);
            var seed = axpertRestAPIToken.seed.ToString();
            var token = axpertRestAPIToken.token.ToString();
            var authKey = axpertRestAPIToken.userAuthKey.ToString();
            var requestJson = new
            {
                convertxltojson = new
                {
                    redisserver,
                    redisport,
                    redispwd,
                    project,
                    s,
                    username = userName,
                    ARMSessionId,
                    ARMToken,
                    userauthkey = authKey,
                    token,
                    seed,
                    transid,
                    isaxput,
                    dataupdate,
                    primarykey,
                    keyfield,
                    importfile,
                    sheetnames,
                    trace,
                    mapcolumns,
                    hasheader
                },
                axprops = axprops,
                axapps = axapps,
                globalvars = globalvars,
                uservars = uservars
            };
            string jsonPayload = JsonConvert.SerializeObject(requestJson, Formatting.Indented);
            string response = await CallApi(apiUrl, jsonPayload, _trace, signalrUrl, project, userName, _exeDir);
            JObject jsonResponse = JObject.Parse(response);
            string apiresponse = jsonResponse["result"]?.ToString();
            if (_trace)
            {
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
                File.AppendAllText(_exeDir, "Api Response:" + apiresponse + Environment.NewLine);
            }
            if (redisConnected)
            {
                await _common._redisCache.KeyDeleteAsync(_RKey);
            }
            return "Success";
        }
        else
        {
            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
            File.AppendAllText(_exeDir, "ARMConvertXLtoJSONWorkerService input is not proper." + Environment.NewLine);
            return "error";
        }
    }
    async Task<string> CallApi(string url, string jsonPayload, bool _trace, string signalrUrl, string project, string userName, string _exeDir)
    {
        try
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(url, content);

            string responseBody = await response.Content.ReadAsStringAsync();
            if (_trace)
            {
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
                File.AppendAllText(_exeDir, "API Response: " + responseBody + Environment.NewLine);
            }
            if (responseBody != "")
            {
                JObject jsonObj = JObject.Parse(responseBody);
                if (jsonObj?["status"] != null && jsonObj?["status"]?.ToString().ToLower() != "true")
                {
                    string errorMsg = $"Error: Data import is failed, please check the trace for proper error information.";
                    string _Payload = "[{\"notifytype\":\"Import Push to Queue - error\",\"type\":\"globalimport\",\"icon\":\"\",\"title\":\"Global Import Push to Queue rest api - error\",\"message\":\"" + errorMsg + " Exception:" + responseBody + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                    SendSignalRMessage(signalrUrl, project, userName, _Payload);
                }
            }
            return $"Error: {response.StatusCode}, Response: {responseBody}";
        }
        catch (Exception ex)
        {
            if (_trace)
            {
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ConvertXLtoJsonApi.log");
                File.AppendAllText(_exeDir, $"API Call Failed: {ex.Message}, URL:{url}, payload:{jsonPayload}" + Environment.NewLine);
            }
            string errorMsg = $"Error: Data import is failed, please check the trace for proper error information.";
            string _Payload = "[{\"notifytype\":\"Import Push to Queue - error\",\"type\":\"globalimport\",\"icon\":\"\",\"title\":\"Global Import Push to Queue rest api - error\",\"message\":\"" + errorMsg + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
            SendSignalRMessage(signalrUrl, project, userName, _Payload);
            return $"Exception: {ex.Message}";
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
}

