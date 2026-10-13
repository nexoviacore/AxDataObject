using System;
namespace ARMCommon.Model
{
    public class AxListData
    {
        public string ARMSessionId { get; set; }
        public string? TransId { get; set; }
        public string? ADSName { get; set; }
        public string? AxSessionId { get; set; }
        public List<string>? ADSNames { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; } = "VIEW";
        public bool Trace { get; set; } = false;
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public string? Fields { get; set; }
        public string? Filter { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? GlobalParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? ViewFilters { get; set; } = new Dictionary<string, string>();
        public List<string>? Roles { get; set; } = new List<string>();
        public string? RolesStr { get; set; }
        public int? PageNo { get; set; }
        public int? PageSize { get; set; }
        public string? Constraint { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? ConnectedData_TransIds { get; set; }
        public List<Dictionary<string, Object>>? Filters { get; set; } = new List<Dictionary<string, Object>>();
        public List<Dictionary<string, string>>? Sorting { get; set; } = new List<Dictionary<string, string>>();
        public List<string>? Select_Columns { get; set; } = new List<string>();
        public List<string>? GroupBy_Columns { get; set; } = new List<string>();
        public Dictionary<string, string>? Aggregations { get; set; } = new Dictionary<string, string>();
        public bool GetAllRecordsCount { get; set; } = false;
        public bool CachePermissions { get; set; } = true;
        public bool RefreshCache { get; set; } = false;
        public bool ADS { get; set; } = false;
        public string AxClient_DateFormat { get; set; } = "";
        public string Dimensions { get; set; } = "";
    }
}
