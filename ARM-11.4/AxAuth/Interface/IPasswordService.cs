using ARMCommon.Model;

namespace AxAuth
{
    public interface IPasswordService
    {
        Task<ARMResult> ChangePasswordAsync(ChangePasswordInput input);
        Task<ARMResult> ForgotPasswordAsync(ForgotPasswordInput input);
    }
}
