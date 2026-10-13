using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IARMLogin
    {
        abstract Task<bool> AddUser(string appname, string username, string password, string email, string usergroup, string mobileno, Guid usergroupid, string registrationKey = "");
        abstract Task<object> SigninUser(ARMLoginUser userModel, ARMUserGroup userGroup, string sessionId);
        abstract bool UserGroupExists(string userGroupName);
        abstract Task<ARMUserGroup> GetUserGroup(string userGroupName);
        abstract bool UserExists(string userName, string appName, string usergroup);
        bool SSOUserExists(string userName, string appName);
        abstract bool AppExists(string appName);
        abstract bool UserEmailExists(string userEmail, string appname);
        abstract Task<bool> SaveUserRegistrationDetails(ARMUser user, ARMUserGroup userGroup, string otp, string regId);
        abstract Task<string> GetInternalUserDetails(ARMUserGroup userGroup, string userId);
        abstract Task<bool> ValidateGoogleSSO(string ssodetailTokenId, string userid, string subId);
        abstract Task<bool> AddSSOUser(ARMUser user);

        abstract bool BiometricEnabled(string appName);
        abstract Task<bool> AddARMDevices(UserDevice user);
        abstract bool DeviceExists(string deviceId);

        abstract Task<object> ChangePassword(string ARMSessionId, string currentPassword, string updatedpassword);
        abstract Task<object> ForgotPassword(string email, string username, string usergroup, string appname);
        abstract Task<bool> RemoveARMUserDevices(string deviceId);
        abstract Task<UserDevice> GetUserDevices(string username, string appname, string deviceid);

        abstract Task<DataTable> ValidatePowerUserWithPassword(string username, string password, string appname);

        abstract Task<DataTable> ValidatePowerUsers(string username, string appname);
        Task<List<string>> GetPowerUserRoleList(string username, string appname);
        abstract Task<ARMUser> GetARMUsers(string appName, string username, string password, string usergroup);

        abstract Task<Dictionary<string, string>> StoreSessionValues(string sessionId, string userRoleslist, string username, string appname, string usergroup, string grouptype, string token);

        abstract Task<Dictionary<string, string>> StoreSessionValuesCardData(string sessionId, string userRoleslist, string username, string appname, string usergroup, string grouptype, string token, string md5Hash);
        abstract Task<object> ValidateAndUpdatePassword(string ARMSessionId, string currentPassword, string updatedpassword);

        Task<bool> ResetPassword(string email, string updatedpassword, string appname);

        Task<APIResult> SignInV2(ARMLoginUser request, ARMUserGroup userGroup,string sessionId);
        Task<APIResult> AxStartSession(ARMLoginUser request, ARMUserGroup userGroup, string sessionId, bool sso = false);

        Task<APIResult> RefreshSession(string ARMSessionId);
    }
}
