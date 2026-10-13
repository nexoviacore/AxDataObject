namespace ARMCommon.Interface
{
    public interface INotificationHelper
    {
        abstract string GenerateOTP();
        abstract Task<object> SendEmailOTP(string senderEmail, string OTP, string username);
    }
}
