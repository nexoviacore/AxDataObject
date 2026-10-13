using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{

    public class ARMDataSource
    {
        public Guid ID { get; set; }
        public string DataSourceID { get; set; }
        public string AppName { get; set; }
        public string Type { get; set; }
        public string? SQLScript { get; set; }
        public string? DataSourceDesc { get; set; }
        public string? DataSourceURL { get; set; }
        public string? DataSourceFormat { get; set; }
        public string? RequestType { get; set; }
        public bool IsActive { get; set; }
        public bool? IsMasterData { get; set; }
        public bool IsDataSyncActive { get; set; }
        public int? DataSyncInterval { get; set; }
        public DateTime? LastSyncedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? DataSyncInitFormat { get; set; }
        public bool AllowAnonymousAccess { get; set; }

        public List<Guid>? selectedDataSyncDataSources { get; set; }

        public string? DataSyncAPIList { get; set; }

        public List<ARMDataSource>? DataSyncDataSources { get; set; }
        public List<Guid>? selectedUserGroups { get; set; }

        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }
    }
    public class APIDefinitions
    {
        public Guid ID { get; set; }
        public string DataSourceID { get; set; }
        public Guid ARMDataSourceID { get; set; }
        public string AppName { get; set; }
        public string? DataSyncAPIList { get; set; }
        public List<Guid>? selectedDataSyncDataSources { get; set; }
        public List<Guid>? selectedUserGroups { get; set; }
        public string? RequestType { get; set; }
        public string? DataSourceDesc { get; set; }
        public string? DataSourceURL { get; set; }
        public string? DataSourceFormat { get; set; }

        public bool IsActive { get; set; }
        public bool? IsMasterData { get; set; }
        public bool IsDataSyncActive { get; set; }
        public int? DataSyncInterval { get; set; }
        public DateTime? LastSyncedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? DataSyncInitFormat { get; set; }
        public bool AllowAnonymousAccess { get; set; }
        public bool iscached { get; set; }
        public int? expiry { get; set; }

        [NotMapped]
        public List<APIDefinitions>? DataSyncDataSources { get; set; }
        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }
    }


    public class SQLDataSource
    {
        public Guid ID { get; set; }
        public string DataSourceID { get; set; }
        public Guid ARMDataSourceID { get; set; }
        public string AppName { get; set; }
        public string? DataSyncAPIList { get; set; }
        public List<Guid>? selectedUserGroups { get; set; }
        public List<Guid>? selectedDataSyncDataSources { get; set; }
        public string SQLScript { get; set; }
        public string? DataSourceDesc { get; set; }
        public bool IsActive { get; set; }
        public bool? IsMasterData { get; set; }
        public bool IsDataSyncActive { get; set; }
        public bool iscached { get; set; }
        public int? expiry { get; set; }
        public int? DataSyncInterval { get; set; }
        public DateTime? LastSyncedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? DataSyncInitFormat { get; set; }
        [NotMapped]
        public List<SQLDataSource>? DataSyncDataSources { get; set; }

        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }
    }

    public class ARMGetDataRequest
    {
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string datasource { get; set; }
        public string? ARMSessionId { get; set; }
        public string? dataId { get; set; }
        public string? RefreshQueueName { get; set; }
        public Dictionary<string, string>? sqlParams { get; set; }
        public Dictionary<string, string>? apiparams { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
    }
}
