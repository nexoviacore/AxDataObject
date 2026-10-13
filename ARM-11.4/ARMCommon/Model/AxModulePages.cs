using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class AxModulePages
    {
        [Key]
        public Guid Id { get; set; }
        public string? appName { get; set; }
        public string PageTitle { get; set; }
        public string PageName { get; set; }
        public string PageIcon { get; set; }
        public string? Module { get; set; }
        public string? SubModule { get; set; }
        public string PageDataTable { get; set; }
        public string PageOwner { get; set; }
        public List<string>? Forms { get; set; }
        public string? KeyField { get; set; }
        public List<string>? AcessControl { get; set; }
        public bool AddQuickAccess { get; set; }
        public bool navigation { get; set; }
        [NotMapped]
        public List<AxInlineForm>? AxInlineForm { get; set; }

        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
        [NotMapped]
        public List<AxModule>? Modules { get; set; }
        [NotMapped]
        public List<AxSubModule>? SubModules { get; set; }

        [NotMapped]
        public List<App>? app1 { get; set; }
        public string? formdata { get; set; }

    }
    public class AxModule
    {
        [Key]
        public Guid Id { get; set; }
        public string ModuleName { get; set; }
        public string ModuleDescription { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }
        public string? appName { get; set; }

    }
    public class AxSubModule
    {
        [Key]
        public Guid Id { get; set; }
        public string SubModuleName { get; set; }
        public string SubModuleDescription { get; set; }

        [NotMapped]
        public List<App>? app1 { get; set; }
        public string? appName { get; set; }
    }
}
