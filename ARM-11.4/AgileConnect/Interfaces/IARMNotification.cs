using ARMCommon.Model;

namespace AgileConnect.Interfaces
{
    public interface IARMNotification
    {
        IEnumerable<ARMNotificationTemplate> NotificationTemplateList();
        IEnumerable<ARMNotificationTemplate> NotificationTemplateList(string Appname);
        ARMNotificationTemplate GetTemplateById(int id);
        bool DeleteTemplate(int id);
    }
}
