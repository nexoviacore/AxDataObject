

using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using ARMCommon.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
var json = System.IO.File.ReadAllText(appSettingsPath);
dynamic obj = JsonConvert.DeserializeObject(json);
string queueName = "emailservice"; //obj.AppConfig["QueueName"];
string signalrUrl = obj.AppConfig["SignalRURL"];
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var configuration = builder.Build();

// Get the EmailConfiguration object from the configuration
var emailConfig = configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton(configuration);
serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
serviceCollection.AddSingleton<IConfiguration>(configuration);
var serviceProvider = serviceCollection.BuildServiceProvider();
var redis = new RedisHelper(configuration);
var armlogger = new ARMLogger(redis);
var rmq = new RabbitMQProducer(configuration);
ServicesLogger _logger = new ServicesLogger(rmq, armlogger);
string servicepath = AppDomain.CurrentDomain.FriendlyName;
Process currentProcess = Process.GetCurrentProcess();
ProcessModuleCollection modules = currentProcess.Modules;
string module = string.Empty;
foreach (ProcessModule process in modules)
{
    module = process.ModuleName;

}
var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
async Task<string> OnConsuming(string message)
{
    _logger.DoLog("Message Received by OnConsuming Method at {DateTime.Now} ", servicepath, module);
    JObject jsonObject = new JObject();
    try
    {
        _logger.DoLog("Message is send for parsing {DateTime.Now} ", servicepath, module);
        jsonObject = JObject.Parse(message);
        JObject queuedataJson = JObject.Parse((string)jsonObject["queuedata"]);
        string subject = (string)queuedataJson["EmailDetails"]["Subject"];
        string body = (string)queuedataJson["Body"];
        List<string> toEmails = queuedataJson["EmailDetails"]["To"]
             .Select(t => (string)t)
             .ToList();

        List<string> ccEmails = queuedataJson["EmailDetails"]["cc"]
            ?.Select(c => (string)c)
            .ToList();

        List<string> bccEmails = queuedataJson["EmailDetails"]["Bcc"]
            ?.Select(b => (string)b)
            .ToList();
        var context = new DataContext(configuration);
        var redis = new RedisHelper(configuration);
        var utils = new Utils(configuration, context, redis);
        EmailSender _emailSender = new EmailSender(emailConfig, utils);
        var messages = new Message(toEmails, bccEmails, ccEmails, subject, body);
        _logger.DoLog("Sending message through emailservcie at  {DateTime.Now} ", servicepath, module);
        await _emailSender.SendEmailAsync(messages);
        _logger.DoLog("email sent successfully at  {DateTime.Now} ", servicepath, module);
        await SendSignalRMessage(jsonObject["signalrclient"].ToString(), JsonConvert.SerializeObject(new { status = true, message = "Notification Sent Succcessfully" }));
        _logger.DoLog("Message sent to SignalR at {DateTime.Now} ", servicepath, module);
    }
    catch (Exception ex)
    {
        await SendSignalRMessage(jsonObject["signalrclient"].ToString(), JsonConvert.SerializeObject(new { status = false, message = " Error in Sending Notification " }));
        _logger.DoLog("Message sent to SignalR at {DateTime.Now} ", servicepath, module);
        return ex.Message;
    }
    return "Notification Sent Succcessfully";
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
rabbitMQConsumer.DoConsume(queueName, OnConsuming);
