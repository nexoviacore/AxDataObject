using ARMCommon.Model;

namespace ARMNotificationHub
{
    public interface IARMNotificationService
    {
      abstract Task<object> ProcessARMMobileNotification(MobileNotification notification);
       abstract Task<bool> DisableMobileNotificationForUserAsync(string ARMSessionId);
      //abstract Task<IActionResult> SendEmailNotification(ARMNotify model);
    }
}
