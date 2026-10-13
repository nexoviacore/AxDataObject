using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class AxInlineForm
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string formIcon { get; set; }
        public string? Module { get; set; }
        public string? SubModule { get; set; }
        public string StatusValue { get; set; }
        public string FormText { get; set; }
        public List<string>? AcessControl { get; set; }
        public List<string>? HideInMyList { get; set; }
        public string? FormCreator { get; set; }
        public string? FormCreatedOn { get; set; }

        public string? FormUpdatedBy { get; set; }
        public string? FormUpdatedOn { get; set; }
        public bool AddQuickAccess { get; set; }
        public bool EnableSend { get; set; }

        public List<string>? SelectedSendOnlyTo { get; set; }
        public string? AutoSend { get; set; }

        public string? AutoSendCondition { get; set; }

        public string? FormValidations { get; set; }

        public string? Compuations { get; set; }
        public string? ButtonCaption { get; set; }

        public string? ButtonIcon { get; set; }

        public string? ButtonScript { get; set; }

        public string? QueueName { get; set; }

        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
        [NotMapped]
        public List<AxModule>? Modules { get; set; }
        [NotMapped]
        public List<AxSubModule>? SubModules { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }
        public string? appName { get; set; }


    }
}
