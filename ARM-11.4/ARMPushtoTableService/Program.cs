using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;



namespace ARMPushtoTableService
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
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
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();

            async Task<string> OnConsuming(string message)
            {
                try
                {
                    JObject messageData = JObject.Parse(message);
                    string queueData = messageData["queuedata"].ToString();
                    string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                    JObject messageDatas = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
                    JArray pushToTableArray = (JArray)messageDatas["pushtotable"];

                    foreach (JObject pushToTableObject in pushToTableArray)
                    {
                        string tableName = pushToTableObject["table"].ToString();
                        string action = pushToTableObject["action"].ToString();
                        JArray dataArray = (JArray)pushToTableObject["data"];

                        try
                        {
                            Dictionary<string, string> config = await GetDBConnString("hcmdev");
                            string connectionString = config["ConnectionString"];
                            //string connectionString = "Host=10.62.1.12; Database=agileconnect_dev; Username=agileconnect; Password=log";
                            string dbType = config["DBType"];
                            var result = await InsertData(tableName, dataArray, connectionString);

                            await SendSignalRMessage(messageData["signalrclient"].ToString(), result);

                            Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = "true", msg = result }));
                        }
                        catch (Exception ex)
                        {
                            await SendSignalRMessage(messageData["signalrclient"].ToString(), ex.Message);

                            Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }));
                            return JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                        }

                    }
                    return JsonConvert.SerializeObject(new { status = "true" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
                    return JsonConvert.SerializeObject(new { status = false, message = ex.Message });
                }
            }

            rabbitMQConsumer.DoConsume(queueName, OnConsuming);


            async Task<Dictionary<string, string>> GetDBConnString(string appName)
            {

                var context = new DataContext(configuration);
                var redis = new RedisHelper(configuration);
                var utils = new Utils(configuration, context, redis);
                Dictionary<string, string> config = await utils.GetDBConfigurations(appName);
                return config;

            }

            async Task<string> InsertData(string tableName, JArray dataArray, string connectionString)
            {
                try
                {
                    var PostgresHelper = new PostgresHelper(configuration);


                    foreach (JObject dataObject in dataArray)
                    {
                        var columnNames = string.Join(",", dataObject.Properties().Select(p => p.Name));
                        var parameterNames = string.Join(",", dataObject.Properties().Select(p => "@" + p.Name));
                        var query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({parameterNames});";

                        var paramName = dataObject.Properties().Select(p => p.Name).ToArray();
                        var paramType = new NpgsqlDbType[dataObject.Properties().Count()];
                        var paramValue = dataObject.Properties().Select(p => p.Value.ToString()).ToArray();

                        for (int i = 0; i < dataObject.Properties().Count(); i++)
                        {
                            paramType[i] = NpgsqlDbType.Varchar;
                        }

                        await PostgresHelper.ExecuteSql(query, connectionString, paramName, paramType, paramValue);
                    }


                    return "Data Inserted";
                }
                catch (Exception ex)
                {
                    // Log or handle exception
                    return ex.Message;
                }
            }


            async Task SendSignalRMessage(string clientId, string message)
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    var singalRMessage = new
                    {
                        UserId = clientId,
                        Message = message
                    };
                    API _api = new API();
                    await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
                }
            }
        }
    }

}