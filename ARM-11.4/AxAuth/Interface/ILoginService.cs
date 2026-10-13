using ARMCommon.Model;

namespace AxAuth
{
    public interface ILoginService
    {
        ARMResult GetMobileLoginSettings(MobileLoginSettings input);
        Task<ARMResult> GetLoginUserDetailsAsync(LoginUserInput input);
        Task<ARMResult> ValidateOTPAsync(LoginInput input);
        Task<ARMResult> ResendOTPAsync(LoginInput input);        
        Task<ARMResult> SigninAsync(LoginInput input);

        Task<APIResult> SignoutAsync(ARMSession session);

        Task<ARMResult> ConnectToAxpert(ARMAxpertConnect axpert);
        Task<ARMResult> ConnectFromAxpert(ARMAxpertConnect axpert);

        Task<APIResult> AxValidateSession(ARMAxpertConnect axpert);
    }

}
