using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DataSyncronization
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            dynamic obj = JsonConvert.DeserializeObject(json);
            string queueName = "DataRefreshQueue";// obj.AppConfig["QueueName"];
            string signalrUrl = obj.AppConfig["SignalRURL"];
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
                    var redis = new RedisHelper(configuration);
                    var context = new DataContext(configuration);
                    var postgres = new PostgresHelper(configuration);
                    var utils = new Utils(configuration, context, redis);
                    API api = new API();
                    RabbitMQProducer Rabbitmq = new RabbitMQProducer(configuration);
                    ARMGetData _getdata = new ARMGetData(redis, postgres, configuration, utils, context, api, Rabbitmq);

                    Dictionary<string, string> cache = await redis.HashGetAllDictAsync(message);
                    if (cache.Count == 0)
                    {
                        return Constants.RESULTS.NOKEYAVAILABLEINREDIS.ToString();
                    }
                    string[] keyParts = message.Split('~');
                    string sourcetype = keyParts[1];
                    string Appname = keyParts[0];
                    string Datasource = keyParts[2];
                    if (sourcetype.ToLower() == Constants.SOURCETYPE.SQL.ToString().ToLower())
                    {
                        string[] paramParts = keyParts[3].Split('&');
                        Dictionary<string, string> SqlParams = new Dictionary<string, string>();

                        foreach (string paramPart in paramParts)
                        {
                            string[] paramKeyValue = paramPart.Split('=');
                            string key = paramKeyValue[0];
                            string value = paramKeyValue[1];
                            SqlParams[key] = value;
                        }

                        try
                        {
                            var SQLDataSource = await _getdata.GetSQLDataSource(Datasource, Appname);
                            var table = await _getdata.GetSQLData(SQLDataSource, SqlParams, queueName, false);
                            var result = JsonConvert.SerializeObject(table);
                            Console.WriteLine(DateTime.Now + result);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                            Console.WriteLine(DateTime.Now + errResult);
                            return errResult;
                        }
                    }
                    else
                    {
                        string url = keyParts[3];
                        string method = keyParts[4];
                        string payload = keyParts[5];
                        string mediaType = "application/json";
                        payload = ConvertToJsonString(payload);
                        ARMResult apiResult;
                        if (method.ToLower() == Constants.HTTPMETHOD.HTTPPOST.ToString().ToLower())
                        {
                            apiResult = await api.POSTData(url, payload, mediaType);
                        }
                        else
                        {
                            apiResult = await api.GetData(url);
                        }
                        Console.WriteLine(apiResult.result["message"]);
                        return JsonConvert.SerializeObject(apiResult.result["message"]);
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }
            string ConvertToJsonString(string inputString)
            {
                var parameters = new Dictionary<string, string>();

                try
                {
                    // Split the input string into key-value pairs
                    string[] keyValuePairs = inputString.Split('&');

                    foreach (string keyValuePair in keyValuePairs)
                    {
                        string[] keyValue = keyValuePair.Split('=');
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0];
                            string value = keyValue[1];
                            parameters[key] = value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle other general exceptions
                    // Log the exception or perform any other necessary error handling
                    Console.WriteLine("An error occurred: " + ex.Message);

                    // You can also throw a specific exception or return an error message
                    throw new Exception("An error occurred while extracting parameters.");
                }

                // Convert the parameters dictionary to a JSON string
                string jsonString = JsonConvert.SerializeObject(parameters);

                return jsonString;
            }

            rabbitMQConsumer.DoConsume(queueName, OnConsuming);
        }
    }

}