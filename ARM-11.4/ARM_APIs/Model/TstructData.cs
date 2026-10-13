namespace ARM_APIs.Model
{
    public class TstructData
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public string? AxOwner { get; set; }
        public string? AxTransAttribute { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public string? DCRows { get; set; }
        public bool? Trace { get; set; }
        public Dictionary<string, object> SubmitData { get; set; } = new Dictionary<string, object>();
        public string? DataArrayJson { get; set; }
        public List<string>? Roles { get; set; } = new List<string>();

        public bool? RapidSave { get; set; } = false;
    }

    public class TstructGetData
    {
        public string ARMSessionId { get; set; }
        public string? TransId { get; set; }
        public string? ADSGroupName { get; set; }
        public string? ADSName { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public string? AxOwner { get; set; }
        public string? AxTransAttribute { get; set; }
        public bool? Trace { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? GlobalParams { get; set; } = new Dictionary<string, string>();
        public List<string>? Roles { get; set; } = new List<string>();
        public bool? DoUserAttributesCheck { get; set; } = true;
    }

    public class TstructDataGet
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public bool? Trace { get; set; }
        public string KeyField { get; set; }
        public string KeyValue { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; } = new Dictionary<string, string>();
        public List<string>? Roles { get; set; } = new List<string>();
    }

    public class ADSGetData
    {
        public string ARMSessionId { get; set; }
        public string? ADSGroupName { get; set; }
        public string? ADSName { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public string? AxOwner { get; set; }
        public string? AxTransAttribute { get; set; }
        public bool? Trace { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? GlobalParams { get; set; } = new Dictionary<string, string>();
        public List<string>? Roles { get; set; } = new List<string>();
    }

    public class TstructDataValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class AxPutData
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public string? DCRows { get; set; }
        public string? DCs { get; set; }
        public bool? Trace { get; set; }
        public Dictionary<string, object>? SubmitData { get; set; } = new Dictionary<string, object>();
        public string? DataArrayJson { get; set; }
        public List<string>? Roles { get; set; } = new List<string>();
        public bool? CachePermissions { get; set; } = true;
    }

    public class AxEditFlag
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? SchemaName { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public bool? Trace { get; set; }
        public string? Roles { get; set; }
        public bool? CachePermissions { get; set; } = true;


    }

    public class AxViewFlag
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? SchemaName { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public bool? Trace { get; set; }
        public string? Roles { get; set; }
    }

    public class AxGetData
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? ADSName { get; set; }
        public List<string>? ADSNames { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? RecordId { get; set; }
        public string Action { get; set; }
        public bool? Trace { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public List<string>? Roles { get; set; } = new List<string>();
        public string? Constraint { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? GlobalParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string>? ViewFilters { get; set; } = new Dictionary<string, string>();

        public List<Dictionary<string, Object>>? Filters { get; set; } = new List<Dictionary<string, Object>>();
        public List<Dictionary<string, string>>? Sorting { get; set; } = new List<Dictionary<string, string>>();
        public int? PageNo { get; set; }
        public int? PageSize { get; set; }
        public bool? GetAllRecordsCount { get; set; } = false;
        public bool? CachePermissions { get; set; } = true;
    }
       
}
