using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;

namespace SFTPInitService
{
    class Program
    {
        private readonly DataContext dataContext;
        static async Task Main(string[] args)
        {
            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic config = JsonConvert.DeserializeObject(json);
            string queueName = config.AppConfig["QueueName"];
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
                    var axpertSFTPVersionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "sftpversion.json");
                    if (!File.Exists(axpertSFTPVersionPath))
                    {
                        File.Create(axpertSFTPVersionPath).Close();
                    }
                    string jsonContent = await CustomFileReadAsync(axpertSFTPVersionPath);

                    //Parse Incoming JSON
                    JObject messageData = JObject.Parse(message);
                    string queueData = messageData["queuedata"].ToString();
                    string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                    JObject receivedSFTPDetails = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
                    string appName = receivedSFTPDetails["appname"].ToString();
                    string starttimefrom = receivedSFTPDetails["starttime"].ToString();
                    DateTime startTime = DateTime.ParseExact(starttimefrom, "HH:mm", null);
                    string version = receivedSFTPDetails["version"].ToString();

                    JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();
                    if (!jobsversion.ContainsKey(appName))
                    {
                        jobsversion.Add(appName, version);
                        await CustomFileWriteAsync(axpertSFTPVersionPath, jobsversion.ToString());
                        Console.WriteLine("entry to sftpversion.json is added");
                    }
                    else
                    {
                        string jsversion = jobsversion[appName].ToString();
                        if (Int64.Parse(version) > Int64.Parse(jsversion))
                        {
                            jobsversion[appName] = version;
                            await CustomFileWriteAsync(axpertSFTPVersionPath, jobsversion.ToString());
                            Console.WriteLine("entry to sftpversion.json is updated");
                        }
                    }

                    string jsonVersion = jobsversion[appName].ToString();
                    if (Int64.Parse(version) >= Int64.Parse(jsonVersion))
                    {
                        DateTime newStartTime;
                        int delayInMs = -1;
                        TimeSpan timeDiff = startTime - DateTime.Now;
                        delayInMs = (int)timeDiff.TotalMilliseconds;
                        var queuejsondata = new
                        {
                            queuedata = receivedSFTPDetails.ToString(),
                        };

                        if (delayInMs < 0)
                        {
                            delayInMs = 0;
                            newStartTime = startTime.AddDays(1);
                            timeDiff = newStartTime - DateTime.Now;
                            delayInMs = (int)timeDiff.TotalMilliseconds;
                            Console.WriteLine("Current  StartTime is " + startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            Console.WriteLine("New StartTime is " + newStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            Console.WriteLine("Delay is Milliseconds is " + delayInMs);
                            rabbitMQProducer.SendMessages(message, queueName, false, delayInMs);
                        }
                        else
                        {
                            newStartTime = startTime;
                            Console.WriteLine("Current  StartTime is " + startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            Console.WriteLine("New StartTime is " + newStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            Console.WriteLine("Delay is Milliseconds is " + delayInMs);
                            rabbitMQProducer.SendMessages(message, queueName, false, delayInMs);
                            return "";
                        }

                        try
                        {
                            var dtSFTPSchedule = await GetSFTPSchedule(appName, startTime);
                            if (dtSFTPSchedule.data.Rows.Count > 0)
                            {
                                var sftpObject = new JObject();
                                sftpObject.Add("appname", appName);
                                sftpObject.Add("version", DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                                sftpObject.Add("updateversion", "true");
                                var sftpConsumerQueueData = new
                                {
                                    queuedata = sftpObject,
                                };

                                string jsonString = JsonConvert.SerializeObject(sftpConsumerQueueData);
                                Console.WriteLine("Constructed json for SFTPConsumerService service is: " + jsonString);
                                rabbitMQProducer.SendMessages(jsonString, config.AppConfig["SFTPConsumerQueue"].ToString(), false, 0);
                            }

                            for (int i = 0; i < dtSFTPSchedule.data.Rows.Count; i++)
                            {
                                var sftpObject = new JObject();
                                sftpObject.Add("appname", appName);
                                sftpObject.Add("sftp_applicable", dtSFTPSchedule.data.Rows[i]["sftp_applicable"].ToString());
                                sftpObject.Add("id", dtSFTPSchedule.data.Rows[i]["id"].ToString());
                                sftpObject.Add("hostname", dtSFTPSchedule.data.Rows[i]["hostname"].ToString());
                                sftpObject.Add("port_number", dtSFTPSchedule.data.Rows[i]["port_number"].ToString());
                                sftpObject.Add("user_name", dtSFTPSchedule.data.Rows[i]["user_name"].ToString());
                                sftpObject.Add("password", dtSFTPSchedule.data.Rows[i]["password"].ToString());
                                sftpObject.Add("localfolder", dtSFTPSchedule.data.Rows[i]["localfolder"].ToString());
                                sftpObject.Add("sourcefolder", dtSFTPSchedule.data.Rows[i]["sourcefolder"].ToString());
                                sftpObject.Add("scheduletime", dtSFTPSchedule.data.Rows[i]["scheduletime"].ToString());
                                sftpObject.Add("file_type", dtSFTPSchedule.data.Rows[i]["file_type"].ToString());
                                sftpObject.Add("backdated_filedays", dtSFTPSchedule.data.Rows[i]["backdated_filedays"].ToString());
                                sftpObject.Add("file_separator", dtSFTPSchedule.data.Rows[i]["file_separator"].ToString());
                                sftpObject.Add("client_email_id", dtSFTPSchedule.data.Rows[i]["client_email_id"].ToString());
                                sftpObject.Add("mail_content", dtSFTPSchedule.data.Rows[i]["mail_content"].ToString());
                                sftpObject.Add("mail_subject", dtSFTPSchedule.data.Rows[i]["mail_subject"].ToString());
                                sftpObject.Add("transid", dtSFTPSchedule.data.Rows[i]["transid"].ToString());
                                sftpObject.Add("version", version);
                                if (dtSFTPSchedule.data.Columns.Contains("mailfrom"))
                                {
                                    sftpObject.Add("mailfrom", dtSFTPSchedule.data.Rows[i]["mailfrom"].ToString());
                                }
                                else
                                {
                                    sftpObject.Add("mailfrom", "admin");
                                }

                                if (dtSFTPSchedule.data.Columns.Contains("cc"))
                                {
                                    sftpObject.Add("cc", dtSFTPSchedule.data.Rows[i]["cc"].ToString());
                                }
                                else
                                {
                                    sftpObject.Add("cc", "");
                                }

                                if (dtSFTPSchedule.data.Columns.Contains("bcc"))
                                {
                                    sftpObject.Add("bcc", dtSFTPSchedule.data.Rows[i]["bcc"].ToString());
                                }
                                else
                                {
                                    sftpObject.Add("bcc", "");
                                }

                                string scheduleTime = dtSFTPSchedule.data.Rows[i]["scheduletime"].ToString();
                                DateTime scheduleDateTime = DateTime.ParseExact(scheduleTime, "HH:mm", null);
                                DateTime currentDateTime = DateTime.Now;

                                TimeSpan timeDifference = scheduleDateTime - currentDateTime;
                                int sftpdelayInMs = (int)timeDifference.TotalMilliseconds;
                                if (sftpdelayInMs < 0)
                                    sftpdelayInMs = 0;

                                // Serialize the JSON object to a JSON string
                                var sftpConsumerQueueData = new
                                {
                                    queuedata = sftpObject,
                                };

                                string jsonString = JsonConvert.SerializeObject(sftpConsumerQueueData);
                                Console.WriteLine("Constructed json for SFTPConsumerService service is: " + jsonString);
                                rabbitMQProducer.SendMessages(jsonString, config.AppConfig["SFTPConsumerQueue"].ToString(), false, sftpdelayInMs + 10000);
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



            void RemoveQueueData(string queueName, string tempMessage)
            {
                rabbitMQConsumer.DeleteQueue(queueName);
                rabbitMQProducer.SendMessages(tempMessage, queueName, false, 0);
                rabbitMQConsumer.DoConsume(queueName, OnConsuming);
            }

            async Task<SQLResult> GetSFTPSchedule(string appName, DateTime startTime)
            {

                var context = new DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                string sql = config.AppConfig["GetSFTPScheduleSQL"];
                Dictionary<string, string> dbConfig = await utils.GetDBConfigurations(appName);
                string connectionString = dbConfig["ConnectionString"];
                string dbType = dbConfig["DBType"];
                string[] paramNames = { "@starttime" };
                DbType[] paramTypes = { DbType.String };
                object[] paramValues = { startTime.ToString("yyyy-MM-dd HH:mm:ss") };

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                var dtSFTPSchedule = await dbHelper.ExecuteQueryAsyncs(sql, connectionString, paramNames, paramTypes, paramValues);
                return dtSFTPSchedule;
            }

            async Task<string> CustomFileReadAsync(string filePath)
            {
                int maxRetries = 5;
                int retryDelayMilliseconds = 1000; // 1 second

                bool fileAccessed = false;
                int retryCount = 0;

                string fileResult = "";

                while (!fileAccessed && retryCount < maxRetries)
                {
                    try
                    {
                        using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            fileAccessed = true;
                            byte[] b = new byte[1024];
                            UTF8Encoding temp = new UTF8Encoding(true);
                            StringBuilder sb = new StringBuilder();

                            while (fs.Read(b, 0, b.Length) > 0)
                            {
                                sb.Append(temp.GetString(b));
                            }

                            fileResult = sb.ToString();

                        }
                    }
                    catch (IOException ex) when (IsFileLocked(ex))
                    {
                        retryCount++;
                        Thread.Sleep(retryDelayMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        break;
                    }
                }

                if (!fileAccessed)
                {
                    Console.WriteLine("Failed to access the file after multiple retries.");
                }

                return fileResult;
            }

            async Task<bool> CustomFileWriteAsync(string filePath, string fileContent)
            {
                int maxRetries = 5;
                int retryDelayMilliseconds = 1000; // 1 second

                bool fileAccessed = false;
                int retryCount = 0;

                while (!fileAccessed && retryCount < maxRetries)
                {
                    try
                    {
                        using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                        {
                            fileAccessed = true;
                            byte[] data = Encoding.UTF8.GetBytes(fileContent);
                            fs.Write(data, 0, data.Length);
                        }
                    }
                    catch (IOException ex) when (IsFileLocked(ex))
                    {
                        retryCount++;
                        Thread.Sleep(retryDelayMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        break;
                    }
                }

                if (!fileAccessed)
                {
                    Console.WriteLine("Failed to access the file after multiple retries.");
                    return false;
                }

                return true;
            }

            // Check if the IOException is due to a locked file
            static bool IsFileLocked(IOException exception)
            {
                int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
                return errorCode == 32 || errorCode == 33; // 32: The process cannot access the file because it is being used by another process
            }


            void ScheduleAppwiseInit()
            {
                try
                {
                    var axpertSFTPVersionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "sftpversion.json");
                    if (!File.Exists(axpertSFTPVersionPath))
                    {
                        File.Create(axpertSFTPVersionPath).Close();
                    }
                    string jsonContent = File.ReadAllText(axpertSFTPVersionPath);
                    JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();

                    var scheduleSection = configuration.GetSection("Schedule");
                    List<string> lstJson = new List<string>();
                    foreach (var appSchedule in scheduleSection.GetChildren())
                    {
                        string appname = appSchedule.Key;
                        string starttime = appSchedule.Value;
                        string version = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                        string[] starttimes = starttime.Replace(" ", "").Split(',');

                        // Create a List<string> from the array of strings
                        List<string> timeList = new List<string>(starttimes);

                        foreach (var time in timeList)
                        {
                            Console.WriteLine($"Appname: {appname}, StartTime: {time}");

                            var myObject = new
                            {
                                starttime = time,
                                appname = appname,
                                version = version
                            };

                            string jsonString = JsonConvert.SerializeObject(myObject);
                            var queuejsondata = new
                            {
                                queuedata = jsonString,

                            };
                            lstJson.Add(JsonConvert.SerializeObject(queuejsondata));

                        }

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

}
