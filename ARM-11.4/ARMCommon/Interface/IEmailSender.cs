using ARMCommon.Model;

namespace ARMCommon.Interface
{
    public interface IEmailSender
    {
        void SendEmail(Message message);
        Task SendEmailAsync(Message message);
    }
}
