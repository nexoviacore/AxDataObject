using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using ARMCommon.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ARMCommon.Helpers
{
    public class AppSettingsValidaterService
    {
        private IConfiguration _config;
        private Utils _common;
        private IRedisCacheHelper _redisCache;
        private IAppSettingsHelper _appSettingsHelper;
        private IARMLogTraceService _traceService;
        private IHttpContextAccessor _http;
        public AppSettingsValidaterService(IConfiguration config, IHttpContextAccessor http)
        {
            _config = config;
            _http = http;
           
        }

        public async Task<ValidationResult> Validate(string appName)
        {
            var result = new ValidationResult();
            try
            {                               
                string _basePath = AppDomain.CurrentDomain.BaseDirectory;
                string _parentDir = Directory.GetParent(_basePath.TrimEnd('\\'))?.FullName;
                string jsonPath = Path.Combine(_basePath, "appsettings.json");
                string servicekeyPath = Path.Combine(_basePath, "service_key.json");
                string iniPathParent = Path.Combine(_parentDir, "appsettings.ini");

                if (!File.Exists(jsonPath))
                    result.Errors.Add("Appsettings.json is missing in ARM folder.");
                else
                {
                    DateTime lastUpdated = File.GetLastWriteTime(jsonPath);
                    result.Messages.Add("Appsettings.json updated on " + lastUpdated.ToString("dd/MM/yyyy HH:mm:ss"));
                }

                if (!File.Exists(servicekeyPath))
                    result.Errors.Add("Service_key.json (Firebase secretkey) is missing in ARM folder.");

                if (File.Exists(iniPathParent))
                {
                    var _appSettingsService = new AppSettingsService();
                    _traceService = new ARMLogTraceService(_http);
                    _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                    _redisCache = new RedisCacheHelper();
                    _common = new Utils(_config, _redisCache, _appSettingsHelper, _traceService);
                    _common._appName = appName;

                    try
                    {
                        string iniContent = File.ReadAllText(iniPathParent);
                        JsonDocument.Parse(iniContent);

                        using JsonDocument doc = JsonDocument.Parse(iniContent);

                        JsonElement root = doc.RootElement;

                        root.TryGetProperty("appconnections", out var appConnections);

                        if (!appConnections.TryGetProperty(appName, out _))
                            result.Errors.Add($"Project '{appName}' is missing in appconnections node in appsettings.ini");

                        root.TryGetProperty("appsettings", out var appSettings);
                        if (!appSettings.TryGetProperty(appName, out _))
                            result.Errors.Add($"Project '{appName}' is missing in appsettings node in appsettings.ini");
                        else
                        {
                            appSettings.TryGetProperty(appName, out var projectSettings);
                            if (projectSettings.TryGetProperty("ARM", out var armNode))
                            {
                                if (armNode.TryGetProperty("ARM_URL", out var armUrl) && !string.IsNullOrWhiteSpace(armUrl.GetString()))
                                {

                                    List<string> microserviceAppsList = new List<string> { "ARM_APIs","ARMNotificationHub"};
                                    foreach(string microserviceApp in microserviceAppsList)
                                    {
                                        using (HttpClient client = new HttpClient())
                                        {
                                            var requestUrl = armUrl + $"/{microserviceApp}/api/v1/armappstatus";
                                            var response = await client.GetAsync(requestUrl);

                                            var output = await response.Content.ReadAsStringAsync();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                result.Messages.Add($"URL '{requestUrl}' is running with response '{output}'");
                                            }
                                            else
                                            {
                                                result.Errors.Add($"URL '{requestUrl}' returned error response '{output}'");
                                            }
                                        }
                                    }

                                    List<string> microserviceAppsList2 = new List<string> { "AxAuth", "AxUtils", "AxStructUtils", "AxTstructData", "AxList" };
                                    foreach (string microserviceApp in microserviceAppsList2)
                                    {
                                        using (HttpClient client = new HttpClient())
                                        {
                                            var requestUrl = armUrl + $"/{microserviceApp}/api/v1/appstatus";
                                            var response = await client.GetAsync(requestUrl);

                                            var output = await response.Content.ReadAsStringAsync();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                result.Messages.Add($"URL '{requestUrl}' is running with response '{output}'");
                                            }
                                            else
                                            {
                                                result.Errors.Add($"URL '{requestUrl}' returned error response '{output}'");
                                            }
                                        }
                                    }

                                }
                                else
                                    result.Errors.Add($"ARM_URL is missing for project '{appName}' in appsettings.ini");

                                if (armNode.TryGetProperty("ARM_Scripts_URL", out var armScriptsUrl) && !string.IsNullOrWhiteSpace(armScriptsUrl.GetString()))
                                {
                                    using (HttpClient client = new HttpClient())
                                    {
                                        {
                                            var requestUrl = armScriptsUrl + "/axapps.xml";
                                            var response = await client.GetAsync(requestUrl);

                                            var output = await response.Content.ReadAsStringAsync();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                result.Messages.Add($"URL '{requestUrl}' has axapps.xml");

                                                try
                                                {
                                                    var axapps = XDocument.Parse(output);

                                                    var exists = axapps.Descendants().Any(e => e.Name.LocalName.Equals(appName, StringComparison.OrdinalIgnoreCase));

                                                    if (!exists)
                                                    {
                                                        result.Errors.Add($"Project '{appName}' is missing in axapps.xml from URL '{requestUrl}'");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    result.Errors.Add($"Error parsing axapps.xml from URL '{requestUrl}': {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                result.Errors.Add($"URL '{requestUrl}' is not having axapps.xml");
                                            }
                                        }

                                        {
                                            var requestUrl = armScriptsUrl + "/axprops.xml";
                                            var response = await client.GetAsync(requestUrl);

                                            var output = await response.Content.ReadAsStringAsync();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                result.Messages.Add($"URL '{requestUrl}' has axprops.xml");
                                            }
                                            else
                                            {
                                                result.Errors.Add($"URL '{requestUrl}' is not having axprops.xml");
                                            }
                                        }
                                    }
                                }
                                else
                                    result.Errors.Add($"ARM_Scripts_URL is missing for project '{appName}' in appsettings.ini");

                                if (armNode.TryGetProperty("ARM_Notification_URL", out var armNotifyUrl) && !string.IsNullOrWhiteSpace(armNotifyUrl.GetString()))
                                {
                                    using (HttpClient client = new HttpClient())
                                    {
                                        var requestUrl = armNotifyUrl + "/api/v1/armappstatus";
                                        var response = await client.GetAsync(requestUrl);

                                        var output = await response.Content.ReadAsStringAsync();
                                        if (response.IsSuccessStatusCode)
                                        {
                                            result.Messages.Add($"ARM_Notification_URL '{requestUrl}' is running with response '{output}'");
                                        }
                                        else
                                        {
                                            result.Errors.Add($"ARM_Notification_URL '{requestUrl}' returned error response '{output}'");
                                        }
                                    }
                                }
                                else
                                    result.Errors.Add($"ARM_Notification_URL is missing for project '{appName}' in appsettings.ini");
                            }
                            else
                            {
                                result.Errors.Add($"ARM node is missing for project '{appName}' in appsettings.ini");
                            }

                            if (projectSettings.TryGetProperty("Redis", out var redisNode))
                            {
                                var redisConnected = await _common.OpenRedisConnectionAsync();
                                if (redisConnected)
                                    result.Messages.Add($"Redis settings is valid for project '{appName}'.");
                                else
                                    result.Errors.Add($"Redis settings is not valid for project '{appName}'.");

                                await _common.CloseRedisConnectionAsync();
                            }
                            else
                            {
                                result.Errors.Add($"Redis node is missing for project '{appName}' in appsettings.ini");
                            }

                            if (projectSettings.TryGetProperty("licdomain", out var licdomainNode))
                            {
                                licdomainNode.TryGetProperty("domainname", out var domainName);
                                if (string.IsNullOrWhiteSpace(domainName.GetString()))
                                    result.Errors.Add($"Domainname is not configured in Licdomain node for project '{appName}' in appsettings.ini");
                            }
                            else
                            {
                                result.Errors.Add($"Licdomain node is missing for project '{appName}' in appsettings.ini");
                            }

                            if (!projectSettings.TryGetProperty("MobileSettings", out var mobileSettingsNode))
                            {
                                result.Errors.Add($"MobileSettings node is missing for project '{appName}' in appsettings.ini");
                            }
                        }


                        if (root.TryGetProperty("rmqsettings", out var rmqSettings))
                        {
                            var rmqUrl = _common._appSettingsHelper.GetRMQueueURL();
                            if (string.IsNullOrEmpty(rmqUrl))
                            {
                                result.Errors.Add($"Rmq connection details is not proper in appsettings.ini");
                            }
                            else
                            {
                                var rmqHelper = new RabbitMQHelper();
                                var rmqStatus = rmqHelper.CanConnectToRabbitMQ(rmqUrl);
                                if (rmqStatus == "Connected to RMQ")
                                    result.Messages.Add($"RMQ settings is valid.");
                                else
                                    result.Errors.Add($"Not able to connect to RMQ. It throwed following error: {rmqStatus}");
                            }
                        }
                        else
                        {
                            result.Errors.Add($"Rmqsettings node is missing in appsettings.ini");
                        }
                    }
                    catch (JsonException ex)
                    {
                        result.Errors.Add($"Invalid JSON in appsettings.ini: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(ex.Message);
                    }

                    DateTime lastUpdated = File.GetLastWriteTime(iniPathParent);
                    result.Messages.Add("Appsettings.ini last updated on " + lastUpdated.ToString("dd/MM/yyyy HH:mm:ss"));

                }
                else
                {
                    result.Errors.Add($"Appsettings.ini is missing in ARM_APIs parent folder - {iniPathParent}");
                }
            }
            catch(Exception ex)
            {
                result.Errors.Add($"Error during validation for project {appName}: " + ex.Message);
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Messages { get; set; } = new List<string>();
        public bool Success => Errors.Count == 0;
    }
}

