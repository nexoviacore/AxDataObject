using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using ARMCommon.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace ConvertXLToJson
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static ServiceLog _serviceLog;
        private static Timer timer;

        public static async Task Main(string[] args)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            dynamic config = JsonConvert.DeserializeObject(json);
            string queueName = config.AppConfig["QueueName"];

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

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
                    WriteMessage("OnConsuming method called with message: " + message);
                    JObject obje = JObject.Parse(message);
                    string queuedataString = obje["queuedata"]?.ToString();
                   

                    if (string.IsNullOrEmpty(queuedataString))
                    {
                        return "Error: Missing 'queuedata' in JSON.";
                    }

                    JObject queueDataObj = JObject.Parse(queuedataString);
                    JObject obj = queueDataObj["convertxltojson"]?.ToObject<JObject>();

                    if (obj == null)
                    {
                        return "Error: Missing 'convertxltojson' in JSON.";
                    }
                    

                    string redisserver = obj.Value<string>("redisserver");
                    string redisport = obj.Value<string>("redisport");
                    string redispwd = obj.Value<string>("redispwd");
                    string project = obj.Value<string>("project");
                    string transid = obj.Value<string>("transid");
                    string keyfield = obj.Value<string>("keyfield");
                    string importfile = obj.Value<string>("importfile");
                    string sheetnames = obj.Value<string>("sheetnames");
                    string trace = obj.Value<string>("trace");
                    string mapcolumns = obj.Value<string>("mapcolumns");
                    string hasheader = obj.Value<string>("hasheader");
                    string userName = obj.Value<string>("username");
                    string signalrUrl = obj.Value<string>("signalrurl");
                    string apiUrl = obj.Value<string>("armscripturl"); 



                    if (string.IsNullOrEmpty(userName))
                    {
                        return "Error: 'username' is required.";
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
                            userauthkey = authKey,
                            token,
                            seed,
                            transid,
                            keyfield,
                            importfile,
                            sheetnames,
                            trace,
                            mapcolumns,
                            hasheader
                        }
                    };

                    string jsonPayload = JsonConvert.SerializeObject(requestJson, Formatting.Indented);
                    WriteMessage("API URL:" + apiUrl);
                    WriteMessage("Payload: " + jsonPayload);
                    string response = await CallApi(apiUrl, jsonPayload);
                    JObject jsonResponse = JObject.Parse(response);
                    string apiresponse = jsonResponse["result"]?.ToString();

                   //tring _Payload = "[{\"notifytype\":\"Import excel\",\"type\":\"Import\",\"icon\":\"\",\"title\":\"Import Excel\",\"message\":\"" + apiresponse + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"" + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

                    //ait SendSignalRMessage(project, userName, _Payload, signalrUrl);

                    return response;
                }
                catch (JsonException jsonEx)
                {
                    return "JSON Parsing Error: " + jsonEx.Message;
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }



            rabbitMQConsumer?.DoConsume(queueName, OnConsuming);

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        static void WriteMessage(string message)
        {
            Console.WriteLine($"{DateTime.Now} - {message}");
        }

        async static Task<string> CallApi(string url, string jsonPayload)
        {
            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    WriteMessage("API Response: " + responseBody);
                    return responseBody;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    WriteMessage($"API Error: {response.StatusCode}, Response: {errorResponse}");
                    return $"Error: {response.StatusCode}, Response: {errorResponse}";
                }
            }
            catch (Exception ex)
            {
                WriteMessage("API Call Failed: " + ex.Message);
                return $"Exception: {ex.Message}";
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
                if (DateTime.Now - lastDate > TimeSpan.FromMinutes(2))
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

        //async static Task SendSignalRMessage(string project, string clientId, string message,string signalrUrl)
        //{
        //    if (!string.IsNullOrEmpty(clientId))
        //    {
        //        var singalRMessage = new
        //        {
        //            project = project,
        //            UserId = clientId,
        //            Message = message
        //        };
        //        API _api = new API();
        //        ARMResult armresult = await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
        //        WriteMessage("SignalR URL:" + signalrUrl);
        //        WriteMessage("SignalR messgage:" + JsonConvert.SerializeObject(singalRMessage));

        //    }
        //}
    }
}
