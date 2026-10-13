using AgileConnect.Interfaces;
using ARMCommon.Helpers;
using ARMCommon.Model;

namespace AgileConnect.Services
{
    public class ARMNotificationService : IARMNotification
    {
        private readonly DataContext _context;


        public ARMNotificationService(DataContext context, IConfiguration configuration)
        {
            _context = context;

        }
        public ARMNotificationTemplate GetTemplateById(int id)
        {
            return GetTemplate(id);
        }

        private ARMNotificationTemplate GetTemplate(int id)
        {
            var user = _context.NotificationTemplate.Find(id);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            return user;
        }

        public IEnumerable<ARMNotificationTemplate> NotificationTemplateList()
        {
            var users = _context.NotificationTemplate.ToList();

            return users;
        }

        public IEnumerable<ARMNotificationTemplate> NotificationTemplateList(string Appname)

        {
            var users = _context.NotificationTemplate.Where(p => p.AppName == Appname).ToList();
            //var users = _context.NotificationTemplate.ToList();

            return users;
        }

        public bool DeleteTemplate(int id)
        {
            var user = GetTemplateById(id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            // _context.Entry(aRMUserResult).State= EntityState.Modified;
            _context.Remove(user);
            var i = _context.SaveChanges();
            if (i > 0)
            {
                return true;
            }
            else
                return false;

        }

    }
}
