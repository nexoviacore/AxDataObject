using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net;

namespace ARMQueueExchange
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic config = JsonConvert.DeserializeObject(json);
            string queueName = config.AppConfig["QueueName"];
            string apiUrl = config.AppConfig["URL"];
            string method = "POST";
            string signalrUrl = config.AppConfig["SignalRURL"];

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
                    WriteMessage("OnConsuming method called with message " + message);

                    JObject saveObj = JObject.Parse(message);
                    string queueData = string.Empty;
                    string armResponseQueue = string.Empty;
                    JObject saveData;
                    queueData = GetTokenIgnoreCase(saveObj, "QueueData")?.ToString();

                    if (queueData != null)
                    {
                        string queueDataEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
                        saveData = JsonConvert.DeserializeObject<JObject>(queueDataEscape);
                    }
                    else
                    {
                        saveData = saveObj;
                    }

                    JObject submitDataObj = JObject.FromObject(saveData["payload"]["submitdata"]);
                    string userName = GetTokenIgnoreCase(submitDataObj, "UserName")?.ToString();
                    string project = GetTokenIgnoreCase(submitDataObj, "Project")?.ToString();
                    string transId = GetTokenIgnoreCase(submitDataObj, "name")?.ToString();
                    string userAuthKey = GetTokenIgnoreCase(submitDataObj, "UserAuthKey")?.ToString();
                    string recordId = submitDataObj["dataarray"]["data"]["recordid"]?.ToString();
                    string clientCode = submitDataObj["dataarray"]?["data"]?["dc1"]?["row1"]?["client_code"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(transId) || string.IsNullOrEmpty(recordId))
                    {
                        Console.WriteLine($"Project/TransId/RecordId Details is missing for '{transId}' is not available in project '{project}'. Message is discarded. ");
                        return "";
                    }
                    var dt = await GetQueueDetails(project, userName, transId, recordId, clientCode);
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        Console.WriteLine($"Queue Details is missing for '{transId}' is not available in project '{project}'. Message is discarded. ");
                        return "";
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        string json = JsonConvert.SerializeObject(DataRowToJObject(row));
                        QueueExchange queueExchange = JsonConvert.DeserializeObject<QueueExchange>(json);

                        string keyValue = "";
                        if (!string.IsNullOrEmpty(queueExchange.KeyField))
                        {
                            keyValue = submitDataObj["dataarray"]?["data"]?["dc1"]?["row1"]?[queueExchange.KeyField]?.ToString();
                        }

                        AxpertRestAPIToken axpertToken = new AxpertRestAPIToken(queueExchange.UserName.ToString());
                        UpdateOrAddJsonKey(ref submitDataObj, "userauthkey", axpertToken.userAuthKey);
                        UpdateOrAddJsonKey(ref submitDataObj, "username", queueExchange.UserName);
                        UpdateOrAddJsonKey(ref submitDataObj, "project", queueExchange.Target);
                        if (!string.IsNullOrEmpty(keyValue))
                        {
                            UpdateOrAddJsonKey(ref submitDataObj, "keyfield", queueExchange.KeyField);
                            submitDataObj["dataarray"]["data"]["keyvalue"] = keyValue;
                            submitDataObj["dataarray"]["data"]["recordid"] = "0";
                        }


                        Utils util = new Utils();
                        var payload = new
                        {
                            Project = queueExchange.Target,
                            Queuename = queueExchange.QueueName,
                            SecretKey = util.GetEncryptedSecret(queueExchange.SecretKey),
                            submitdata = submitDataObj
                        };
                        string mediaType = "application/json";
                        API _api = new API();
                        try
                        {

                            ARMResult apiResult;

                            string apiUrl = queueExchange.ArmUrl.TrimEnd('/') + "/api/v1/ARMQueueSubmit";

                            Console.WriteLine("Url: " + apiUrl);
                            Console.WriteLine("Payload: " + JsonConvert.SerializeObject(payload));
                            //apiResult = await _api.POSTData(apiUrl, JsonConvert.SerializeObject(payload), mediaType);
                            var result = CallWebAPI(apiUrl, "POST", mediaType, JsonConvert.SerializeObject(payload));
                            //var result = JsonConvert.SerializeObject(apiResult);
                            WriteMessage($"API Result : {result}");
                        }
                        catch (Exception ex)
                        {
                            var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                            WriteMessage(errResult);
                        }
                    }

                    return "";
                }
                catch (Exception ex)
                {
                    var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                    WriteMessage(errResult);
                    return ex.Message;
                }

            }

            static JObject DataRowToJObject(DataRow row)
            {
                // Create a new JObject
                JObject jObject = new JObject();

                // Populate JObject with DataRow values
                foreach (DataColumn col in row.Table.Columns)
                {
                    jObject.Add(col.ColumnName, JToken.FromObject(row[col]));
                }

                return jObject;
            }

            static void WriteMessage(string message)
            {
                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
            }

            static JToken GetTokenIgnoreCase(JObject jObject, string propertyName)
            {
                // Find the property in a case-insensitive manner
                var property = jObject.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                return property?.Value;
            }

            static void RemoveJsonKey(ref JObject jsonObject, string key)
            {
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        jsonObject.Remove(property.Name);
                        break;
                    }
                }
            }

            static void UpdateOrAddJsonKey(ref JObject jsonObject, string key, JToken value)
            {
                bool keyExists = false;

                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value = value;
                        keyExists = true;
                        break;
                    }
                }

                if (!keyExists)
                {
                    jsonObject[key] = value;
                }
            }

            static APIResult ParseAxpertRestAPIResult(ARMResult armResult)
            {
                APIResult result = new APIResult();
                result.data.Add("success", armResult.result["success"]);
                JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
                if (!(resultJsonObj["status"]?.ToString().ToLower() == "true" || resultJsonObj["status"]?.ToString().ToLower() == "success"))
                {
                    result.error = resultJsonObj["result"].ToString();
                    return result;
                }
                foreach (var property in resultJsonObj.Properties())
                {
                    if (property.Name.ToLower() != "status")
                        result.data.Add(property.Name, property.Value);
                }
                return result;

            }

            static string CallWebAPI(string url, string method = "GET", string contentType = "application/json", string body = "")
            {
                string result = string.Empty;
                try
                {

                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = method;
                    httpRequest.ContentType = contentType;

                    using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                    {
                        streamWriter.Write(body);
                    }

                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
                catch (WebException e)
                {
                    try
                    {
                        using (WebResponse response = e.Response)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)response;
                            Console.WriteLine("Error code: {0}", httpResponse.StatusCode);

                            using (Stream data = response.GetResponseStream())
                            using (var reader = new StreamReader(data))
                            {
                                result = reader.ReadToEnd();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                    }
                }
                catch (Exception e)
                {
                    result = e.Message;
                }
                return result;
            }

            async Task<DataTable> GetJobModifiedOn(string appName, string jobName)
            {
                var context = new ARMCommon.Helpers.DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                try
                {
                    Dictionary<string, string> config = await utils.GetDBConfigurations(appName);
                    string connectionString = config["ConnectionString"];
                    string dbType = config["DBType"];
                    string sql = Constants_SQL.GET_AXPERTJOBSDETAILS;

                    string[] paramNames = { "@jobname" };
                    DbType[] paramTypes = { DbType.String };
                    object[] paramValues = { jobName.ToLower() };

                    IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);

                    return null;
                }
            }


            async Task<DataTable> GetQueueDetails(string appName, string userName, string transId, string recordId, string clientCode)
            {
                var context = new ARMCommon.Helpers.DataContext(configuration);
                var redis = new RedisHelper(configuration);
                Utils utils = new Utils(configuration, context, redis);

                try
                {
                    Dictionary<string, string> config = await utils.GetDBConfigurations(appName);
                    string connectionString = config["ConnectionString"];
                    string dbType = config["DBType"];
                    string sql = Constants_SQL.GET_QUEUEDETAILS;

                    string[] paramNames = { "@transid", "@username", "@recordid", "clientcode" };
                    DbType[] paramTypes = { DbType.String, DbType.String, DbType.String, DbType.String };
                    object[] paramValues = { transId.ToLower(), userName.ToLower(), recordId, clientCode };

                    IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                    return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);

                    return null;
                }
            }

            rabbitMQConsumer.DoConsume(queueName, OnConsuming);
        }

    }
}

public class APIResult
{
    public string error { get; set; }
    public Dictionary<string, object> data = new Dictionary<string, object>();
}

/*
async Task<string> OnConsuming(string message)
{
    try
    {
        DateTime messagedConsumedOn = DateTime.Now;
        var axpertJobversionPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "jsonversion.txt");
        string jsonContent = File.ReadAllText(axpertJobversionPath);
        JObject jobsversion = !string.IsNullOrEmpty(jsonContent) ? JObject.Parse(jsonContent) : new JObject();

        JObject messageData = JObject.Parse(message);
        string queueData = messageData["queuedata"].ToString();
        string messageWithoutEscape = queueData.Replace("\r\n", "").Replace("\\", "").Trim('"');
        JObject receivedJob = JsonConvert.DeserializeObject<JObject>(messageWithoutEscape);
        string jobid = receivedJob["Jobid"].ToString();
        string jobdata = receivedJob["Jobdata"].ToString();
        string version = receivedJob["version"].ToString();
        string active = receivedJob["active"].ToString();
        int interval = int.Parse(receivedJob["interval"].ToString());
        bool fromapi = (bool)receivedJob["fromapi"];
        string starttimefrom = receivedJob["starttime"].ToString();
        DateTime startTime = DateTime.ParseExact(starttimefrom, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        if (startTime < messagedConsumedOn)
        {
            startTime = messagedConsumedOn;
        }
        Console.WriteLine("Current  StartTime is " + startTime);
        TimeSpan timeDiff = startTime - messagedConsumedOn;
        int delayInMilliseconds = (int)timeDiff.TotalMilliseconds;
        //Console.WriteLine("delay in seconds  is " + delayInseconds);
        //int delayInMilliseconds = (delayInseconds > 0) ? delayInseconds * 60000 : 0;
        //int delay = (delayInMinutes > 0) ? delayInMinutes : 0;
        Console.WriteLine("delay is Milliseconds is " + delayInMilliseconds);
        DateTime newStartTime = startTime.AddMinutes(interval);

        // Convert newStartTime to a string using an appropriate format for display or storage
        string newStarttimefrom = newStartTime.ToString("yyyyMMddHHmmss");
        Console.WriteLine("New StartTime is " + newStarttimefrom);
        if (active == "T")
        {
            if (!jobsversion.ContainsKey(jobid))
            {
                jobsversion.Add(jobid, version);
                await File.WriteAllTextAsync(axpertJobversionPath, jobsversion.ToString());
                Console.WriteLine("entry to jobsversion.json is added");
            }

            string jsonVersion = jobsversion[jobid].ToString();

            if (fromapi)
            {
                if (int.Parse(version) > int.Parse(jsonVersion))
                {
                    jobsversion[jobid] = version;
                    await File.WriteAllTextAsync(axpertJobversionPath, jobsversion.ToString());
                    receivedJob["starttime"] = newStarttimefrom;//receivedJob["starttime"] = (long)Math.Round(newStartTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    receivedJob["fromapi"] = false;
                    string updatedJson = receivedJob.ToString();
                    var queuejsondata = new
                    {
                        queuedata = updatedJson,
                        queuename = queueName

                    };
                    rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(queuejsondata), queueName, false, delayInMilliseconds);
                }
                else if (int.Parse(version) == int.Parse(jsonVersion))
                {

                    receivedJob["starttime"] = newStarttimefrom;//receivedJob["starttime"] = (long)Math.Round(newStartTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    receivedJob["fromapi"] = false;
                    string updatedJson = receivedJob.ToString();
                    var queuejsondata = new
                    {
                        queuedata = updatedJson,
                        queuename = queueName

                    };
                    rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(queuejsondata), queueName, false, delayInMilliseconds);
                    return "published";
                }
                else if (int.Parse(version) < int.Parse(jsonVersion))
                {
                    //do nothing
                }
            }
            else
            {
                if (int.Parse(version) > int.Parse(jsonVersion) || int.Parse(version) == int.Parse(jsonVersion))
                {

                    receivedJob["starttime"] = newStarttimefrom;//(long)Math.Round(newStartTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    receivedJob["fromapi"] = false;
                    string updatedJson = receivedJob.ToString();
                    var queuejsondata = new
                    {
                        queuedata = updatedJson,
                        queuename = queueName

                    };
                    //call API here
                    API _api = new API();
                    ARMResult apiResult;
                    try
                    {
                        var request = new
                        {
                            jobid = jobid,
                            jobdata = jobdata,
                            trace = "t"
                        };
                        var payload = new
                        {
                            axpertjobsapi = request
                        };


                        if (method == "POST")
                            apiResult = await _api.POSTData(apiUrl, JsonConvert.SerializeObject(payload), "application/json");
                        else
                            apiResult = await _api.GetData(apiUrl);

                        var resultdata = JsonConvert.SerializeObject(new { status = apiResult.result["success"], msg = apiResult.result["message"] });
                        Console.WriteLine(DateTime.Now + resultdata);
                    }
                    catch (Exception ex)
                    {
                        var errResult = JsonConvert.SerializeObject(new { error = new List<string> { ex.Message } });
                        Console.WriteLine(DateTime.Now + errResult);
                    }
                    rabbitMQProducer.SendMessages(JsonConvert.SerializeObject(queuejsondata), queueName, false, delayInMilliseconds);
                }
                else
                {
                    // Do nothing
                }
            }


        }
        else
        {
            jobsversion[jobid] = version;
            await File.WriteAllTextAsync(axpertJobversionPath, jobsversion.ToString());
        }
    }

    catch (Exception ex)
    {
        Console.WriteLine(DateTime.Now + JsonConvert.SerializeObject(new { status = false, message = ex.Message }));
        return ex.Message;
    }
    return message;
}
*/

public class QueueExchange
{

    public string UserName { get; set; }
    public string TransId { get; set; }
    public string KeyField { get; set; }
    public string Target { get; set; }
    public string QueueName { get; set; }
    public string SecretKey { get; set; }
    public string ArmUrl { get; set; }


}