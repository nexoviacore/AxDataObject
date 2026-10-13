using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ARMCommon.Model
{
    public class AppSettingsConfig
    {
        public Dictionary<string, AppConnection> AppConnections { get; set; } = new Dictionary<string, AppConnection>();
        public Dictionary<string, AppSettings> AppSettings { get; set; } = new Dictionary<string, AppSettings>();

        public RMQueueSettings RMQSettings { get; set; } = new RMQueueSettings();
    }
    public class AppConnection
    {
        public string Type { get; set; } = string.Empty;
        public string StructUrl { get; set; } = string.Empty;
        public string Db { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DbCon { get; set; } = string.Empty;
        public string DbUser { get; set; } = string.Empty;
        public string Pwd { get; set; } = string.Empty;
        public string DecryptedPwd { get; set; } = string.Empty;
        public string DataUrl { get; set; } = string.Empty;
        public string ODBCDbUser { get; set; } = string.Empty;
    }
    public class ARMSettings
    {
        public string ARM_PrivateKey { get; set; } = string.Empty;
        public string ARM_Notification_URL { get; set; } = string.Empty;
        public string ARM_Notification_ExpiryHours { get; set; } = string.Empty;
        public string ARM_Notification_MaxPerUser { get; set; } = string.Empty;
        public string ARM_URL { get; set; } = string.Empty;
        public string ARM_Scripts_URL { get; set; } = string.Empty;
        public string PEG { get; set; } = string.Empty;
        public string ScriptsPath { get; set; } = string.Empty;
        public string scriptsUrlPath { get; set; } = string.Empty;
        public string ExpiryMinutes { get; set; } = string.Empty;
    }

    public class RedisSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Pwd { get; set; } = string.Empty;
        public string DecryptedPwd { get; set; } = string.Empty;
    }

    public class MobileSettings
    {
        public string Mob_CitizenUser { get; set; } = string.Empty;
        public string Mob_Geofencing { get; set; } = string.Empty;
        public string Mob_Geotag { get; set; } = string.Empty;
        public string Mob_FingerAuth { get; set; } = string.Empty;
        public string Mob_FaceAuth { get; set; } = string.Empty;
        public string Mob_ForceLogin { get; set; } = string.Empty;
        public string Mob_ForceLoginUsers { get; set; } = string.Empty;
    }

    public class FileConfig
    {
        public string FileUploadPath { get; set; } = string.Empty;
        public string FileDownloadPath { get; set; } = string.Empty;
        public string FileServerMapUsername { get; set; } = string.Empty;
        public string FileServerMapPwd { get; set; } = string.Empty;
        public string AxAttachmentSize { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Pwd { get; set; } = string.Empty;
        public string Arm_Url { get; set; } = string.Empty;
        public string EmailSubject { get; set; } = string.Empty;
        public string EmailBody { get; set; } = string.Empty;
        public string SmsContent { get; set; } = string.Empty;
        public string AxOTPAuth { get; set; } = string.Empty;
        public string AxOTPAuthCahrs { get; set; } = string.Empty;
        public string AxOTPAuthExpiry { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public ARMSettings ARM { get; set; } = new ARMSettings();
        public MobileSettings MobileSettings { get; set; } = new MobileSettings();
        public RedisSettings Redis { get; set; } = new RedisSettings();
        public FileConfig FileConfig { get; set; } = new FileConfig();
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();
        public LicDomain LicDomain { get; set; } = new LicDomain();
        public InternalResources InternalResources { get; set; } = new InternalResources();
        public RMQueueSettings RMQueue { get; set; } = new RMQueueSettings();
        public Dictionary<string, Dictionary<string, string>> SSO { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        public JObject ExternalResources { get; set; } = new JObject();
        public AxpDevOptsMenu AxpDevOptsMenu { get; set; }
    }

    public class LicDomain
    {
        public string DomainName { get; set; } = string.Empty;
    }

    public class InternalResources
    {
        public string AxRMQAPIURL { get; set; } = string.Empty;
        public string AxSignalRapiURL { get; set; } = string.Empty;
        public string AxFCMSendMsgURL { get; set; } = string.Empty;
        public string AxRapidSaveURL { get; set; } = string.Empty;
        public string axpegemailactionurl { get; set; } = string.Empty;
        public string AxScriptsAPIURL { get; set; } = string.Empty;

    }

    public class RMQueueSettings
    {
        public string RMQueueHost { get; set; } = string.Empty;
        public int RMQueuePort { get; set; }
        public string RMQueueUser { get; set; } = string.Empty;
        public string RMQueuePassword { get; set; } = string.Empty;
    }

    public class AxpDevOptsMenu
    {
        public string Options { get; set; } 
    }
}

