using ARMCommon.Model;

namespace ARMCommon.Interface
{
    public interface IAppSettingsHelper
    {
        Dictionary<string, string> GetDBConfigurations(string appName);
        string GetARMUrl(string appName);
        string GetARMNotificationUrl(string appName);
        string GetARMNotificationExpiry(string appName);
        string GetARMNotificationMaxLimit(string appName);
        string GetARMPrivateKey(string appName);
        string GetARMScriptsURL(string appName);
        string GetLicDomain(string appName);
        AppConnection GetAppConnection(string appName);
        RedisSettings GetRedisSettings(string appName);
        ARMSettings GetARMSettings(string appName);
        FileConfig GetFileConfig(string appName);
        EmailSettings GetEmailSettings(string appName);
        MobileSettings GetMobileSettings(string appName);
        InternalResources GetInternalResources(string appName);
        string GetRMQueueURL();
        Dictionary<string, Dictionary<string, string>> GetSSOSettings(string appName);
        Int32 GetARMTokenExpiryMinutes(string appName);
        public AxpDevOptsMenu GetAxpDevOption(string appName);
    }
}
