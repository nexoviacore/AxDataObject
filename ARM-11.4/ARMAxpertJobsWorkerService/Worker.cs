using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using ARMCommon.Interface;
using ARMCommon.Services;
using System.Data;
using ARMCommon.Model;
using RabbitMQ.Client;
using System.Runtime.InteropServices;
using System.Collections;
using System.Globalization;
using System.Security.Policy;

namespace ARMAxpertJobsWorkerService
{
    public class Worker : BackgroundService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly IConfiguration _config;
        private readonly IRabbitMQConsumer _consumer;
        private readonly Utils _common;
        private readonly RedisJobStore _redis;
        public ArrayList appList = new ArrayList();

        public AppSettingsService _appSettingsService;
        public AppSettingsHelper _appSettingsHelper;

        public Worker(IConfiguration config, IRabbitMQConsumer consumer, Utils common, RedisJobStore redis)
        {
            _config = config;
            _consumer = consumer;
            _common = common;
            _redis = redis;
            _appSettingsService = new AppSettingsService();
            _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appSettingsHelper = _appSettingsHelper;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RestoreJobsFromDatabase();

            //_ = Task.Run(() => PeriodicDbRefresh(stoppingToken), stoppingToken);

            //_ = Task.Run(() => RunDailyCleanupAsync(stoppingToken), stoppingToken);

            string queueName = _config["AppConfig:QueueName"];
            _consumer.DoConsumeWorkerServiceJob(queueName, OnConsuming);
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (string accessCode in appList)
                {
                    var config = _appSettingsHelper.GetRedisSettings(accessCode);
                    string redisConnString = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;

                    while (true)
                    {
                        string? jobId = await _redis.PopDueJob(accessCode, redisConnString);
                        if (string.IsNullOrEmpty(jobId))
                            break;

                        _ = Task.Run(() => ExecuteScheduledJob(accessCode, redisConnString, jobId), stoppingToken);
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        private async Task RestoreJobsFromDatabase()
        {
            try
            {
                //foreach (var appName in _appSettingsService._appSettings)
                foreach (var appName in _appSettingsService.AppSettingsConfig.AppSettings)
                {
                    string accessCode = appName.Key;
                    _common._appName = accessCode;

                    var config = _appSettingsHelper.GetRedisSettings(accessCode);
                    string redisConnectionString = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;
                    await _redis.RemoveAllJobs(accessCode, redisConnectionString, "*");
                    var dt = await GetAllActiveJobs(accessCode);
                    if (dt == null || dt.Rows.Count == 0)
                        continue;
                    try
                    {
                        appList.Add(accessCode);
                        foreach (DataRow row in dt.Rows)
                        {
                            string jobId = row["jobid"]?.ToString();
                            if (string.IsNullOrEmpty(jobId))
                                continue;

                            string payload = BuildJobPayloadFromRow(row);
                            if (string.IsNullOrEmpty(payload))
                                continue;

                            JObject jobObj = JObject.Parse(payload);
                            var next = CalculateNextOccurrence(jobObj);

                            //Console.WriteLine($"Next UTC : {next:dd/MM/yyyy hh:mm:ss tt}");
                            //Console.WriteLine($"Next IST : {next.ToLocalTime():dd/MM/yyyy hh:mm:ss tt}");

                            bool exists = await _redis.JobExists(accessCode, redisConnectionString, jobId);
                            bool scheduled = await _redis.IsScheduled(accessCode, redisConnectionString, jobId);

                            if (!exists)
                            {
                                await _redis.SaveJob(accessCode, redisConnectionString, jobId, payload);
                            }

                            if (!scheduled)
                            {
                                await _redis.Schedule(accessCode, redisConnectionString, jobId, next);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                        Directory.CreateDirectory(_exeDir);
                        string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                        _exeDir = Path.Combine(_exeDir, logFileName);
                        File.AppendAllText(_exeDir, "Restore error: accessCode: " + accessCode + " Exception:" + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Restore error: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
        }
        private async Task PeriodicDbRefresh(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

                    //Console.WriteLine("Refreshing jobs from DB...");

                    await RestoreJobsFromDatabase();

                    //Console.WriteLine("DB refresh completed.");
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("DB refresh error: " + ex.Message);
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, logFileName);
                    File.AppendAllText(_exeDir, "DB refresh error: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                }
            }
        }
        private async Task<string> OnConsuming(string message)
        {
            JObject jobObj = JObject.Parse(message);
            string project = jobObj["queuejson"]["project"]?.ToString();
            string jobName = jobObj["queuejson"]["jobname"]?.ToString();
            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(jobName))
                return "Invalid message";
            var config = _appSettingsHelper.GetRedisSettings(project);
            string redisConnString = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;
            var dt = await GetJobModifiedOn(project, jobName);
            if (dt == null || dt.Rows.Count == 0)
            {
                string _jobid = jobObj["queuejson"]?["jobid"]?.ToString();
                if (!string.IsNullOrEmpty(_jobid))
                    await _redis.Remove(project, redisConnString, _jobid);
                return "Job not found";
            }
            string jobId = dt.Rows[0]["jobid"]?.ToString();
            string redisKey = dt.Rows[0]["rediskeyname"]?.ToString();
            string redisVal = dt.Rows[0]["rediskeyval"]?.ToString();
            string modifiedOn = dt.Rows[0]["modifiedon"]?.ToString();
            string isActive = dt.Rows[0]["isactive"]?.ToString();

            jobObj["jobid"] = jobId;
            jobObj["rediskeyname"] = redisKey;
            jobObj["rediskeyval"] = redisVal;
            jobObj["modifiedon"] = modifiedOn;
            jobObj["isactive"] = isActive;

            var next = CalculateNextOccurrence(jobObj);

            //Console.WriteLine($"Next1 UTC : {next:dd/MM/yyyy hh:mm:ss tt}");
            //Console.WriteLine($"Next1 IST : {next.ToLocalTime():dd/MM/yyyy hh:mm:ss tt}");

            if (!appList.Contains(project))
                appList.Add(project);

            await _redis.Remove(project, redisConnString, jobId);
            if (isActive == "T")
            {
                await _redis.SaveJob(project, redisConnString, jobId, jobObj.ToString());
                await _redis.Schedule(project, redisConnString, jobId, next);
            }
            else
            {
                await _redis.RemoveJobData(project, redisConnString, jobId);
            }
            return "Scheduled";
        }
        private async Task ExecuteScheduledJob(string accessCode, string redisConnection, string jobId)
        {
            try
            {
                string payload = await _redis.GetJob(accessCode, redisConnection, jobId);
                if (string.IsNullOrEmpty(payload))
                    return;
                JObject jobObj = JObject.Parse(payload);
                if (jobObj["isactive"]?.ToString() == "F")
                {
                    await _redis.RemoveJobData(accessCode, redisConnection, jobId);
                    return;
                }
                await CallAxpertJobAPI(accessCode, jobObj);
                var next = CalculateNextOccurrence(jobObj);

                //Console.WriteLine($"Next2 UTC : {next:dd/MM/yyyy hh:mm:ss tt}");
                //Console.WriteLine($"Next2 IST : {next.ToLocalTime():dd/MM/yyyy hh:mm:ss tt}");

                if (next > DateTime.UtcNow)
                {
                    await _redis.Schedule(accessCode, redisConnection, jobId, next);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(
                //    $"Execution error (Access: {accessCode}, Job: {jobId}): {ex.Message}");
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, $"Execution error (Access: {accessCode}, Job: {jobId}): {ex.Message}" + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
        }
        private async Task CallAxpertJobAPI(string accessCode, JObject jobObj)
        {
            try
            {
                string project = accessCode;
                if (jobObj["queuejson"] != null)
                    project = jobObj["queuejson"]["project"]?.ToString();
                string jobId = jobObj["jobid"]?.ToString();
                string redisKey = jobObj["rediskeyname"]?.ToString();
                string redisVal = jobObj["rediskeyval"]?.ToString();
                string isActive = jobObj["isactive"]?.ToString();

                if (isActive == "F")
                    return;
                string _trace = _config["AppConfig:trace"];
                if (!string.IsNullOrEmpty(_trace) && _trace == "true")
                    _trace = "t";
                else
                    _trace = "f";
                string baseUrl = await _common.AxpertWebScriptsURL(project);
                string proj_BaseRestPath = _config["AppConfig:" + project + "_ASBScriptRest_URL"];
                string selectedUrl = !string.IsNullOrEmpty(proj_BaseRestPath) ? proj_BaseRestPath : baseUrl;
                string apiUrl = $"{selectedUrl.TrimEnd('/')}/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/AxpertJobsAPI";

                var payload = new
                {
                    axpertjobsapi = new
                    {
                        id = jobId,
                        jobid = redisKey,
                        jobdata = redisVal,
                        trace = _trace,
                        JOBID = jobId,
                        ISACTIVE = isActive
                    }
                };

                var api = new API();
                await api.POSTData(apiUrl, JsonConvert.SerializeObject(payload), "application/json");
            }
            catch (Exception ex)
            {
                //Console.WriteLine(
                //   $"Execution error (Access: {accessCode}, Job: {jobObj}): {ex.Message}");
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, $"Execution error (Access: {accessCode}, Job: {jobObj}): {ex.Message}" + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
        }
        private DateTime CalculateNextOccurrence(JObject jobObj)
        {
            var scheduler = new JobScheduler();
            DateTime nextLocal;
            if (jobObj["queuejson"] != null)
            {
                var _startDate = jobObj["queuejson"]["start_date"]?.ToString();
                if (!string.IsNullOrWhiteSpace(_startDate))
                {
                    DateTime dateValue;
                    if (!DateTime.TryParse(_startDate.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
                    {
                        //throw new Exception($"Invalid start date: {_startDate}");
                    }
                    _startDate = dateValue.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                var timeJson = new
                {
                    startDate = _startDate,// jobObj["queuejson"]["start_date"]?.ToString(),
                    period = jobObj["queuejson"]["period"]?.ToString(),
                    sendTime = jobObj["queuejson"]["sendtime"]?.ToString(),
                    sendOn = jobObj["queuejson"]["sendon"]?.ToString()
                };
                nextLocal = scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson));
            }
            else
            {
                var _startDate = jobObj["start_date"]?.ToString();
                if (!string.IsNullOrWhiteSpace(_startDate))
                {
                    DateTime dateValue;
                    if (!DateTime.TryParse(_startDate.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
                    {
                        //throw new Exception($"Invalid start date: {_startDate}");
                    }
                    _startDate = dateValue.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                var timeJson = new
                {
                    startDate = _startDate,// jobObj["start_date"]?.ToString(),
                    period = jobObj["period"]?.ToString(),
                    sendTime = jobObj["sendtime"]?.ToString(),
                    sendOn = jobObj["sendon"]?.ToString()
                };
                nextLocal = scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson));
            }
            DateTime nextUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), TimeZoneInfo.Local);

            return nextUtc;
        }
        private async Task<DataTable> GetAllActiveJobs(string appName)
        {
            try
            {
                _common._appName = appName;
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(appName);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];

                string sql = Constants_SQL.GET_AXPERTJOBSDETAILS_ACTIVE;
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                return await dbHelper.ExecuteQueryAsync(sql, connectionString);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error in GetAllActiveJobs: " + ex.Message);
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Error in GetAllActiveJobs: appName: " + appName + " Exception:" + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                return null;
            }
        }

        private string BuildJobPayloadFromRow(DataRow row)
        {
            if (row == null)
                return null;

            JObject jobObj = new JObject();

            //string GetValue(string col) =>
            //    row.Table.Columns.Contains(col) ? row[col]?.ToString() : null;
            string GetValue(string col)
            {
                if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value)
                    return null;
                var value = row[col];
                if (value is DateTime dt)
                    return dt.ToString("dd/MM/yyyy");
                return value.ToString();
            }

            jobObj["jobid"] = GetValue("jobid");
            jobObj["rediskeyname"] = GetValue("rediskeyname");
            jobObj["rediskeyval"] = GetValue("rediskeyval");
            jobObj["modifiedon"] = GetValue("modifiedon");
            jobObj["isactive"] = GetValue("isactive");
            jobObj["start_date"] = GetValue("jobstartfrom");
            jobObj["period"] = GetValue("jobschedule");
            jobObj["sendtime"] = GetValue("axptm_starttime");

            if (jobObj["period"].ToString().ToLower() == "every week")
                jobObj["sendon"] = GetValue("weekday");
            else if (jobObj["period"].ToString().ToLower() == "every month" || jobObj["period"].ToString().ToLower() == "every quarter")
                jobObj["sendon"] = GetValue("jday");
            else if (jobObj["period"].ToString().ToLower() == "custom")
                jobObj["sendon"] = GetValue("noofmins");

            return jobObj.ToString();
        }

        private async Task<DataTable> GetJobModifiedOn(string appName, string jobName)
        {
            try
            {
                _common._appName = appName;
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(appName);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];

                string sql = Constants_SQL.GET_AXPERTJOBSDETAILS;
                string[] paramNames = { "@jobname" };
                DbType[] paramTypes = { DbType.String };
                object[] paramValues = { jobName.ToLower() };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error in GetJobModifiedOn: " + ex.Message);
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "AxpertJobs-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Error in GetJobModifiedOn: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                return null;
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _redis.CloseAllConnections();
            await base.StopAsync(cancellationToken);
        }

        private async Task RunDailyCleanupAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                await Task.Delay(delay, stoppingToken);

                CleanupOldLogs();
            }
        }
        private void CleanupOldLogs()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(logDir);

            foreach (var file in Directory.GetFiles(logDir, "AxpertJobs-*.log"))
            {
                if (File.GetCreationTime(file).Date < DateTime.Today)
                {
                    File.Delete(file);
                }
            }
        }
    }
}