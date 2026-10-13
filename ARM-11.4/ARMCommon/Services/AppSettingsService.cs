using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace ARMCommon.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly string _appSettingsPath;
        private readonly object _lock = new object();
        private AppSettingsConfig _appSettingsConfig;
        public readonly ConcurrentDictionary<string, AppConnection> _appConnections = new();
        public readonly ConcurrentDictionary<string, RedisSettings> _redisConnections = new();
        public string RMQueueUrl = string.Empty;

        public AppSettingsConfig AppSettingsConfig => _appSettingsConfig;
        
        public AppSettingsService()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string parentDir = Directory.GetParent(exeDir.TrimEnd('\\'))?.FullName;
            _appSettingsPath = Path.Combine(parentDir, "AppSettings.ini");

            _appSettingsConfig = LoadAppSettings();
        }

        private AppSettingsConfig LoadAppSettings(string appName = "")
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    throw new FileNotFoundException($"AppSettings.ini file not found at: {_appSettingsPath}");
                }               

                var jsonContent = File.ReadAllText(_appSettingsPath);
                var appSettingsConfig = JsonConvert.DeserializeObject<AppSettingsConfig>(jsonContent);

                if (appSettingsConfig == null)
                {
                    throw new InvalidOperationException("Failed to deserialize AppSettings.ini file");
                }

                if (string.IsNullOrEmpty(appName))
                {
                    foreach (var connection in appSettingsConfig.AppConnections)
                    {
                        var appConnection = connection.Value;
                        if (appConnection != null && !string.IsNullOrEmpty(appConnection?.Pwd) && string.IsNullOrEmpty(appConnection?.DecryptedPwd))
                            appConnection.DecryptedPwd = DecryptPWD(appConnection.Pwd);

                        _appConnections.TryAdd(connection.Key, appConnection);
                    }

                    foreach (var settings in appSettingsConfig.AppSettings)
                    {
                        var redisSettings = settings.Value?.Redis;

                        if (redisSettings != null && !string.IsNullOrEmpty(redisSettings?.Pwd) && string.IsNullOrEmpty(redisSettings?.DecryptedPwd))
                            redisSettings.DecryptedPwd = DecryptPWD(redisSettings.Pwd);

                        _redisConnections.TryAdd(settings.Key, redisSettings);

                    }

                    var rmqueueSettings = appSettingsConfig.RMQSettings;
                    RMQueueUrl = "amqp://" + rmqueueSettings.RMQueueUser + ":" + rmqueueSettings.RMQueuePassword + "@" + rmqueueSettings.RMQueueHost + ":" + rmqueueSettings.RMQueuePort;
                }
                else
                {
                    AppConnection appConnection = appSettingsConfig.AppConnections.TryGetValue(appName, out var connection) ? connection : null;                    
                    if (appConnection != null && !string.IsNullOrEmpty(appConnection?.Pwd) && string.IsNullOrEmpty(appConnection?.DecryptedPwd))
                        appConnection.DecryptedPwd = DecryptPWD(appConnection.Pwd);

                    if (appConnection != null)
                        _appConnections.TryAdd(appName, appConnection);

                    AppSettings appSettings = appSettingsConfig.AppSettings.TryGetValue(appName, out var settings) ? settings : null;
                    var redisSettings = appSettings?.Redis;

                    if (redisSettings != null && !string.IsNullOrEmpty(redisSettings?.Pwd) && string.IsNullOrEmpty(redisSettings?.DecryptedPwd))
                        redisSettings.DecryptedPwd = DecryptPWD(redisSettings.Pwd);

                    if (redisSettings != null)
                        _redisConnections.TryAdd(appName, redisSettings);
                }
                return appSettingsConfig;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading AppSettings.ini: {ex.Message}", ex);
            }
        }

        private object GetAppSettingsFromIni(string appName, string fieldName)
        {
            object result = null;
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    throw new FileNotFoundException($"AppSettings.ini file not found at: {_appSettingsPath}");
                }

                var jsonContent = File.ReadAllText(_appSettingsPath);
                var appSettingsConfig = JsonConvert.DeserializeObject<AppSettingsConfig>(jsonContent);

                if (appSettingsConfig == null)
                {
                    throw new InvalidOperationException("Failed to deserialize AppSettings.ini file");
                }

                AppSettings appSettings = appSettingsConfig.AppSettings.TryGetValue(appName, out var settings) ? settings : null;
                
                if (appSettings != null)
                {

                    ReloadAppSettings(appName, appSettings);

                    if (fieldName == "AxARMJSON")
                    {
                        result = appSettings.ARM;
                    }
                    else if(fieldName == "AxEmailSMTPJSON")
                    {
                        result = appSettings.EmailSettings;
                    }
                    else if (fieldName == "AxARMIntResJSON")
                    {
                        result = appSettings.InternalResources;
                    }
                    else if (fieldName == "AxSSOJSON")
                    {
                        result = appSettings.SSO;
                    }
                    else if (fieldName == "AxLicDomainJSON")
                    {
                        result = appSettings.LicDomain;
                    }
                    else if (fieldName == "AxFileServerJSON")
                    {
                        result = appSettings.FileConfig;
                    }
                    else if (fieldName == "AxMobileSettingsJSON")
                    {
                        result = appSettings.MobileSettings;
                    }
                    else if (fieldName == "AxARMExtResJSON")
                    {
                        result = appSettings.ExternalResources;
                    }
                    else if (fieldName == "AxDevOption")
                    {
                        result = appSettings.AxpDevOptsMenu;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading AppSettings.ini: {ex.Message}", ex);
            }

            return System.Text.Json.JsonSerializer.Serialize(result);
        }

        public AppConnection? GetAppConnection(string appName)
        {
            if (string.IsNullOrEmpty(appName))
                return null;

            var appConnections = _appConnections.TryGetValue(appName, out var connection) ? connection : null;

            if (appConnections == null)
            {
                ReloadSettings(appName);
                appConnections = _appConnections.TryGetValue(appName, out connection) ? connection : null;
            }            

            return appConnections;
        }

        private object GetAppSettingsNode(string appName,string nodeName)
        {
            var settings = GetAppSettingsNodeFromRedis(appName, nodeName);
            if (settings != null)
                return settings;
            else
                return GetAppSettingsFromIni(appName, nodeName);
        }
        public ARMSettings? GetARMSettings(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxARMJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<ARMSettings>(settings.ToString());
            return null;
        }

        public MobileSettings? GetMobileSettings(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxMobileSettingsJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<MobileSettings>(settings.ToString());
            return null;

        }

        public JObject? GetExternalResources(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxARMExtResJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<JObject> (settings.ToString());
            return null;

        }

        public RedisSettings? GetRedisSettings(string appName)
        {
            var redisSettings = _redisConnections.TryGetValue(appName, out var settings) ? settings : null;
            if (redisSettings == null)
            {
                ReloadSettings(appName);
                redisSettings = _redisConnections.TryGetValue(appName, out settings) ? settings : null;
            }
            return redisSettings;
        }

        public FileConfig? GetFileConfig(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxFileServerJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<FileConfig>(settings.ToString());
            return null;

        }

        public EmailSettings? GetEmailSettings(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxEmailSMTPJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<EmailSettings>(settings.ToString());
            return null;
        }

        public Dictionary<string, Dictionary<string, string>>? GetSSOSettings(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxSSOJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(settings.ToString());
            return null;
        }

        public LicDomain? GetLicDomain(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxLicDomainJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<LicDomain>(settings.ToString());
            return null;
        }

        public InternalResources? GetInternalResources(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxARMIntResJSON");
            if (settings != null)
                return JsonConvert.DeserializeObject<InternalResources>(settings.ToString());
            return null;
        }
        public AxpDevOptsMenu? GetAxDevOption(string appName)
        {
            var settings = GetAppSettingsNode(appName, "AxDevOption");
            if (settings != null)
                return JsonConvert.DeserializeObject<AxpDevOptsMenu>(settings.ToString());
            return null;
        }


        public void ReloadSettings(string appName)
        {
            lock (_lock)
            {
                _appSettingsConfig = LoadAppSettings(appName);
            }
        }
        #region Password Decrypt methods

        public string DecryptPWD(string insId)
        {
            int i = 0;
            string pwd = string.Empty;
            string cpuId = string.Empty;

            Int32.TryParse(insId.Substring(0, 4), out i);
            insId = insId.Substring(4);
            i = i * 4;
            cpuId = insId.Substring(0, i);
            insId = insId.Substring(i);
            i = insId.Length;
            insId = insId.Substring(0, i - 3);
            pwd = GetPassword(insId, cpuId);

            return pwd;
        }

        public string GetPassword(string dtid, string mid)
        {
            int len, len1, i, k;
            string s = string.Empty, dbid = string.Empty;

            string temp = mid;
            while (temp.Length > 0)
            {
                string s1 = temp.Substring(0, 4);
                Int32.TryParse(s1, out k);

                // ✅ Equivalent to VB Chr(k)
                char c = Convert.ToChar(k);
                dbid += c;

                temp = temp.Substring(4);
            }

            len = dbid.Length;
            len1 = dtid.Length;

            if (len1 < len)
            {
                for (i = len1; i < len; i++)
                {
                    dtid += '0';
                }
            }

            string result = string.Empty;
            for (i = 0; i < len; i++)
            {
                // ✅ Equivalent to VB Chr(Asc(dbid[i]) - Asc(dtid[i]))
                int val = Convert.ToInt32(dbid[i]) - Convert.ToInt32(dtid[i]);
                result += Convert.ToChar(val);
            }

            return result;
        }

        #endregion

        public string GetRMQueueURL()
        {
            return RMQueueUrl;
        }

        private Dictionary<string, object> GetAppSettingsFromRedis(string appName) 
        {
            Dictionary<string, object> appSettings = new Dictionary<string, object>();
            try
            {
                var redisCache = new RedisCacheHelper();
                redisCache.AppName = appName;
                var redisSettings = GetRedisSettings(appName);

                if (redisSettings != null)
                {
                    redisCache.RedisSettings = redisSettings;

                    redisCache.Redis = redisCache.OpenConnectionAsync().GetAwaiter().GetResult();

                    string[] fields = new string[]
                    {
                        "AxARMJSON",
                        "AxEmailSMTPJSON",
                        "AxARMIntResJSON",
                        "AxSSOJSON",
                        "AxLicDomainJSON",
                        "AxFileServerJSON",
                        "AxMobileSettingsJSON",
                        "AxARMExtResJSON",
                        "AxDevOption"
                    };

                    if (redisCache.Redis.IsConnected)
                    {
                        appSettings = redisCache.ObjectHashGetFields($"{appName}-AxAccessCodeSettings", fields);
                    }

                    redisCache.CloseAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
            }   
            return appSettings;
        }

        private object GetAppSettingsNodeFromRedis(string appName, string fieldName)
        {
            object value = null;
            try
            {
                var redisCache = new RedisCacheHelper();
                redisCache.AppName = appName;
                var redisSettings = GetRedisSettings(appName);

                if (redisSettings != null)
                {
                    redisCache.RedisSettings = redisSettings;

                    redisCache.Redis = redisCache.OpenConnectionAsync().GetAwaiter().GetResult();

                    if (redisCache.Redis.IsConnected)
                    {
                        value = redisCache.ObjectHashGet($"{appName}-AxAccessCodeSettings", fieldName);
                    }

                    redisCache.CloseAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
            }
            return value;
        }

        private bool SetAppSettingsToRedis(string appName, Dictionary<string, object> appSettings)
        {
            bool result = false;
            try
            {
                var redisCache = new RedisCacheHelper();
                redisCache.AppName = appName;
                var redisSettings = GetRedisSettings(appName);

                if (redisSettings != null)
                {
                    redisCache.RedisSettings = redisSettings;

                    redisCache.Redis = redisCache.OpenConnectionAsync().GetAwaiter().GetResult();
                    

                    if (redisCache.Redis.IsConnected)
                    {
                        result = redisCache.ObjectHashSetFields($"{appName}-AxAccessCodeSettings", appSettings);
                    }

                    redisCache.CloseAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
            }
            return result;
        }

        private void ReloadAppSettings(string appName, AppSettings appSettings)
        {
            try
            {
                Dictionary<string, object> appSettingsToRedis = new Dictionary<string, object>();
                var appSettingsFromRedis = GetAppSettingsFromRedis(appName);
                if (!appSettingsFromRedis.ContainsKey("AxARMJSON"))
                {
                    appSettingsToRedis.Add("AxARMJSON", appSettings.ARM);
                }

                if (!appSettingsFromRedis.ContainsKey("AxEmailSMTPJSON"))
                {
                    appSettingsToRedis.Add("AxEmailSMTPJSON", appSettings.EmailSettings);
                }

                if (!appSettingsFromRedis.ContainsKey("AxARMIntResJSON"))
                {
                    appSettingsToRedis.Add("AxARMIntResJSON", appSettings.InternalResources);
                }

                if (!appSettingsFromRedis.ContainsKey("AxSSOJSON"))
                {
                    appSettingsToRedis.Add("AxSSOJSON", appSettings.SSO);
                }

                if (!appSettingsFromRedis.ContainsKey("AxLicDomainJSON"))
                {
                    appSettingsToRedis.Add("AxLicDomainJSON", appSettings.LicDomain);
                }

                if (!appSettingsFromRedis.ContainsKey("AxFileServerJSON"))
                {
                    appSettingsToRedis.Add("AxFileServerJSON", appSettings.FileConfig);
                }

                if (!appSettingsFromRedis.ContainsKey("AxMobileSettingsJSON"))
                {
                    appSettingsToRedis.Add("AxMobileSettingsJSON", appSettings.MobileSettings);
                }

                if (!appSettingsFromRedis.ContainsKey("AxARMExtResJSON"))
                {
                    appSettingsToRedis.Add("AxARMExtResJSON", appSettings.ExternalResources);
                }

                if (!appSettingsFromRedis.ContainsKey("AxDevOption"))
                {

                    var devOptions = appSettings?.AxpDevOptsMenu;
                    if(devOptions != null)
                        appSettingsToRedis.Add("AxDevOption", appSettings.AxpDevOptsMenu);
                    else
                        appSettingsToRedis.Add("AxDevOption", "nooptions");
                }

                if (appSettingsToRedis.Count > 0)
                {
                    SetAppSettingsToRedis(appName, appSettingsToRedis);
                }
            }
            catch
            {
            }
        }
    }
}
