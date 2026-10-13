using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ARMHtml
    {
        public Guid ID { get; set; }
        public string DefinitionID { get; set; }
        public string DefinitionHTML { get; set; }
        public string AppName { get; set; }
        public string? DataSource { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool AllowAnonymousAccess { get; set; }

        public string[]? DataSources { get; set; }

        [NotMapped]
        public List<App>? app1 { get; set; }


        public List<Guid>? selectedUserGroups { get; set; }

        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
    }
}
