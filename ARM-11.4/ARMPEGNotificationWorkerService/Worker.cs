using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class Worker : BackgroundService
{
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly IConfiguration _config;
    private readonly IRabbitMQConsumer _consumer;
    private readonly Utils _common;

    public Worker(IConfiguration config, IRabbitMQConsumer consumer, Utils common)
    {
        _config = config;
        _consumer = consumer;
        _common = common;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string queueName = _config["AppConfig:QueueName"];
        try
        {
            async Task<string> OnConsuming(string message)
            {
                try
                {
                    return await PegNotificationService(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "PegNotificationService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{ex}\n\n");
                    return "error";
                }
            }
            _consumer.DoConsumeWorkerService(queueName, OnConsuming);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "PegNotificationService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            _exeDir = Path.Combine(_exeDir, _logFileName);
            File.AppendAllText(_exeDir, $"{ex}\n\n" + Environment.NewLine);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
    private async Task<string> PegNotificationService(string request)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string logFileName = "PegNotificationService-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, logFileName);
        try
        {
            JObject messageData = JObject.Parse(request);
            string queueData = messageData["queuedata"].ToString();
            JObject queueDataObj = JObject.Parse(queueData);

            string url = queueDataObj["url"]?.ToString();
            string method = queueDataObj["method"]?.ToString();
            string payload = queueDataObj["payload"]?.ToString();

            if (method == "POST")
            {
                string Mediatype = "application/json";
                API _api = new API();
                try
                {
                    var apiResult = await _api.POSTData(url, payload, Mediatype);
                    Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] }));
                    return JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] });
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_exeDir, "Error :" + JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }) + ex.StackTrace + Environment.NewLine);
                    return JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                }
            }

            else
            {
                API _api = new API();
                try
                {
                    var apiResult = await _api.GetData(url);
                    return JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] });
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_exeDir, "Error :" + JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }) + ex.StackTrace + Environment.NewLine);
                    return JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                }
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(_exeDir, "Error :" + JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } }) + ex.StackTrace + Environment.NewLine);
            return ex.Message;
        }
    }
}