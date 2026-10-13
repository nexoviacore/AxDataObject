using ARMCommon.Model;

namespace AxAuth
{
    public interface IPasswordRepository
    {
        Task<SQLResult> GetUserDetails(string userName);
        Task<SQLResult> GetUserForPasswordReset(string userName, string email);
        Task<SQLResult> GetUserPwdPolicy(string userName);
        Task<TransactionResult> UpdatePassword(string userName, string newPasswordHash, string oldPasswordHash, bool isFirstTime, bool pwdPolicyExists, string pwdChangeFlag);
    }
}
