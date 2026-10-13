using ARMCommon.Model;

namespace AxAuth
{
    public interface ILoginRepository
    {
        Task<string> GetAxPropsXMLFromDB();
        Task<SQLResult> GetUserDetails(string userName);
        Task<LoginUserData> GetUserAuthDetails(string userName);
        Task<SQLResult> GetUserRoles(string userName);
        Task<SQLResult> GetUserResponsibilities(string userName);
        Task<ScalarResult> UpdateLoginTry(string userName, int loginTry);
        Task<SQLResult> GetUserPwdPolicy(string userName);
        Task<SQLResult> GetAxGloTstruct();
        Task<SQLResult> GetAxGloFields();
        Task<SQLResult> GetAxGloUserData(string userName);
        Task<NonQueryResult> AddSSOColumn(string columnName);
        Task<NonQueryResult> UpdateSSOKeyForUser(LoginInput input, string columnName);

        Task<bool> StoreUserSessionInCache(string sessionId, string data, int expiry = 60);
        Task<string> GetUserSessionFromCache(string sessionId);
        Task<bool> DeleteUserSessionFromCache(string sessionId);
        Task<SQLResult> GetDBVariablesFunctionsList();
        Task<SQLResult> GetDBVariablesFromFunction(string dbFunction, string dbFunctionParams);
    }

}
