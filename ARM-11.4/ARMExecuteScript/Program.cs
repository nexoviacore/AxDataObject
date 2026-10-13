using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Text;

namespace ARMExecuteScript
{
    internal class Program
    {
        private static Timer timer;
        static void Main(string[] args)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
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


            async Task<string> OnConsuming(string message)
            {
                try
                {
                    WriteMessage("OnConsuming method called with message " + message);

                    JObject obje = JObject.Parse(message);
                    string armResponseQueue = string.Empty;
                    string queueData = GetTokenIgnoreCase(obje, "queuedata")?.ToString();

                    if (queueData != null)
                    {
                        JObject obj = JObject.Parse(queueData);
                        string Project = GetTokenIgnoreCase(obj, "project")?.ToString();
                        string Script = GetTokenIgnoreCase(obj, "script")?.ToString();
                        string Type = GetTokenIgnoreCase(obj, "type")?.ToString();
                        string Name = GetTokenIgnoreCase(obj, "name")?.ToString();
                        string RecordID = GetTokenIgnoreCase(obj, "recordid")?.ToString();
                        string Trace = GetTokenIgnoreCase(obj, "trace")?.ToString();
                        string UserName = GetTokenIgnoreCase(obj, "username")?.ToString();
                        string inputAPIURL = GetTokenIgnoreCase(obj, "apiurl")?.ToString();
                        string APIUrl = configuration["AppConfig:apiurl"];

                        AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(UserName);
                        var Seed = axpertRestAPIToken.seed.ToString();
                        var Token = axpertRestAPIToken.token.ToString();
                        var authkey = axpertRestAPIToken.userAuthKey.ToString();
                        var response = SubmitApi(APIUrl, Name, Project, Script, Type, RecordID, Trace, Token, Seed, authkey);
                    }

                    return "";

                }

                catch (Exception ex)
                {
                    var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                    WriteMessage(errResult);
                    return ex.Message;
                }


            }


            static async Task<ApiResponse> SubmitApi(string APIUrl, string Name, string Project, string Script, string Type, string RecordID, string Trace, string Token, string Seed, string authkey)
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    //client.DefaultRequestHeaders.Connection.Add("Keep-Alive");
                    var payload = new
                    {
                        executescript = new
                        {
                            project = Project,
                            token = Token,
                            seed = Seed,
                            userauthkey = authkey,
                            script = Script,
                            type = Type,
                            name = Name,
                            recordid = RecordID,
                            trace = Trace
                        }
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    Console.WriteLine($"Payload: {jsonPayload}");

                    var jsonContent = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(APIUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("API Result:" + responseContent.ToString());
                        return new ApiResponse(true, responseContent);

                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(errorContent.ToString());
                        return new ApiResponse(false, errorContent);
                    }
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

            static void WriteMessage(string message)
            {
                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
            }

            static JToken GetTokenIgnoreCase(JObject jObject, string propertyName)
            {

                var property = jObject.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                return property?.Value;
            }


            rabbitMQConsumer.DoConsume(queueName, OnConsuming);

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        }
    }
}
public class ApiResponse
{
    public bool Success { get; }
    public string Content { get; }

    public ApiResponse(bool success, string content)
    {
        Success = success;
        Content = content;
    }
}