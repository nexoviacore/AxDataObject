using System.Data;

namespace AxAuth

{
    public class MobileLoginSettings
    {
        public string AppName { get; set; }
    }

    public class LoginInput
    {
        public string AppName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SessionId { get; set; }

        // Optional fields
        public string ARMSessionId { get; set; }
        public string Seed { get; set; }
        public string SSO { get; set; }
        public string SingleSigninKey { get; set; }
        public bool Trace { get; set; }
        public string Other { get; set; }
        public string Language { get; set; }
        public string IPAddress { get; set; }
        public string OtpAuth { get; set; }
        public string OtpLoginKey { get; set; }
        public string Otp { get; set; }
        public bool Globalvars { get; set; } = false;
        public bool ClearPreviousSession { get; set; } = false;
    }

    public class LoginUserInput
    {
        public string AppName { get; set; }
        public string Username { get; set; }
        public bool Trace { get; set; }
    }

    public class LoginData
    {
        public DateTime StartTime { get; set; }
        public string PropsXML { get; set; }
        public DataRow UserRow { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> GlobalVars { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> UserVars { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class UserData
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string? UserGroup { get; set; }
        public string? Build { get; set; }
        public string? Manage { get; set; }
        public string? Email { get; set; }
        public string? PageAccess { get; set; }
        public string? Active { get; set; }
        public string? ReportingTo { get; set; }
        public string? IsFirstTime { get; set; }
        public int? LoginTry { get; set; }
        public int? PwdExpDays { get; set; }
        public string? AppManager { get; set; }
        public string? Debug { get; set; }
        public string? Nickname { get; set; }
        public string? Mobile { get; set; }
        public string? PwdAuth { get; set; }
        public string? OtpAuth { get; set; }
        public string? Homepage { get; set; }
        public Object? SingleLoginKey { get; set; }
        public string? UserNameAsEmailId { get; set; }
        public string? IsIp { get; set; }
        public string? IpAddr { get; set; }
        public string? ReportInto { get; set; }
        public string? AppMgrAccess { get; set; }
        public string? PortalUser { get; set; }
        public string? AuthKey { get; set; }
        public string? UserKey { get; set; }
        public Object? SamlSingleLoginKey { get; set; }

        public string? UserType { get; set; }
        public string? ActFlag { get; set; }
        public int? GroupNo { get; set; }
        public string? WorkFlow { get; set; }
        public Object? CardDetails { get; set; }
        public string? WebSkin { get; set; }
        public string? ImportStruct { get; set; }
        public string? ExportStruct { get; set; }
        public string? IsUniqueHybrid { get; set; }
        public string? FirstTimeLogin { get; set; }

        public string? AxLang { get; set; }
        public Dictionary<string, string> SSOKeys { get; set; } = new Dictionary<string, string>();
        public string? AxGlo { get; set; }
        public string? AxGloRecordId { get; set; }

    }

    public class LoginUserData
    {
        public string UserName { get; set; }
        public string? Email { get; set; }
        public string? Active { get; set; }
        public string? IsFirstTime { get; set; }
        public string? Mobile { get; set; }
        public string? PwdAuth { get; set; }
        public string? OtpAuth { get; set; }
        public string? ActFlag { get; set; }
        public string? FirstTimeLogin { get; set; }
        public string? AxLang { get; set; }
        public string? StaySignedIn { get; set; }
        public string? SigninExpiry { get; set; }
    }

    public class LoginUserResponse
    {
        public string? PwdAuth { get; set; }
        public string? OtpAuth { get; set; }
        public string? AxLang { get; set; }
    }    

}
