using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARMLoggingService
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic obj = JsonConvert.DeserializeObject(json);
            //string queueName = obj.AppConfig["QueueName"];
            string queueName = "logQueue";
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
                    message = message.Replace("=", ":");
                    JObject jsonObject = JObject.Parse(message);
                    string instanceid = jsonObject["InstanceId"].ToString();
                    string logtype = jsonObject["logtype"].ToString();
                    string Module = jsonObject["Module"].ToString();
                    string pathValue = jsonObject["StackSTrace"].ToString().Trim();
                    var context = new DataContext(configuration);
                    var logGuid = Guid.NewGuid();
                    var log = new ARMLogs()
                    {
                        Id = logGuid,
                        path = pathValue,
                        module = Module,
                        logdetails = message,
                        username = "",
                        logtime = DateTime.Now.ToString(),
                        logtype = logtype,
                        instanceid = instanceid

                    };
                    try
                    {
                        context.Add(log);
                        var result = await context.SaveChangesAsync();
                        return JsonConvert.SerializeObject(new { status = "true" });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
                        return JsonConvert.SerializeObject(new { status = false, message = ex.Message });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
                    return JsonConvert.SerializeObject(new { status = false, message = ex.Message });
                }

            }
            rabbitMQConsumer.DoConsume(queueName, OnConsuming);


        }

    }
}