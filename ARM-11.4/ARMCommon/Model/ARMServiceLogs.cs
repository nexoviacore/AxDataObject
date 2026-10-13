using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ARMServiceLogs
    {
        [Key]
        public Guid LogID { get; set; }
        public int? InstanceID { get; set; }
        public string? ServiceName { get; set; }
        public string? Status { get; set; }
        public DateTime? LastOnline { get; set; }

        public DateTime? StartOnTime { get; set; }
        public string? Server { get; set; }
        public string? Folder { get; set; }
        public string? OtherInfo { get; set; }
        [NotMapped]
        public Dictionary<string, object>? OtherInfoJson { get; set; }

        public bool?  IsMailSent { get; set; }
    }


}
