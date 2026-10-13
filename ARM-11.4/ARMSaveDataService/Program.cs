using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARMSaveDataService
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
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
            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
            var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();

            async Task<string> OnConsuming(string message)
            {
                try
                {
                    Console.WriteLine("OnConsuming method called with message " + message);
                    JObject messageObj = JObject.Parse(message);
                    string queueData = string.Empty;
                    string armResponseQueue = string.Empty;
                    JObject messageData;
                    string SignalrClientId = messageObj["signalrclient"].ToString();
                    queueData = messageObj["queuedata"]?.ToString();
                    armResponseQueue = messageObj["responsequeue"]?.ToString();
                    if (queueData != null)
                    {
                        string queueDataEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                        messageData = JsonConvert.DeserializeObject<JObject>(queueDataEscape);
                    }
                    else
                    {
                        messageData = messageObj;
                    }


                    string url = messageData["url"]?.ToString();
                    string method = messageData["method"]?.ToString();
                    string axResponseQueue = messageData["responsequeue"]?.ToString();
                    var payload = messageData["payload"]?.ToString()?.Replace("\r\n", "")?.Replace("\\", "") ?? "";

                    string mediaType = "application/json";
                    API _api = new API();
                    try
                    {
                        if (string.IsNullOrEmpty(url))
                        {
                            url = obj.AppConfig["URL"];
                            method = obj.AppConfig["METHOD"];
                        }
                        ARMResult apiResult;
                        if (method == "POST")
                        {
                            Console.WriteLine("url called" + url);
                            Console.WriteLine("payload called" + payload);
                            apiResult = await _api.POSTData(url, payload, mediaType);

                        }
                        else
                        {
                            Console.WriteLine("url called" + url);
                            Console.WriteLine("payload called" + payload);
                            apiResult = await _api.GetData(url);
                        }

                        if (!string.IsNullOrEmpty(SignalrClientId))
                        {
                            var singalRMessage = new
                            {
                                UserId = SignalrClientId,
                                Message = apiResult.result["message"]
                            };
                            await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
                            Console.Write("message sent to signalR client");
                        }
                        var result = JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] });
                        if (!string.IsNullOrEmpty(armResponseQueue))
                        {
                            Console.WriteLine(" sending message to armresponsequeue" + armResponseQueue);
                            Console.WriteLine("payload called" + payload);
                            rabbitMQProducer.SendMessages(result, armResponseQueue);
                        }
                        if (!string.IsNullOrEmpty(axResponseQueue))
                        {
                            Console.WriteLine(" sending message to axresponsequeue" + axResponseQueue);
                            rabbitMQProducer.SendMessages(result, axResponseQueue);
                        }

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
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }


            rabbitMQConsumer.DoConsume(queueName, OnConsuming);
        }
    }

}
