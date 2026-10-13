namespace AxDataObject.Models.Dto;

public class BaseRequest
{
    public string ARMSessionId { get; set; }
    public string TransId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool Trace { get; set; }
    public string AxClient_DateFormat { get; set; } = "";
    public bool MilliSecsInTimeStamp { get; set; } = false;
}

// ── SetValue ──────────────────────────────────────────────
public class SetValueRequest : BaseRequest
{
    public string FieldName { get; set; } = string.Empty;
    public int RowNo { get; set; } = 1;
    public string Value { get; set; } = string.Empty;
    public long IdValue { get; set; } = 0;
    public string RecordId { get; set; } = string.Empty;

}

public class SetValueResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }

    /// <summary>
    /// All fields that changed as a result of this set (including dependents).
    /// Key = fieldName, Value = { rowNo → new value }
    /// </summary>
    public Dictionary<string, Dictionary<int, string>> ChangedFields { get; set; } = new();
}

// ── GetValue ──────────────────────────────────────────────
public class GetValueRequest : BaseRequest
{
    public string FieldName { get; set; } = string.Empty;
    public int RowNo { get; set; } = 1; 
    public string RecordId { get; set; } = string.Empty; 

}

public class GetValueResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public string Value { get; set; }
    public long IdValue { get; set; }
}

// ── SubmitValue ───────────────────────────────────────────
/// <summary>
/// Batch-set multiple fields at once (e.g. on form load or paste).
/// Validates each field after setting.
/// </summary>
public class SubmitValueRequest : BaseRequest
{
    /// <summary>
    /// DC name → list of rows → field name → value
    /// e.g. { "dc1": [ { "fieldA": "10", "fieldB": "20" } ] }
    /// </summary>
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> SubmitData { get; set; }
    public string Action { get; set; } = "create";
    public string RecordId { get; set; }
}

public class SubmitValueResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, Dictionary<int, string>> ChangedFields { get; set; } = new();
}

public class ValidationError
{
    public string FieldName { get; set; }
    public int RowNo { get; set; }
    public string Message { get; set; }
}

// ── GetData (AxGet) ───────────────────────────────────────
public class GetDataRequest : BaseRequest
{
    public string KeyField { get; set; }
    public string KeyValue { get; set; }
    public string Dimensions { get; set; } // global vars / dimensions
    public string CalledFrom { get; set; } // "UI" | "SAVEDATA" | "API"
    public bool RefreshCache { get; set; }
    public bool CachePermissions { get; set; } = true;
    public bool AxpertWSFormat { get; set; } = false;
}

public class GetDataResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }

    /// <summary>
    /// Full form data — DC name → rows → field name → value.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    public List<ValidationError> ValidationErrors { get; set; } = new();
}

// ── SetData (AxPut) ───────────────────────────────────────
public class SetDataRequest : BaseRequest
{
    /// <summary>
    /// "create" | "edit"
    /// </summary>
    public string Action { get; set; }
    public long RecordId { get; set; }
    public bool ValidateOnly { get; set; }
    public string Dimensions { get; set; }

    /// <summary>
    /// Optional override: submit fresh data instead of reading from Redis DataObject.
    /// When null, DataObject from Redis is used (preferred flow).
    /// </summary>
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> SubmitData { get; set; }
}

    public class SetDataResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public long SavedRecordId { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ValidateSessionResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
}