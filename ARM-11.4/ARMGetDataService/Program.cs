using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                    string datasources = (string)messageDatas["Datasources"];
                    string Id = (string)messageDatas["Id"];
                    string appname = (string)messageDatas["AppName"];
                    Dictionary<string, string> sqlParams = null;
                    var context = new DataContext(configuration);
                    var redis = new RedisHelper(configuration);
                    var postgres = new PostgresHelper(configuration);
                    var utils = new Utils(configuration, context, redis);
                    API api = new API();
                    RabbitMQProducer Rabbitmq = new RabbitMQProducer(configuration);
                    var getdata = new ARMGetData(redis, postgres, configuration, utils, context, api, Rabbitmq);
                    var data = await getdata.GetDataSourceData(appname, datasources, sqlParams);
                    await SaveDataSourceResultToRedis(datasources, Id, JsonConvert.SerializeObject(data));
                    return JsonConvert.SerializeObject(new { status = "true" });

                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
                    return JsonConvert.SerializeObject(new { status = false, message = ex.Message });
                }
            }

            async Task<bool> SaveDataSourceResultToRedis(string datasource, string dataId, string data)
            {
                string Id = datasource + dataId.ToString();
                var redis = new RedisHelper(configuration);
                try
                {
                    await redis.StringSetAsync(Id, data);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            rabbitMQConsumer.DoConsume(queueName, OnConsuming);


        }

    }
}