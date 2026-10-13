using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMServices;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net.Http.Headers;
using System.Text;

var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
var json = File.ReadAllText(appSettingsPath);
dynamic obj = JsonConvert.DeserializeObject(json);
string queueName = obj.AppConfig["QueueName"];

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
    Console.WriteLine("OnConsuming method called with message: " + message);

    try
    {
        JObject json = JObject.Parse(message);
        string[] parts = json["queuedata"].ToString().Split('~');

        if (parts.Length > 2)
        {
            var parsedMessage = new QueueMessage
            {
                Project = parts[0],
                Username = parts[1],
                LoginTime = parts[2],
                Identifier = (parts.Length == 3 ? "ALL" : parts[3])
            };

            var records = await GetGeoFencingRecords(parsedMessage);

            if (records == null || records.Rows.Count == 0)
            {
                var firebaseIdRecord = await GetUserFireBaseId(parsedMessage);

                foreach (DataRow row in firebaseIdRecord.Rows)
                {
                    await StopLocationFirebaseCall(parsedMessage, row);
                }

                Console.WriteLine($"User does not have any records: {message}");
                return "";
            }

            foreach (DataRow row in records.Rows)
            {
                await SendLocationFirebaseCall(parsedMessage, row);
            }

            return "";
        }

        return "Invalid message format";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing queue message: {ex.Message}");
        return "Error";
    }
}

async Task<DataTable> GetGeoFencingRecords(QueueMessage msg)
{
    var context = new ARMCommon.Helpers.DataContext(configuration);
    var redis = new RedisHelper(configuration);
    Utils utils = new Utils(configuration, context, redis);

    try
    {
        Dictionary<string, string> config = await utils.GetDBConfigurations(msg.Project);
        string connectionString = config["ConnectionString"];
        string dbType = config["DBType"];
        string sql = Constants_SQL.GET_USERGEOLOCATIONCONFIG;

        string[] paramNames = { "@username", "@logintime", "@identifier", "@identifier" };
        DbType[] paramTypes = { DbType.String, DbType.String, DbType.String, DbType.String };
        object[] paramValues = { msg.Username.ToLower(), msg.LoginTime, msg.Identifier, msg.Identifier };

        IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
        return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error:" + ex.Message);

        return null;
    }
}

async Task<DataTable> GetUserFireBaseId(QueueMessage msg)
{
    var context = new ARMCommon.Helpers.DataContext(configuration);
    var redis = new RedisHelper(configuration);
    Utils utils = new Utils(configuration, context, redis);

    try
    {
        Dictionary<string, string> config = await utils.GetDBConfigurations(msg.Project);
        string connectionString = config["ConnectionString"];
        string dbType = config["DBType"];
        string sql = Constants_SQL.GET_FIREBASEID;

        string[] paramNames = { "@username" };
        DbType[] paramTypes = { DbType.String };
        object[] paramValues = { msg.Username.ToLower() };

        IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
        return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error:" + ex.Message);

        return null;
    }
}


async Task SendLocationFirebaseCall(QueueMessage msg, DataRow row)
{
    try
    {
        string firebaseApiUrl = obj.AppConfig["FireBaseURL"];
        string armUrl = obj.AppConfig["ARMUpdateLocationURL"];
        var accessToken = await GetAccessTokenAsync();

        var payload = new
        {
            message = new
            {
                token = row["FirebaseID"]?.ToString(),
                data = new
                {
                    armurl = armUrl,
                    type = "sendlocation",
                    identifier = row["IDENTIFIER"]?.ToString(),
                    expectedlocations = row["EXPECTEDLOCATIONS"]?.ToString(),
                    project = msg.Project,
                    username = msg.Username,
                    interval = row["INTERVAL"]?.ToString(),
                    queuename = queueName,
                    logintime = row["LOGINTIME"]?.ToString()
                }
            }
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        Console.WriteLine($"JSON Payload: {jsonPayload}");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(firebaseApiUrl, content);

            var result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"FireBase result: {result}.");
            }
            else
            {
                Console.WriteLine($"Error sending notification: {result}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in SendLocationFirebaseCall: {ex.Message}");
    }
}


async Task StopLocationFirebaseCall(QueueMessage msg, DataRow row)
{
    try
    {
        string firebaseApiUrl = obj.AppConfig["FireBaseURL"];
        var accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            Console.WriteLine("Access token could not be retrieved.");
            return;
        }

        var payload = new
        {
            message = new
            {
                token = row["FIREBASEID"].ToString(),
                data = new
                {
                    type = "stoplocation",
                    project = msg.Project,
                    username = msg.Username,
                    identifier = msg.Identifier
                }
            }
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        Console.WriteLine($"JSON Payload: {jsonPayload}");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(firebaseApiUrl, content);

            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Firebase result: {result}");
            }
            else
            {
                Console.WriteLine($"Error stopping notification: StatusCode={response.StatusCode}, Message={result}");
            }
        }
    }
    catch (HttpRequestException httpEx)
    {
        Console.WriteLine($"HTTP Request Error in StopLocationFirebaseCall: {httpEx.Message}");
    }

}

async Task<string> GetAccessTokenAsync()
{
    //string[] scopes = { "https://www.googleapis.com/auth/firebase.messaging" }; 
    //GoogleCredential googleCredential;
    //string scopes = "https://www.googleapis.com/auth/firebase.messaging";

    var bearertoken = "";
    using (var stream = new FileStream("service_key.json", FileMode.Open, FileAccess.Read))
    {
        var credential = GoogleCredential.FromStream(stream).CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
        bearertoken = credential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;

    }


    return bearertoken;
}

rabbitMQConsumer.DoConsume(queueName, OnConsuming);


namespace ARMServices
{
    public class QueueMessage
    {
        public string Project { get; set; }
        public string Username { get; set; }
        public string LoginTime { get; set; }
        public string Identifier { get; set; }
    }
}