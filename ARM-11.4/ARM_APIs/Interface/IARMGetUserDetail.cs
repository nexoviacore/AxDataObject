using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IARMGetUserDetail
    {
        abstract Task<ARMUser> GetUserProfileDetails(string ARMSessionId);
        abstract Task<bool> UpdateUserProfileDetails(string ARMSessionId, string email, string mobileno);
        abstract Task<bool> MobileExist(string mobile);
        abstract Task<bool> ValidateUserByEmail(string email);



       abstract Task<bool> SaveLogindetailsToRedis(string otp, string regId);
    }
}
