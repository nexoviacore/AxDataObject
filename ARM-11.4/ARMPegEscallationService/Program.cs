using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Data;
using System.Globalization;

namespace ARMPegInitService
{
    class Program
    {
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

            serviceCollection.AddSingleton(configuration);
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
                    var axpertJobversionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "escallationversion.txt");
                    if (!File.Exists(axpertJobversionPath))
                    {
                        File.Create(axpertJobversionPath).Close();
                    }
                    string jsonContent = await File.ReadAllTextAsync(axpertJobversionPath);

                    //Parse Incoming JSON
                    JObject messageData = JObject.Parse(message);
                    string queueData = messageData["queuedata"].ToString();
                    string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                    JObject receivedJob = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
                    string appName = receivedJob["appname"].ToString();
                    string endtime = receivedJob["endtime"]?.ToString() ?? "";
                    string version = receivedJob["version"].ToString();
                    int interval = int.Parse(receivedJob["interval"].ToString());

                    string starttimefrom = receivedJob["starttime"].ToString();
                    DateTime startTime = DateTime.ParseExact(starttimefrom, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    //if (startTime < messagedConsumedOn)
                    //{
                    //    startTime = messagedConsumedOn;
                    //}

                    Console.WriteLine("Current  StartTime is " + startTime);
                    TimeSpan timeDiff = startTime - messagedConsumedOn;
                    int delayInMilliseconds = (int)timeDiff.TotalMilliseconds;
                    if (delayInMilliseconds < 0)
                        delayInMilliseconds = 0;
                    Console.WriteLine("delay is Milliseconds is " + delayInMilliseconds);


                    DateTime newStartTime = startTime.AddMilliseconds(interval);
                    string newStarttimefrom = newStartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine("New StartTime is " + newStarttimefrom);

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

                    if (!string.IsNullOrEmpty(endtime))
                    {
                        DateTime serviceendtime = DateTime.ParseExact(endtime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        Console.WriteLine("Service has endtime of" + serviceendtime);
                        if (serviceendtime < startTime || serviceendtime < messagedConsumedOn)
                        {
                            Console.WriteLine("starttime is greater than endtime");
                            return "starttime is greater than endtime";
                        }
                    }

                    string jsonVersion = jobsversion[appName].ToString();
                    if (int.Parse(version) > int.Parse(jsonVersion) || int.Parse(version) == int.Parse(jsonVersion))
                    {
                        receivedJob["starttime"] = newStarttimefrom;
                        string updatedJson = receivedJob.ToString();
                        var queuejsondata = new
                        {
                            queuedata = updatedJson,

                        };
                        rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(queuejsondata), queueName, false, delayInMilliseconds);

                        try
                        {
                            var table = await GetEscallation(appName, startTime, interval);
                            Console.WriteLine(DateTime.Now + "DataTable result is:" + JsonConvert.SerializeObject(table.data));
                            for (int i = 0; i < table.data.Rows.Count; i++)
                            {
                                var jsonObject = new JObject();

                                // Create the 'axnotify' object
                                var axNotifyObject = new JObject();
                                axNotifyObject.Add("axpapp", appName); // Set 'axpapp' from DataTable
                                axNotifyObject.Add("scriptname", "axpeg_escalation");
                                axNotifyObject.Add("trace", "true");
                                axNotifyObject.Add("stype", "tstructs");
                                axNotifyObject.Add("username", table.data.Rows[i]["q_fromuser"].ToString()); // You can set this field based on your requirements

                                // Create the 'escallation' array
                                var escalationsArray = new JArray();
                                var escalationsObject = new JObject();
                                escalationsObject.Add("sname", table.data.Rows[i]["q_transid"].ToString());
                                escalationsObject.Add("recordid", table.data.Rows[i]["q_recordid"].ToString());
                                escalationsObject.Add("taskid", table.data.Rows[i]["q_taskid"].ToString());
                                escalationsObject.Add("processname", table.data.Rows[i]["q_processname"].ToString());
                                escalationsObject.Add("taskname", table.data.Rows[i]["q_taskname"].ToString());
                                escalationsObject.Add("keyfield", table.data.Rows[i]["q_keyfield"].ToString());
                                escalationsObject.Add("keyvalue", table.data.Rows[i]["q_keyvalue"].ToString());
                                escalationsObject.Add("escalateactionflag", table.data.Rows[i]["q_escalateactionflag"].ToString());
                                escalationsObject.Add("escalateto", table.data.Rows[i]["q_escalateto"].ToString());
                                escalationsObject.Add("notifyto", table.data.Rows[i]["q_fromuser"].ToString());
                                escalationsObject.Add("notifytemplate", table.data.Rows[i]["q_templates"].ToString());
                                 
                                // Add the reminder object to the 'reminders' array
                                escalationsArray.Add(escalationsObject);

                                // Add the 'axnotify' object to the outer JSON object
                                jsonObject.Add("axnotify", axNotifyObject);

                                // Add the 'reminders' array to the 'axnotify' object
                                axNotifyObject.Add("escalations", escalationsArray);

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
                                Console.WriteLine("constructed json for escallation service is:" + jsonString);
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

            async Task<SQLResult> GetEscallation(string appName, DateTime startTime, int interval)
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
            rabbitMQConsumer.DoConsume(queueName, OnConsuming);
        }

    }
}