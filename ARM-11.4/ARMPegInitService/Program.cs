using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Data;
using System.Globalization;

namespace ARMPegInitService
{
    class Program
    {
        private readonly DataContext dataContext;
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic obj = JsonConvert.DeserializeObject(json);
            string queueName = obj.AppConfig["QueueName"];
            string apiUrl = obj.AppConfig["URL"];
            string method = "POST";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();


            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
            var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();

            async Task<string> OnConsuming(string message)
            {
                try
                {
                    DateTime messagedConsumedOn = DateTime.Now;
                    var axpertJobversionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "remainderversion.txt");
                    if (!File.Exists(axpertJobversionPath))
                    {
                        File.Create(axpertJobversionPath).Close();
                    }
                    string jsonContent = await File.ReadAllTextAsync(axpertJobversionPath);

                    //Parse Incoming JSON
                    JObject messageData = JObject.Parse(message);
                    string queueData = messageData["queuedata"].ToString();
                    string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                    JObject receivedReminder = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
                    string appName = receivedReminder["appname"].ToString();
                    string endtime = receivedReminder["endtime"]?.ToString() ?? "";
                    string version = receivedReminder["version"].ToString();
                    int interval = int.Parse(receivedReminder["interval"].ToString());
                    string starttimefrom = receivedReminder["starttime"].ToString();
                    string fromApi = receivedReminder["fromapi"]?.ToString() ?? "true";
                    if (!string.IsNullOrEmpty(endtime))
                    {
                        DateTime serviceendtime = DateTime.ParseExact(endtime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        Console.WriteLine("Service End Time of: " + serviceendtime);
                        if (serviceendtime < DateTime.Now)
                        {
                            Console.WriteLine("Current time is greater than endtime.");
                            return "Current time is greater than endtime.";
                        }
                    }

                    DateTime startTime = DateTime.ParseExact(starttimefrom, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    DateTime newStartTime;
                    if (fromApi == "true")
                    {
                        while (startTime < DateTime.Now)
                        {
                            startTime = startTime.AddMinutes(interval);
                        }
                        newStartTime = startTime;
                    }
                    else
                    {
                        newStartTime = startTime.AddMinutes(interval);
                    }

                    Console.WriteLine("Current  StartTime is " + startTime);
                    string newStarttimefrom = newStartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine("New StartTime is " + newStarttimefrom);

                    TimeSpan timeDiff = newStartTime - messagedConsumedOn;
                    int delayInMilliseconds = (int)timeDiff.TotalMilliseconds;
                    if (delayInMilliseconds < 0)
                        delayInMilliseconds = 0;
                    Console.WriteLine("delay is Milliseconds is " + delayInMilliseconds);


                    JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();
                    if (!jobsversion.ContainsKey(appName))
                    {
                        jobsversion.Add(appName, version);
                        await File.WriteAllTextAsync(axpertJobversionPath, jobsversion.ToString());
                        Console.WriteLine("entry to pegInitjobsversion.json is added");
                    }
                    else
                    {
                        string jsversion = jobsversion[appName].ToString();
                        if (int.Parse(version) > int.Parse(jsversion))
                        {
                            jobsversion[appName] = version;
                            await File.WriteAllTextAsync(axpertJobversionPath, jobsversion.ToString());
                            Console.WriteLine("entry to pegInitjobsversion.json is updated");
                        }
                    }

                    string jsonVersion = jobsversion[appName].ToString();
                    if (int.Parse(version) >= int.Parse(jsonVersion))
                    {
                        receivedReminder["starttime"] = newStarttimefrom;

                        if (receivedReminder.ContainsKey("fromapi"))
                        {
                            receivedReminder["fromapi"] = "false";
                        }
                        else
                        {
                            receivedReminder.Add("fromapi", "false");
                        }

                        string updatedJson = receivedReminder.ToString();

                        var queuejsondata = new
                        {
                            queuedata = updatedJson,

                        };

                        rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(queuejsondata), queueName, false, delayInMilliseconds);

                        if (fromApi == "true")
                            return "";
                        try
                        {
                            var table = await GetRemainder(appName, startTime, interval);
                            Console.WriteLine(DateTime.Now + "DataTable result is:" + JsonConvert.SerializeObject(table));
                            for (int i = 0; i < table.data.Rows.Count; i++)
                            {
                                var jsonObject = new JObject();

                                // Create the 'axnotify' object
                                var axNotifyObject = new JObject();
                                axNotifyObject.Add("axpapp", appName); // Set 'axpapp' from DataTable
                                axNotifyObject.Add("scriptname", "axpeg_reminder");
                                axNotifyObject.Add("trace", "true");
                                axNotifyObject.Add("stype", "tstructs");
                                axNotifyObject.Add("username", table.data.Rows[i]["q_fromuser"].ToString()); // You can set this field based on your requirements

                                // Create the 'reminders' array
                                var remindersArray = new JArray();
                                var reminderObject = new JObject();
                                reminderObject.Add("notifyto", table.data.Rows[i]["q_notify_touser"].ToString());
                                reminderObject.Add("sname", table.data.Rows[i]["q_transid"].ToString());
                                reminderObject.Add("recordid", table.data.Rows[i]["q_recordid"].ToString());
                                reminderObject.Add("taskid", table.data.Rows[i]["q_taskid"].ToString());
                                reminderObject.Add("processname", table.data.Rows[i]["q_processname"].ToString());
                                reminderObject.Add("taskname", table.data.Rows[i]["q_taskname"].ToString());
                                reminderObject.Add("keyfield", table.data.Rows[i]["q_keyfield"].ToString());
                                reminderObject.Add("keyvalue", table.data.Rows[i]["q_keyvalue"].ToString());
                                reminderObject.Add("notifytemplate", table.data.Rows[i]["q_templates"].ToString());

                                // Add the reminder object to the 'reminders' array
                                remindersArray.Add(reminderObject);

                                // Add the 'axnotify' object to the outer JSON object
                                jsonObject.Add("axnotify", axNotifyObject);

                                // Add the 'reminders' array to the 'axnotify' object
                                axNotifyObject.Add("reminders", remindersArray);

                                // Serialize the JSON object to a JSON string
                                string jsonString = jsonObject.ToString(Formatting.Indented);
                                var payload = new
                                {
                                    payload = jsonString
                                };
                                var axpegremainderqueue = new
                                {
                                    queuedata = payload,

                                };
                                DateTime pegremainderstartTime = (DateTime)table.data.Rows[i]["q_send_on"];
                                DateTime CurrentTime = DateTime.Now;
                                if (pegremainderstartTime < CurrentTime)
                                {
                                    pegremainderstartTime = CurrentTime;
                                }
                                TimeSpan pegtimeDiff = pegremainderstartTime - CurrentTime;
                                int pegdelayInMilliseconds = (int)pegtimeDiff.TotalMilliseconds;
                                Console.WriteLine("constructed json for pegremainder service is:" + jsonString);
                                rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(axpegremainderqueue), obj.AppConfig["PegRemainderQueueName"].ToString(), false, pegdelayInMilliseconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                            Console.WriteLine(DateTime.Now + errResult);
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
                    return ex.Message;
                }
                return message;
            }

            async Task<SQLResult> GetRemainder(string appName, DateTime startTime, int interval)
            {

                var context = new DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                string sql = obj.AppConfig["GetRemaindersSQL"];
                Dictionary<string, string> config = await utils.GetDBConfigurations(appName);
                string connectionString = config["ConnectionString"];
                string dbType = config["DBType"];
                string[] paramNames = { "@starttime", "@interval" };
                DbType[] paramTypes = { DbType.String, DbType.Int32 };
                object[] paramValues = { startTime.ToString("yyyy-MM-dd HH:mm:ss"), interval };

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                var table = await dbHelper.ExecuteQueryAsyncs(sql, connectionString, paramNames, paramTypes, paramValues);
                return table;
            }
            void ScheduleAppwiseInit()
            {
                try
                {
                    var axpertJobversionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "sftpversion.txt");
                    if (!File.Exists(axpertJobversionPath))
                    {
                        File.Create(axpertJobversionPath).Close();
                    }
                    string jsonContent = File.ReadAllText(axpertJobversionPath);
                    JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();

                    var scheduleSection = configuration.GetSection("Schedule");
                    List<string> lstJson = new List<string>();
                    foreach (var appSchedule in scheduleSection.GetChildren())
                    {
                        string appname = appSchedule.Key;
                        string starttime = appSchedule.Value;
                        string version = "";

                        if (!jobsversion.ContainsKey(appname))
                        {
                            version = "1";
                        }
                        else
                        {
                            string jsversion = jobsversion[appname].ToString();
                            int versionNumber;
                            if (int.TryParse(jsversion, out versionNumber))
                            {
                                versionNumber++; // Increment by 1
                                version = versionNumber.ToString();
                            }
                            else
                            {
                                Console.WriteLine($"Failed to parse 'version' as an integer: {version}");
                            }
                        }
                        // Print appname and starttime
                        Console.WriteLine($"Appname: {appname}, StartTime: {starttime}");

                        var myObject = new
                        {
                            starttime = starttime,
                            appname = appname,
                            version = version,

                        };

                        string jsonString = JsonConvert.SerializeObject(myObject);
                        var queuejsondata = new
                        {
                            queuedata = jsonString,

                        };
                        lstJson.Add(JsonConvert.SerializeObject(queuejsondata));
                    }
                    int delay = 0;
                    foreach (var item in lstJson)
                    {
                        rabbitMQProducer.SendMessages(item, queueName, false, delay);
                        delay = delay + 10000;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception in scheduleAppwiseInit Method " + ex.Message);

                }

            }
            ScheduleAppwiseInit();
            rabbitMQConsumer.DoConsume(queueName, OnConsuming);
        }

    }

    //class PEGReminderInitService{
    //    public void PEGReminderInitService(DataContext ) { 


    //    }

    //}
}