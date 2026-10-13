using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;


namespace ARMServices
{
    class Program
    {
        private static ServiceLog _serviceLog;
        private static Timer timer;
        public static async Task Main(string[] args)
        {

            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            dynamic obj = JsonConvert.DeserializeObject(json);
            string queueName = obj.AppConfig["QueueName"];
            string signalrUrl = obj.AppConfig["SignalRURL"];
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
            var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();

            var context = new DataContext(configuration);
            _serviceLog = new ServiceLog(context, configuration);


            await _serviceLog.LogServiceStartedAsync();
            TimeSpan interval = TimeSpan.FromMinutes(1);

            timer = new Timer(Execute, null, TimeSpan.Zero, interval);

            async Task<string> OnConsuming(string message)
            {
                try
                {

                    Console.WriteLine("OnConsuming method called with message " + message);

                    // Parse the incoming JSON message
                    JObject saveObj = JObject.Parse(message);
                    string queueData = string.Empty;
                    string armResponseQueue = string.Empty;
                    string axResponseQueue = string.Empty;
                    string url = string.Empty;
                    string method = string.Empty;
                    JObject saveData;
                    queueData = GetTokenIgnoreCase(saveObj, "queuedata")?.ToString();
                    JObject queueDataObj = JObject.Parse(queueData);



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

                    if (rapidsave == null)
                    {
                        Console.WriteLine("rapidsave object missing.");
                        return string.Empty;
                    }


                    string impFileWithPath =
                        rapidsave["impfilewithpath"]?.ToString();
                    string impFileWithSummary = rapidsave["impfilewithsummary"]?.ToString();
                    string project = saveData["payload"]?["rapidsave"]?["project"]?.ToString();
                    string apiDesc = saveData["payload"]?["rapidsave"]?["apidesc"]?.ToString();
                    string mapcolumns = saveData["payload"]?["rapidsave"]?["mapcolumns"]?.ToString();
                    string hasheader = saveData["payload"]?["rapidsave"]?["hasheader"]?.ToString();
                    string xlsheetnames = saveData["payload"]?["rapidsave"]?["xlsheetnames"]?.ToString();

                    JObject resultJson = new JObject
                    {
                        ["rapidsave"] = new JObject
                        {
                            ["project"] = rapidsave["project"],
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

                        }
                    };

                    string payload = JsonConvert.SerializeObject(resultJson, Formatting.Indented);
                    string mediaType = "application/json";
                    API _api = new API();
                    try
                    {
                        ApiRequest apiRequest = new ApiRequest();
                        apiRequest.StartTime = DateTime.Now;
                        if (string.IsNullOrEmpty(obj.AppConfig?["URL"]?.ToString()))
                        {
                            url = obj.AppConfig["URL"];
                            method = obj.AppConfig["METHOD"];
                        }

                        if (string.IsNullOrEmpty(url))
                        {
                            url = queueDataObj["url"]?.ToString();
                            method = queueDataObj["method"]?.ToString();
                        }

                        ARMResult apiResult;
                        if (method.ToUpper() == "POST")
                        {
                            Console.WriteLine("url called: " + url);
                            Console.WriteLine("payload called: " + payload);
                            apiResult = await _api.POSTData(url, payload, mediaType);

                        }
                        else
                        {
                            Console.WriteLine("url called: " + url);
                            Console.WriteLine("payload called: " + payload);
                            apiResult = await _api.GetData(url);
                        }

                        var result = JsonConvert.SerializeObject(ParseAxpertRestAPIResult(apiResult));
                        if (!string.IsNullOrEmpty(armResponseQueue))
                        {
                            WriteMessage(" sending message to armresponsequeue" + armResponseQueue);
                            Console.WriteLine("payload called" + payload);
                            rabbitMQProducer.SendMessages(result, armResponseQueue);
                        }
                        if (!string.IsNullOrEmpty(axResponseQueue))
                        {
                            Console.WriteLine(" sending message to axresponsequeue" + axResponseQueue);
                            rabbitMQProducer.SendMessages(result, axResponseQueue);
                        }

                        WriteMessage(result);
                        apiRequest.Project = project;
                        apiRequest.Url = url;
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

                        await LogAPICall(apiRequest);
                        //Task logResult = LogAPICall(apiRequest);
                        //Task.WaitAll(logResult);
                        return result;



                    }

                    catch (Exception ex)
                    {
                        var errResult = JsonConvert.SerializeObject(ex);
                        WriteMessage(errResult);
                        return errResult;
                    }

                }
                catch (Exception ex)
                {
                    var errResult = JsonConvert.SerializeObject(ex);
                    WriteMessage(errResult);
                    return ex.Message;
                }



            }



            static void WriteMessage(string message)
            {
                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
            }




            static JToken GetTokenIgnoreCase(JObject jObject, string propertyName)
            {
                // Find the property in a case-insensitive manner
                var property = jObject.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                return property?.Value;
            }

            static void RemoveJsonKey(ref JObject jsonObject, string key)
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

            static void UpdateOrAddJsonKey(ref JObject jsonObject, string key, JToken value)
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


            async Task<bool> LogAPICall(ApiRequest apiRequest)
            {
                if (string.IsNullOrEmpty(apiRequest.Project))
                {
                    Console.WriteLine("Project details are missing in JSON. Can't write logs to 'axapijobdetails' table.");
                    return false;
                }

                var context = new ARMCommon.Helpers.DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                try
                {
                    Dictionary<string, string> config = await utils.GetDBConfigurations(apiRequest.Project);
                    if (!config.TryGetValue("ConnectionString", out string connectionString) ||
                        !config.TryGetValue("DBType", out string dbType))
                    {
                        Console.WriteLine("Configuration missing for DB connection.");
                        return false;
                    }

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
                    //WriteMessage("SQL:" + sql);
                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                    var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }


            rabbitMQConsumer.DoConsume(queueName, OnConsuming);

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;


        }


        static void WriteMessage(string message)
        {
            Console.WriteLine(DateTime.Now.ToString() + " - " + message);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var configuration = builder.Build();
                var context = new DataContext(configuration);
                Assembly assembly = Assembly.GetExecutingAssembly();
                string serviceName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                string exeName = assembly.Location;
                string exePath = Path.GetDirectoryName(exeName);
                string hostName = Dns.GetHostName();
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
                IPAddress ipv4Address = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var appConfigSection = configuration.GetSection("AppConfig");
                int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var otherInfoValue = JsonConvert.SerializeObject(appConfigSection.Get<Dictionary<string, object>>());
                DateTime lastDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
                if (DateTime.Now - lastDate > TimeSpan.FromMinutes(5))
                {
                    var newLog = new ARMServiceLogs()
                    {
                        ServiceName = serviceName,
                        Server = ipv4Address?.ToString(),
                        Folder = exePath,
                        InstanceID = processId,
                        OtherInfo = otherInfoValue,
                        Status = "Stopped",
                        LastOnline = DateTime.Now,
                        StartOnTime = lastDate,
                        IsMailSent = true
                         
                    };

                    context.Add(newLog);
                    context.SaveChanges();
                    WriteMessage("Service Stopped " + DateTime.Now);
                    timer?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on service stop: {ex.Message}");
            }
        }


        private static async void Execute(object state)
        {
            try
            {

                var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var configuration = builder.Build();
                var context = new DataContext(configuration);
                Assembly assembly = Assembly.GetExecutingAssembly();
                string serviceName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                string exeName = assembly.Location;
                string exePath = Path.GetDirectoryName(exeName);
                string hostName = Dns.GetHostName();
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
                IPAddress ipv4Address = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var appConfigSection = configuration.GetSection("AppConfig");
                int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var otherInfoValue = JsonConvert.SerializeObject(appConfigSection.Get<Dictionary<string, object>>());
                DateTime lastDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
                var newLog = new ARMServiceLogs()
                {
                    ServiceName = serviceName,
                    Server = ipv4Address?.ToString(),
                    Folder = exePath,
                    InstanceID = processId,
                    OtherInfo = otherInfoValue,
                    Status = "Running",
                    LastOnline = DateTime.Now,
                    StartOnTime = lastDate,
                    IsMailSent = true
                };

                context.Add(newLog);
                await context.SaveChangesAsync();
                WriteMessage("Service Running " + DateTime.Now);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting executable information: {ex.Message}");
            }
            static void WriteMessage(string message)
            {
                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
            }
        }
    }

    public class APIResult
    {
        public string error { get; set; }
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }
}
