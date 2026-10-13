using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using Hangfire.PostgreSql;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

using EasyNetQ.Management.Client.Model;

namespace ARMAxpertJobs
{

    class Program
    {
        private static Timer timer;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static ServiceLog _serviceLog;
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();


            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);

            dynamic config = JsonConvert.DeserializeObject(json);
            string queueName = config.AppConfig["QueueName"];
            string apiUrl = config.AppConfig["APIURL"];
            string method = "POST";


            var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var appConfigSection = configuration.GetSection("AppConfig");
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

            serviceCollection.AddHangfireServer();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
            var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();
            var context = new DataContext(configuration);
            _serviceLog = new ServiceLog(context, configuration);


            await _serviceLog.LogServiceStartedAsync();


            TimeSpan interval = TimeSpan.FromMinutes(1);
            timer = new Timer(Execute, null, TimeSpan.Zero, interval);

            host.RunAsync();

            async Task<string> OnConsuming(string message)
            {
                try
                {
                    Console.WriteLine("OnConsuming called with message: " + message);

                    JObject jobObj = JObject.Parse(message);
                    string queueData = GetTokenIgnoreCase(jobObj, "QueueJson")?.ToString();

                    if (queueData != null)
                    {
                        string queueDataEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                        jobObj = JsonConvert.DeserializeObject<JObject>(queueDataEscape);
                    }

                    string startDate = GetTokenIgnoreCase(jobObj, "start_date")?.ToString();
                    string period = GetTokenIgnoreCase(jobObj, "period")?.ToString();
                    string sendTime = GetTokenIgnoreCase(jobObj, "sendtime")?.ToString();
                    string sendOn = GetTokenIgnoreCase(jobObj, "sendon")?.ToString();
                    string project = GetTokenIgnoreCase(jobObj, "project")?.ToString();
                    string jobName = GetTokenIgnoreCase(jobObj, "jobname")?.ToString();
                    string userName = GetTokenIgnoreCase(jobObj, "username")?.ToString();
                    string jobRedisKey = GetTokenIgnoreCase(jobObj, "jobrediskey")?.ToString();
                    string jobRedisKeyVal = GetTokenIgnoreCase(jobObj, "jobrediskeyval")?.ToString();
                    string JobID = GetTokenIgnoreCase(jobObj, "jobid")?.ToString();
                    string modifiedOn = GetTokenIgnoreCase(jobObj, "modifiedon")?.ToString();
                    string isActive = GetTokenIgnoreCase(jobObj, "isactive")?.ToString();

                    DateTime parsedDate;
                    string formattedDate = null;

                    if (DateTime.TryParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                    {
                        formattedDate = parsedDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        Console.WriteLine("Invalid date format for start_date");
                        return "Invalid date format";
                    }

                    var timeJson = new
                    {
                        startDate = formattedDate,
                        period = period,
                        sendTime = sendTime,
                        sendOn = sendOn,
                        firstDayOfWeek = config.AppConfig["FirstDayOfWeek"].ToString()
                    };

                    var scheduler = new AxpertScheduler();
                    var nextOccurrence = scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson));

                    // **Delete existing jobs with the same JobID before scheduling**
                    

                    TimeSpan timeDiff = nextOccurrence - DateTime.Now;
                    if (timeDiff < TimeSpan.Zero) timeDiff = TimeSpan.Zero; // Ensure no negative delay

                    if (string.IsNullOrEmpty(modifiedOn))
                    {
                        var dt = await GetJobModifiedOn(project, jobName);
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            Console.WriteLine($"Job '{jobName}' is not available in project '{project}'. Message is discarded.");
                            return "";
                        }

                        jobRedisKey = dt.Rows[0][0].ToString();
                        jobRedisKeyVal = dt.Rows[0][1].ToString();
                        JobID = dt.Rows[0][2].ToString();
                        modifiedOn = dt.Rows[0][3].ToString();
                        isActive = dt.Rows[0][4].ToString();



                        UpdateOrAddJsonKey(ref jobObj, "jobrediskey", jobRedisKey);
                        UpdateOrAddJsonKey(ref jobObj, "jobrediskeyval", jobRedisKeyVal);
                        UpdateOrAddJsonKey(ref jobObj, "jobid", JobID);
                        UpdateOrAddJsonKey(ref jobObj, "modifiedon", modifiedOn);
                        UpdateOrAddJsonKey(ref jobObj, "isactive", isActive);
                    }

                    await DeleteExistingJobs(JobID);
                    var res1 = await ScheduleJob(jobObj, jobRedisKey, jobRedisKeyVal, JobID, project, isActive);
                    return res1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in OnConsuming: " + ex.Message);
                    return $"Error in OnConsuming: {ex.Message}";
                }
            }

            rabbitMQConsumer.DoConsume(queueName, OnConsuming);


            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            



           



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

            static JToken GetTokenIgnoreCase(JObject jObject, string propertyName)
            {
                var property = jObject.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                return property?.Value;
            }

            async Task<DataTable> GetJobModifiedOn(string appName, string jobName)
            {
                try
                {
                    var context = new ARMCommon.Helpers.DataContext(configuration);
                    var redis = new RedisHelper(configuration);
                    var utils = new Utils(configuration, context, redis);
                    Dictionary<string, string> config = await utils.GetDBConfigurations(appName);
                    string connectionString = config["ConnectionString"];
                    string dbType = config["DBType"];
                    string sql = Constants_SQL.GET_AXPERTJOBSDETAILS;
                    string[] paramNames = { "@jobname" };
                    DbType[] paramTypes = { DbType.String };
                    object[] paramValues = { jobName.ToLower() };
                    IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in GetJobModifiedOn: " + ex.Message);
                    return null;
                }
            }
        }

        private static APIResult ParseAxpertRestAPIResult(ARMResult armResult)
        {
            var result = new APIResult();
            var resultJson = JObject.Parse(armResult.result["message"].ToString());

            if (!(resultJson["status"]?.ToString().ToLower() == "true" || resultJson["status"]?.ToString().ToLower() == "success"))
            {
                result.error = resultJson["result"].ToString();
                return result;
            }

            foreach (var property in resultJson.Properties())
            {
                if (property.Name.ToLower() != "status")
                    result.data.Add(property.Name, property.Value);
            }

            return result;
        }

        public static async Task<string> ScheduleJob(JObject jobObj, string jobRedisKey, string jobRedisKeyVal, string JobID, string project, string isactive)
        {
            try
            {

                
                if (isactive == "F")
                {
                    await DeleteExistingJobs(JobID);
                    return $"Deleted job(s) with JobId: {JobID}";
                }

                string startDate = jobObj["start_date"]?.ToString();
                string period = jobObj["period"]?.ToString();
                string sendTime = jobObj["sendtime"]?.ToString();
                string sendOn = jobObj["sendon"]?.ToString();
                string jobname = jobObj["jobname"]?.ToString();

                if (string.IsNullOrEmpty(startDate))
                {
                    Console.WriteLine("Missing or invalid start_date");
                    return "Invalid start_date";
                }

                if (!DateTime.TryParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    Console.WriteLine("Invalid date format for start_date");
                    return "Invalid start_date format";
                }

                string formattedDate = parsedDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                var timeJson1 = new
                {
                    startDate = formattedDate,
                    period,
                    sendTime,
                    sendOn
                };

                var scheduler = new AxpertScheduler();
                var nextOccurrence = scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson1));
                TimeSpan timeDiff = nextOccurrence - DateTime.Now;
                BackgroundJob.Schedule(() => ExecuteJob(jobRedisKey, jobRedisKeyVal, JobID, project, isactive, formattedDate, period, sendTime, sendOn), timeDiff);

                WriteMessage($"Job '{jobname}' scheduled for: {nextOccurrence}");
                return $"Job '{jobname}' scheduled";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ScheduleJob: {ex}");
                return "Error in ScheduleJob";
            }
        }

        public static async Task ExecuteJob(string jobRedisKey, string jobRedisKeyVal, string JobID, string project, string isactive, string formattedDate, string period, string sendTime, string sendOn)
        {
            try
            {
                string mediaType = "application/json";
                API _api = new API();
                ARMResult apiResult;

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var configuration = builder.Build();
                var context = new ARMCommon.Helpers.DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                string AxpertWebScriptsURL = configuration[$"ConnectionStrings:{project}_scriptsurl"];
                if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                    AxpertWebScriptsURL = await utils.AxpertWebScriptsURL(project);

                string apiUrl = $"{AxpertWebScriptsURL}/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/AxpertJobsAPI";

                var payload = new
                {
                    axpertjobsapi = new
                    {
                        id = JobID,
                        jobid = jobRedisKey,
                        jobdata = jobRedisKeyVal,
                        trace = "f",
                        JOBID = JobID,
                        ISACTIVE = isactive
                    }
                };

                apiResult = await _api.POSTData(apiUrl, JsonConvert.SerializeObject(payload), mediaType);
                var result = JsonConvert.SerializeObject(ParseAxpertRestAPIResult(apiResult));

                WriteMessage($"API URL: {apiUrl}");
                WriteMessage($"Payload: {JsonConvert.SerializeObject(payload)}");
                WriteMessage($"API Result: {result}");


                var timeJson1 = new
                {
                    startDate = formattedDate,
                    period,
                    sendTime,
                    sendOn
                };

                var scheduler = new AxpertScheduler();
                var nextOccurrence = scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson1));

               
                 TimeSpan timeDiff = nextOccurrence - DateTime.Now;
                    BackgroundJob.Schedule(() => ExecuteJob(jobRedisKey, jobRedisKeyVal, JobID, project, isactive, formattedDate, period, sendTime, sendOn), timeDiff);
                WriteMessage("Next Job Scheduled at:" + nextOccurrence);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExecuteJob: {ex}");
            }
        }

        
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHangfire(config =>
                    {
                        var postgresConnectionString = context.Configuration.GetConnectionString("WebApiDatabase");
                        config.UsePostgreSqlStorage(postgresConnectionString);
                    });
                })
                .Configure(app =>
                {
                    var dashboardUrl = app.ApplicationServices.GetRequiredService<IConfiguration>()
                .GetSection("AppConfig:HangfireDashboardUrl").Value;

                    app.UseHangfireDashboard(dashboardUrl, new DashboardOptions
                    {

                    });
                    app.UseHangfireServer();
                });


        static void WriteMessage(string message)
        {
            Console.WriteLine(DateTime.Now.ToString() + " - " + message);
        }
        public static async Task<string> DeleteExistingJobs(string JobID)
        {
            try
            {
                var monitor = JobStorage.Current?.GetMonitoringApi();
                if (monitor == null)
                {
                    return "Job monitor is not available.";
                }

                var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue);
                var jobsToDelete = new List<string>();

                foreach (var job in scheduledJobs)
                {
                    if (job.Value.Job.Args.Any(arg => arg?.ToString() == JobID))
                    {
                        jobsToDelete.Add(job.Key);
                    }
                }

                // Delete all found jobs
                foreach (var jobKey in jobsToDelete)
                {
                    BackgroundJob.Delete(jobKey);
                }

                return jobsToDelete.Count > 0
                    ? $"Deleted {jobsToDelete.Count} scheduled job(s) with JobId: {JobID}"
                    : $"No scheduled jobs found with JobId: {JobID}";
            }
            catch (Exception ex)
            {
                return $"Error deleting scheduled jobs: {ex.Message}";
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
        }

        //static async Task<string> CallApiAsync(string apiUrl)
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync(apiUrl);
        //        response.EnsureSuccessStatusCode();
        //        var responseBody = await response.Content.ReadAsStringAsync();
        //        Console.WriteLine(responseBody);
        //        return responseBody;
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        Console.WriteLine($"Request exception: {e.Message}");
        //        return null;
        //    }
        //}

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





    }
}

public class APIResult
{
    public string error { get; set; }
    public Dictionary<string, object> data = new Dictionary<string, object>();
}


