using ARMCommon.Model;
using Newtonsoft.Json.Linq;

namespace ARMCommon.Interface
{
    public interface IAppSettingsService
    {
        AppSettingsConfig AppSettingsConfig { get; }
        AppConnection? GetAppConnection(string appName);
        ARMSettings? GetARMSettings(string appName);
        Dictionary<string, Dictionary<string, string>>? GetSSOSettings(string appName);
        MobileSettings? GetMobileSettings(string appName);
        RedisSettings? GetRedisSettings(string appName);
        FileConfig? GetFileConfig(string appName);
        EmailSettings? GetEmailSettings(string appName);
        LicDomain? GetLicDomain(string appName);
        InternalResources? GetInternalResources(string appName);
        void ReloadSettings(string appName);
        JObject? GetExternalResources(string appName);
        string GetRMQueueURL();
        AxpDevOptsMenu? GetAxDevOption(string appName);
    }
}
