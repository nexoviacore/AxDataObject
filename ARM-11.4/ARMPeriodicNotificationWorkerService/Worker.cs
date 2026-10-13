using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ARMCommon.Services;
using System.Collections;
using System.Data;
using ARMCommon.Interface;

namespace ARMPeriodicNotificationWorkerService
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

            string queueName = _config["AppConfig:QueueName"];
            //_consumer.DoConsumeWorkerServiceJob(queueName, OnConsuming);
            _consumer.DoConsumeWorkerService(queueName, OnConsuming);
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (string accessCode in appList)
                {
                    var config = _appSettingsHelper.GetRedisSettings(accessCode);
                    string redisConnString = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;
                    while (true)
                    {
                        try
                        {
                            string? jobId = await _redis.PopDueJob(accessCode, redisConnString);
                            if (string.IsNullOrEmpty(jobId))
                                break;
                            _ = Task.Run(() => ProcessDueNotifications(accessCode, redisConnString, jobId), stoppingToken);
                        }
                        catch (Exception ex) { }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        private async Task<string> OnConsuming(string message)
        {
            JObject notificationObj = JObject.Parse(message);
            string _accessCode = notificationObj["queuejson"]["project"]?.ToString();
            string jobName = notificationObj["queuejson"]["notification"]?.ToString();
            if (!appList.Contains(_accessCode))
                appList.Add(_accessCode);
            if (string.IsNullOrEmpty(_accessCode) || string.IsNullOrEmpty(jobName))
                return "Invalid message";
            var dt = await GetJobModifiedOn(_accessCode, jobName);
            if (dt == null || dt.Rows.Count == 0)
                return "Job not found";
            string Active = dt.Rows[0]["active"]?.ToString();
            string Notifyid = dt.Rows[0]["axperiodnotifyid"]?.ToString();
            string payload = string.Empty;
            foreach (DataRow row in dt.Rows)
            {
                string jobId = row["axperiodnotifyid"]?.ToString();
                if (string.IsNullOrEmpty(jobId))
                    continue;
                payload = BuildJobPayloadFromRow(row);
            }
            JObject jObj = JObject.Parse(payload);
            var config = _appSettingsHelper.GetRedisSettings(_accessCode);
            string _redisConnection = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;
            if (Active == "F")
            {
                await _redis.Remove(_accessCode, _redisConnection, Notifyid);
                return "Deleted";
            }

            var nextRun = CalculateNextOccurrence(jObj);

            await _redis.SaveJob(_accessCode, _redisConnection, Notifyid, payload.ToString());

            await _redis.Schedule(_accessCode, _redisConnection, Notifyid, nextRun);

            return "Scheduled";
        }
        private async Task RestoreJobsFromDatabase()
        {
            try
            {
                foreach (var appName in _appSettingsService._appSettings)
                {
                    string accessCode = appName.Key;
                    _common._appName = accessCode;
                    try
                    {
                        var dt = await GetAllActivePeriodicNoti(accessCode);
                        if (dt == null || dt.Rows.Count == 0)
                            continue;
                        appList.Add(accessCode);
                        var config = _appSettingsHelper.GetRedisSettings(accessCode);
                        string redisConnectionString = config.Host + ":" + config.Port + ",password=" + config.DecryptedPwd;

                        foreach (DataRow row in dt.Rows)
                        {
                            string jobId = row["axperiodnotifyid"]?.ToString();
                            if (string.IsNullOrEmpty(jobId))
                                continue;

                            string payload = BuildJobPayloadFromRow(row);
                            if (string.IsNullOrEmpty(payload))
                                continue;

                            JObject jobObj = JObject.Parse(payload);
                            var next = CalculateNextOccurrence(jobObj);

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
                        string logFileName = "PeriodicNotification-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                        _exeDir = Path.Combine(_exeDir, logFileName);
                        File.AppendAllText(_exeDir, "Restore error: accessCode: " + accessCode + " Exception:" + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "PeriodicNotification-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Restore error: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
        }
        private async Task ProcessDueNotifications(string _accessCode, string _redisConnection, string jobId)
        {
            var payload = await _redis.GetJob(_accessCode, _redisConnection, jobId);
            if (payload == null)
                return;
            JObject jobObj = JObject.Parse(payload);
            if (jobObj["active"]?.ToString() == "F")
            {
                await _redis.RemoveJobData(_accessCode, _accessCode, jobId);
                return;
            }
            JObject notificationObj = JObject.Parse(payload);
            await ExecuteNotification(notificationObj, _accessCode);
            var nextRun = CalculateNextOccurrence(notificationObj);
            await _redis.Schedule(_accessCode, _redisConnection, jobId, nextRun);
        }
        private async Task ExecuteNotification(JObject notificationObj, string _accessCode)
        {
            try
            {
                string notification = notificationObj["name"]?.ToString();
                string userName = notificationObj["fromuser"]?.ToString();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = notificationObj["username"]?.ToString();
                }
                string _trace = _config["AppConfig:trace"];
                string _armSignalRUrl = _appSettingsHelper.GetARMUrl(_accessCode);
                _armSignalRUrl = _armSignalRUrl + "/ARM_APIs/api/v1/SendSignalR";
                var payload = new
                {
                    axnotify = new
                    {
                        axpapp = _accessCode,
                        scriptname = "axpeg_notification",
                        username = userName,
                        isperiodicnotification = "true",
                        trace = _trace,
                        signalrurl = _armSignalRUrl,
                        notifications = new[] { new { periodicnotifyname = notification, modifiedon = "" } }
                    }
                };
                //string apiUrl = await GetApiUrl(project);
                string baseUrl = await _common.AxpertWebScriptsURL(_accessCode);

                string apiUrl = $"{baseUrl.TrimEnd('/')}/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/axnotify";

                var api = new API();
                await api.POSTData(apiUrl, JsonConvert.SerializeObject(payload), "application/json");
            }
            catch (Exception ex)
            {
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "PeriodicNotification-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, $"Execution error (Access: {_accessCode}, Job: {notificationObj}): {ex.Message}" + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
        }
        private DateTime CalculateNextOccurrence(JObject notificationObj)
        {
            var scheduler = new JobScheduler();
            if (notificationObj["queuejson"] != null)
            {
                var timeJson = new
                {
                    startDate = notificationObj["queuejson"]["start_date"]?.ToString(),
                    period = notificationObj["queuejson"]["period"]?.ToString(),
                    sendTime = notificationObj["queuejson"]["sendtime"]?.ToString(),
                    sendOn = notificationObj["queuejson"]["sendon"]?.ToString(),
                    firstDayOfWeek = _config["AppConfig:FirstDayOfWeek"]
                };
                return scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson));
            }
            else
            {
                var timeJson = new
                {
                    startDate = notificationObj["startdate"]?.ToString(),
                    period = notificationObj["frequency"]?.ToString(),
                    sendTime = notificationObj["sendtime"]?.ToString(),
                    sendOn = notificationObj["sendon"]?.ToString(),
                    firstDayOfWeek = _config["AppConfig:FirstDayOfWeek"]
                };
                return scheduler.GetNextOccurrence(JsonConvert.SerializeObject(timeJson));
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _redis.CloseAllConnections();
            await base.StopAsync(cancellationToken);
        }
        private async Task<DataTable> GetAllActivePeriodicNoti(string appName)
        {
            try
            {
                _common._appName = appName;
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(appName);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];

                string sql = Constants_SQL.GET_PERIODICNOTIFICATIION_ALL;
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                return await dbHelper.ExecuteQueryAsync(sql, connectionString);
            }
            catch (Exception ex)
            {
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "PeriodicNotification-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Error in GetAllActivePeriodicNoti: appName: " + appName + " Exception:" + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                return null;
            }
        }
        private string BuildJobPayloadFromRow(DataRow row)
        {
            if (row == null)
                return null;

            JObject jobObj = new JObject();
            //string GetValue(string col) => row.Table.Columns.Contains(col) ? row[col]?.ToString() : null;
            string GetValue(string col)
            {
                if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value)
                    return null;
                var value = row[col];
                if (value is DateTime dt)
                    return dt.ToString("dd/MM/yyyy");
                return value.ToString();
            }
            jobObj["axperiodnotifyid"] = GetValue("axperiodnotifyid");
            jobObj["name"] = GetValue("name");
            jobObj["startdate"] = GetValue("startdate");
            jobObj["starttime"] = GetValue("starttime");
            jobObj["frequency"] = GetValue("frequency");
            jobObj["sendday"] = GetValue("sendday");
            jobObj["sendon"] = GetValue("sendon");
            if (string.IsNullOrEmpty(jobObj["sendon"].ToString()) && !string.IsNullOrEmpty(jobObj["sendday"].ToString()))
                jobObj["sendon"] = jobObj["sendday"];
            jobObj["sendtime"] = GetValue("sendtime");
            jobObj["active"] = GetValue("active");
            jobObj["datasource"] = GetValue("datasource");
            jobObj["messagetitle"] = GetValue("messagetitle");
            jobObj["messagetext"] = GetValue("messagetext");
            jobObj["notifyto"] = GetValue("notifyto");
            jobObj["usergroups"] = GetValue("usergroups");
            jobObj["usernames"] = GetValue("usernames");
            jobObj["externalemails"] = GetValue("externalemails");
            jobObj["fromuser"] = GetValue("fromuser");
            jobObj["modifiedon"] = GetValue("modifiedon");

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
                string sql = Constants_SQL.GET_PERIODICNOTIFICATIION_MODIFIEDON;
                string[] paramNames = { "@name" };
                DbType[] paramTypes = { DbType.String };
                object[] paramValues = { jobName };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
            }
            catch (Exception ex)
            {
                string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(_exeDir);
                string logFileName = "PeriodicNotification-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                _exeDir = Path.Combine(_exeDir, logFileName);
                File.AppendAllText(_exeDir, "Error in GetJobModifiedOn: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                return null;
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
    }
}