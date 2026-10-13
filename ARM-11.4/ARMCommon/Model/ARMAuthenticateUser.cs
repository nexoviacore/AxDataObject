using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ARMAuthenticateUser
    {
        public string appname { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string usergroup { get; set; }
        public string mobile { get; set; }

    }
    public class ARMNotificationTemplate
    {
        [Key]
        public int Id { get; set; }
        public string AppName { get; set; }
        public string TemplateId { get; set; }

        public string TemplateString { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }


    }
}
