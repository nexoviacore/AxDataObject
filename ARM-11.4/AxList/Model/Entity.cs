using System.Data;

namespace AxList
{
    public class Entity
    {
        public string? Page { get; set; }
        public string? ARMSessionId { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? EntityName { get; set; }
        public int? PageNo { get; set; }
        public int? PageSize { get; set; }        
        public Dictionary<string, string>? GlobalParams { get; set; }
        public bool? MetaData { get; set; }
        public string? TransId { get; set; }
        public string? KeyValue { get; set; }
        public string? Filter { get; set; }
        public string? Fields { get; set; }
        public string? UserName { get; set; }
        public Int64? RecordId { get; set; }
        public List<Dictionary<string, Object>>? Filters { get; set; }
        public Dictionary<string, string>? ViewFilters { get; set; }
        public string? Roles { get; set; }
        public List<string>? PropertiesList { get; set; }
        public bool? Trace { get; set; }
        public bool? OnlyData { get; set; }
        public bool? MetaDataFromDB { get; set; }

        public bool CalledFromDataList { get; set; } = false;
    }

    public class EntityCharts
    {
        public string? ARMSessionId { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? EntityName { get; set; }
        public string? TransId { get; set; }
        public string? Condition { get; set; }
        public string? Criteria { get; set; }
        public string? KeyValue { get; set; }
        public Int64? RecordId { get; set; }
        public string? UserName { get; set; }
        public Dictionary<string, string>? ViewFilters { get; set; }
    }

    public class EntityForm
    {
        public string? Page { get; set; }
        public string? ARMSessionId { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? EntityName { get; set; }
        public string? TransId { get; set; }
        public string? KeyValue { get; set; }
        public string? UserName { get; set; }
        public bool? SubEntityMetaData { get; set; }
        public string? Condition { get; set; }
        public string? Criteria { get; set; }
        public Int64? RecordId { get; set; }
        public Dictionary<string, string>? ViewFilters { get; set; }
        public List<Dictionary<string, Object>>? Filters { get; set; }
        public string? Roles { get; set; }
        public string? Fields { get; set; }
        public List<string>? PropertiesList { get; set; }
        public int? PageNo { get; set; }
        public int? PageSize { get; set; }
        public bool? Trace { get; set; }
    }

    public class EntityFormOutput
    {
        public string? TransId { get; set; }
        public string? EntityName { get; set; }
        public DataTable? FormMetaData { get; set; }
        public DataTable? SubEntityMetaData { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public string? KeyField { get; set; }
        public string? Error { get; set; }
        public bool? DataSource { get; set; }
        public string? Trace { get; set; }

    }

    public class AnalyticsData
    {
        public string? ARMSessionId { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string AppName { get; set; }
        public string Page { get; set; }
        public string? TransId { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public List<string>? PropertiesList { get; set; }
        public bool? All { get; set; }
        public string UserName { get; set; }
    }


    public class AnalyticsEntityInput
    {
        public string? ARMSessionId { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string AppName { get; set; }
        public string Page { get; set; }
        public string? TransId { get; set; }
        public List<string>? PropertiesList { get; set; }
        public string UserName { get; set; }
        public string Roles { get; set; }
    }

    public class AnalyticsEntityOutput
    {

        public string? TransId { get; set; }
        public string? SelectedEntities { get; set; }
        public DataTable? SelectedEntitiesList { get; set; }
        public DataTable? AllEntitiesList { get; set; }
        public DataTable? MetaData { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public string? Error { get; set; }
    }

    public class AnalyticsCharts
    {
        public string? ARMSessionId { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? Page { get; set; }
        public string? TransId { get; set; }
        public string? UserName { get; set; }
        public List<ChartMetaData>? ChartMetaData { get; set; }
        public Dictionary<string, string>? ViewFilters { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
        public string? Constraint { get; set; }
        public string? Roles { get; set; }
        public bool? Trace { get; set; }

    }

    public class ChartMetaData
    {
        public string? AggField { get; set; }
        public string? GroupField { get; set; }
        public string? AggTransId { get; set; }
        public string? GroupTransId { get; set; }
        public string? AggFunc { get; set; }
    }

    public class EntityListOutput
    {
        public string? TransId { get; set; }
        public string? EntityName { get; set; }
        public DataTable? ListData { get; set; }
        public DataTable? MetaData { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public int? PageNo { get; set; }
        public int? PageSize { get; set; }
        public string? ActiveFilter { get; set; }
        public string? Fields { get; set; }
        public string? Filter { get; set; }
        public string? Error { get; set; }
        public string? Trace { get; set; }
        public int? RecordsFetched { get; set; }
    }

    public class EntityFilter
    {
        public string? TransId { get; set; }
        public string? FieldId { get; set; }
        public string? Page { get; set; }
        public string? ARMSessionId { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public string? EntityName { get; set; }
        public string? UserName { get; set; }

        public string? Filter { get; set; }
        public string? SearchText { get; set; }


    }

    public class UserConfigData
    {
        public string PageName { get; set; }
        public string StructName { get; set; }
        public string Key { get; set; }
        public string Username { get; set; }
        public string Value { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }



    public class FieldMetadata
    {
        public string? Ftransid { get; set; }
        public string? Fcaption { get; set; }
        public string? Fldname { get; set; }
        public string? Fldcap { get; set; }
        public string? Cdatatype { get; set; }
        public string? Fdatatype { get; set; }
        public string? Fmodeofentry { get; set; }
        public string? Hide { get; set; }
        public string? Props { get; set; }
        public string? Normalized { get; set; }
        public string? Srctable { get; set; }
        public string? Srcfield { get; set; }
        public string? Srctransid { get; set; }
        public string? Allowempty { get; set; }
        public string? Filtertype { get; set; }
        public string? Grpfield { get; set; }
        public string? Aggfield { get; set; }
        public string? Subentity { get; set; }
        public string? Datacnd { get; set; }
        public string? Entityrelfld { get; set; }
        public string? Allowduplicate { get; set; }
        public string? Tablename { get; set; }
        public string? Dcname { get; set; }
        public string? Fordno { get; set; }
        public string? Dccaption { get; set; }
        public string? Griddc { get; set; }
        public string? Listingfld { get; set; }
    }
}


