
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ARMUserGroup
    {
        public Guid ID { get; set; }
        public string AppName { get; set; }
        public string Name { get; set; }
        public string? GroupType { get; set; }
        public string? InternalAuthMethod { get; set; }
        public string? InternalAuthUrl { get; set; }
        public string? InternalAuthRequest { get; set; }
        public string? InternalAuthResponse { get; set; }
        public bool IsActive { get; set; }

        public List<string>? Roles { get; set; }
        [NotMapped]
        public List<Role>? Role { get; set; }


    }
}
