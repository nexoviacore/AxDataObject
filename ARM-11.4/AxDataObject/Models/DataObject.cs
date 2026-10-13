using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxParserApp;
using AxStructDef;
using AxStructDef.Helpers;
using AxStructDef.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using System.Data;
using System.Xml;

namespace AxDataObject.Models;

/// <summary>
/// Per-user, per-transaction runtime state.
/// Stored in Redis separately from StructDef (schema).
/// Key: {schema}:ARM:{transId}:{userName}:ARM-AX-DATAOBJECT
/// </summary>
public class DataObject
{
    // The store — schema + runtime data, everything lives here
    public StructDef Sdef { get; set; }

    // Session identity (for Redis key only)
    //public string FormInstanceId { get; set; }
    //public string SchemaName { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DataObject(StructDef structDef, Utils common, IARMLogTraceService traceService)
    {
        Sdef = structDef;
        //FormInstanceId = formInstanceId;
        //SchemaName = schemaName;
        _common = common;
        _traceService = traceService;
    }

    public DataObject()
    {
    }

    // Called after loading from Redis — re-attaches non-serialized services
    public void ReAttachServices(Utils common, IARMLogTraceService traceService)
    {
        _common = common;
        _traceService = traceService;
        Sdef.TraceService = traceService;
    }

    #region Serialized Private Members

    [JsonProperty] private List<string> _oldFieldsList;
    [JsonProperty] private List<string> _autogenTransid;
    [JsonProperty] private List<string> _prefixes;
    [JsonProperty] private List<string> _prefixFields;
    [JsonProperty] private List<string> _autoGenVals;
    [JsonProperty] private string _transType;
    [JsonProperty] private bool _disposed;
    [JsonProperty] private bool _onLoad;
    //[JsonProperty] private List<StructDef> _structDefList;
    [JsonProperty] private List<string> _genFieldValueList;
    [JsonProperty] private List<Int64> _genIdValueList;
    [JsonProperty] private string _genFieldValue;
    [JsonProperty] private Int64 _genIdValue;
    [JsonProperty] private int _parentRowNo;
    [JsonProperty] private bool _rowExists;
    [JsonProperty] private string _oldFieldValue;
    [JsonProperty] private List<string> _invalidRows;
    [JsonProperty] private List<string> _fieldsAdded;
    [JsonProperty] private List<string> _rowsAdded;
    [JsonProperty] private List<string> _groupFieldValues;
    [JsonProperty] private bool _clientNotify;
    [JsonProperty] private bool _depCall;

    #endregion

    #region Non-Serialized Private Members

    private Utils _common;
    private IARMLogTraceService _traceService;
    private Dictionary<string, TField>? _fieldLookup;
    private bool _lookupInitialized;
    private AxStructDefUtils? _utils;
    private StructDef? _parentStructDef;
    private static readonly Dictionary<Type, DbType> _typeMap = new()
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(byte[]), DbType.Binary },
            { typeof(byte?), DbType.Byte },
        };
    #endregion

    #region Public Methods

    /// <param name="fieldname">Name of the field to find (case-insensitive)</param>
    /// <returns>TField object if found, null if field doesn't exist</returns>
    /// <example>GetField("CustomerName") returns CustomerName field definition</example>
    public TField? GetField(string fieldName)
    {
        InitializeFldLookup();
        _fieldLookup.TryGetValue(fieldName, out var field);
        return field;
    }

    /// <param name="tableName">Frame table name to find</param>
    /// <returns>TFrame with matching table name, null if not found</returns>
    /// <example>GetFrame("custd1") returns customer details frame</example>
    public TFrame? GetFrame(string tableName)
    {
        string tbName = tableName.ToLower();
        return Sdef.Frames?.FirstOrDefault(f => f.TableName.Equals(tbName, StringComparison.OrdinalIgnoreCase));
    }

    /// <param name="fieldName">Field name to update value for</param>
    /// <param name="rowNo">Row number (1 for non-grid fields)</param>
    /// <param name="value">New field value to set</param>
    /// <param name="oldValue">Previous field value for change tracking</param>
    /// <param name="idValue">New ID value for the field</param>
    /// <param name="oldIdValue">Previous ID value for change tracking</param>
    /// <param name="recordId">Record identifier for data mapping</param>
    /// <example>SubmitValue("Amount", 1, "100.50", "90.00", 123, 122, 1001)</example>
    public void SubmitValue(
        string fieldName,
        int rowNo,
        string value,
        string oldValue,
        Int64 idValue,
        Int64 oldIdValue,
        Int64 recordId
    ) //, string encryptValue = "")
    {
        var field = GetField(fieldName);

        if (field is null)
        {
            //Check if it's a company-specific field
            //var companyField = $",{(Sdef.Sdef.CompanyName + fieldName[..^2]).ToLower()},";

            //if (!Tables.Contains(companyField, StringComparison.CurrentCultureIgnoreCase))
            //    return;

            SetFieldValue(fieldName, "", "", "", value, "", rowNo, 0, 0, 0, 0, false);
            return;
        }

        // Use pattern matching and simplified null check
        rowNo = field is { AsGrid: false } ? 1 : rowNo;

        if (field.EncryptValue)
        {
            value = AXPEncryptData.Encrypt(value);
        }

        SetFieldValue(
            fieldName,
            field.DataType,
            field.CustType,
            field.Tablename,
            //value, oldValue, rowNo, idValue, oldIdValue,
            value,
            field.OldFieldValue,
            rowNo,
            idValue,
            field.OldIDValue, // onSubmitValue should not change the oldField value
            field.FrameNo,
            recordId,
            field.SourceKey
        ); // encryptValue);
    }

    public void SubmitValue(string fieldName, int rowNo, string value, Int64 idvalue = 0)
    {
        var field = GetField(fieldName);

        if (field is null)
        {
            SetFieldValue(fieldName, "", "", "", value, "", rowNo, idvalue, 0, 0, 0, false);
            return;
        }

        rowNo = field is { AsGrid: false } ? 1 : rowNo;

        if (field.EncryptValue)
        {
            value = AXPEncryptData.Decrypt(value);
        }

        SetFieldValue(
            fieldName,
            field.DataType,
            field.CustType,
            field.Tablename,
            value,
            field.OldFieldValue,
            rowNo,
            idvalue,
            field.OldIDValue, // onSubmitValue should not change the oldField value
            field.FrameNo,
            0,
            field.SourceKey
        ); // encryptValue);
    }

    /// <param name="FieldName">Name of the field to set value for</param>
    /// <param name="DataType">Data type of the field (n=numeric, d=date, etc.)</param>
    /// <param name="TableName">Database table name for the field</param>
    /// <param name="Value">New value to set for the field</param>
    /// <param name="OldValue">Previous value for change tracking</param>
    /// <param name="RowNo">Row number in the data container</param>
    /// <param name="IdValue">ID value associated with the field</param>
    /// <param name="OldIdValue">Previous ID value for change tracking</param>
    /// <param name="FrameNo">Frame number containing the field</param>
    /// <param name="RecordId">Unique record identifier</param>
    /// <param name="SourceKey">Whether field is a source key/normalized</param>
    /// <example>SetFieldValue("CustomerCode", "s", "Customers", "CUST001", "", 1, 1001, 0, 1, 5001, true)</example>
    public void SetFieldValue(
        string FieldName,
        string DataType,
        string CustomDataType,
        string TableName,
        string Value,
        string OldValue,
        int RowNo,
        Int64 IdValue,
        Int64 OldIdValue,
        int FrameNo,
        Int64 RecordId,
        bool SourceKey
    ) //, string encrypt_value = "")
    {
        DataType = DataType.ToLower();

        // Handle date validation
        if (DataType == "d")
        {
            if (!string.IsNullOrEmpty(Value))
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"ConvertClientDateToDBDateFormat(CustomDataType : {CustomDataType}, Value : {Value}, clientDateFormat: {Sdef.clientDateFormat})");

                try
                {
                    Value = _common.ConvertClientDateToDBDateFormat(CustomDataType, Value, Sdef.clientDateFormat);

                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat response, Value : {Value}");
                }
                catch (Exception ex)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat Exception, {JsonConvert.SerializeObject(ex)}");

                    //Value = "";
                    throw new Exception(ex.Message);
                }
            }

            if (!string.IsNullOrEmpty(OldValue))
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"ConvertClientDateToDBDateFormat(CustomDataType : {CustomDataType}, OldValue : {OldValue}, clientDateFormat: {Sdef.clientDateFormat})");

                try
                {
                    OldValue = _common.ConvertClientDateToDBDateFormat(CustomDataType, OldValue, Sdef.clientDateFormat);
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat response, Value : {OldValue}");
                }
                catch (Exception ex)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"ConvertClientDateToDBDateFormat Exception, {JsonConvert.SerializeObject(ex)}");
                    OldValue = "";
                }
            }
        }

        //if (DataType == "i")
        //    return;

        int p;
        if (RowNo == -1)
            p = -1;
        else
            p = GetIndex(FieldName, TableName, RowNo);

        FieldRec frec;
        string ov;

        if (p == -1)
        {
            frec = new FieldRec
            {
                DataType = DataType,
                FrameNo = FrameNo,
                //RecordId = RecordId,
                FieldName = FieldName,
                TableName = TableName.ToUpper(),
                RowNo = RowNo,
                SourceKey = SourceKey,
                AutoValue = false,
                //OldIdValue = OldIdValue,
                ParentRowNo = 0,
                ZeroValue = true,
                OldRow = 0,
            };

            // Set PrimaryTable
            if (TableName.ToLower() == Sdef.PrimaryTableName.ToLower())
                frec.PrimaryTable = "a";
            else
                frec.PrimaryTable = Sdef.SubFrames.Contains(FrameNo.ToString()) ? "x" : "b";

            if (_onLoad)
            {
                frec.OldValue =
                    (DataType == "n" && string.IsNullOrEmpty(OldValue)) ? "0" : OldValue;
                frec.OldIdValue = OldIdValue;
                frec.RecordId = RecordId;
            }

            // Get field reference
            TField fp = GetField(FieldName);
            frec.Fld = fp;
            frec.Orders = fp?.OrderNo ?? 0;

            Sdef.FieldList.Add(frec);
            p = Sdef.FieldList.Count - 1;

            // Handle grid insertion tracking
            if (fp != null && fp.AsGrid)
            {
                //if (Quickload)
                //{
                //    if (
                //        !string.IsNullOrEmpty(Value)
                //        || (DataType == "n" && Value != "0" && Value != "0.00")
                //    )
                //    {
                //        InsertedRowNoInfo(fp.FrameNo, RowNo);
                //    }
                //}
                //else
                {
                    InsertedRowNoInfo(fp.FrameNo, RowNo);
                }
            }
        }
        else
        {
            frec = Sdef.FieldList[p];
        }

        ov = frec.Value;

        // Set the actual value with type-specific handling
        if (DataType == "d" && IsDateEmpty(Value))
        {
            frec.Value = "";
        }
        else if (DataType == "n")
        {
            if (string.IsNullOrEmpty(Value))
            {
                frec.Value = "0";
                frec.ZeroValue = false;
            }
            else
            {
                frec.Value = RemoveCommas(Value);
                frec.ZeroValue = true;
            }
        }
        else
        {
            frec.Value = Value;
        }

        // Handle auto value tracking
        if (frec.AutoValue && ov != frec.Value)
            frec.AutoValue = false;

        // Web data handling
        //if (WebData)
        //{
        //    frec.WebValue = Value;
        //    frec.EncryptValue = encrypt_value;
        //}

        frec.ClientNotify = _clientNotify;
        frec.IdValue = IdValue;

        // Update field data rows
        if (frec.Fld != null && !frec.Fld.DataRows.Contains(p.ToString()))
            frec.Fld.DataRows.Add(p.ToString());

        if (frec.Fld != null)
        {
            SetFieldValue(frec.Fld, RowNo, Value, OldValue, IdValue, OldIdValue, RecordId);
        }

        // Currency decimal handling
        //if (!string.IsNullOrWhiteSpace(Value) && RowNo > 0 &&
        //    FieldName.ToLower() == "axpcurrencydec")
        //{
        //    SetCurrencyDec(Value, RowNo);
        //}
    }

    private string RemoveCommas(string s)
    {
        while (true)
        {
            int k = s.IndexOf(Sdef.LocThousandSeparator);
            if (k == -1)
                break;
            s = s.Remove(k, 1);
        }
        return s;
    }

    /// <param name="tempFld">TField object to update values for</param>
    /// <param name="rowNo">Row number to set value for</param>
    /// <param name="fVal">New field value to set</param>
    /// <param name="fOldVal">Previous field value for tracking</param>
    /// <param name="idValue">New ID value for the field</param>
    /// <param name="oldIdValue">Previous ID value for tracking</param>
    /// <param name="recordId">Record identifier for data mapping</param>
    /// <example>SetFieldValue(customerField, 1, "John Doe", "Jane Smith", 1001, 1000, 5001)</example>
    public void SetFieldValue(
        TField tempFld,
        int rowNo,
        string fVal,
        string fOldVal,
        Int64 idValue,
        Int64 oldIdValue,
        Int64 recordId
    )
    {
        if (tempFld.FieldName == null)
            return;

        tempFld.FieldValue = fVal;
        tempFld.IDValue = idValue;

        // It's handled through SubmitValue & OnSubmitValue
        if (_onLoad)
        {
            tempFld.OldFieldValue = fOldVal;
            tempFld.OldIDValue = oldIdValue;
            tempFld.RecId = recordId;
        }

        tempFld.ValueSubmitted = true;

        if (tempFld.AsGrid)
        {
            string rowKey = rowNo.ToString();
            int index = FindIndexInList(tempFld.FieldValList, rowKey);

            if (index < 0)
            {
                //tempFld.FieldValList.Add($"{rowKey}={fVal}");
                tempFld.FieldValList.Add(rowKey, fVal);
                //tempFld.OldFieldValList.Add($"{rowKey}={fOldVal}");
                tempFld.OldFieldValList.Add(rowKey, fVal);
                tempFld.IDValList.Add($"{rowKey}={idValue}");
                tempFld.OldIDValList.Add($"{rowKey}={oldIdValue}");
                tempFld.RecIDList.Add($"{rowKey}={recordId}");
                tempFld.OldRecIDList.Add($"{rowKey}={recordId}");
            }
            else
            {
                //tempFld.FieldValList[index] = $"{rowKey}={fVal}";
                tempFld.FieldValList[rowKey] = fVal;
                tempFld.IDValList[index] = $"{rowKey}={idValue}";

                if (_onLoad)
                {
                    //tempFld.OldFieldValList[index] = $"{rowKey}={fOldVal}";
                    tempFld.OldFieldValList[rowKey] = fOldVal;
                    tempFld.OldIDValList[index] = $"{rowKey}={oldIdValue}";
                    tempFld.RecIDList[index] = $"{rowKey}={recordId}";
                    tempFld.OldRecIDList[index] = $"{rowKey}={recordId}";
                }
            }

            if (tempFld.FieldValList.Count > Sdef.Frames[tempFld.FrameNo - 1].RowCount)
                Sdef.Frames[tempFld.FrameNo - 1].RowCount = tempFld.FieldValList.Count;
        }
        else
        {
            Sdef.Frames[tempFld.FrameNo - 1].RowCount = 1;
        }

        // Update the field in the list
        int fieldIndex = GetFieldIndex(tempFld.FieldName);

        if (fieldIndex >= 0)
            Sdef.Flds[fieldIndex] = tempFld;
        _lookupInitialized = false;
    }

    /// <param name="fieldname">Field name to find index for</param>
    /// <returns>Zero-based index of field in Flds list, -1 if not found</returns>
    /// <example>GetFieldIndex("CustomerName") returns 5</example>
    public int GetFieldIndex(string fieldName)
    {
        for (int i = 0; i < Sdef.Flds.Count; i++)
        {
            if (Sdef.Flds[i].FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public int GetFieldIndex(string fieldName, int rowNo)
    {
        for (int i = 0; i < Sdef.FieldList.Count; i++)
        {
            if (Sdef.FieldList[i].FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) && Sdef.FieldList[i].RowNo == rowNo)
            {
                return i;
            }
        }
        return -1;
    }

    /// <param name="Frameno">Frame number to search for</param>
    /// <returns>Index of last field in specified frame, -1 if no fields</returns>
    /// <example>GetLastFieldIndex(1) returns 15</example>
    public int GetLastFieldIndex(int Frameno)
    {
        return Sdef.Flds.FindLastIndex(f => f.FrameNo == Frameno);
    }

    /// <param name="Frameno">Frame number to search in</param>
    /// <returns>Name of first enabled field in frame, empty string if none</returns>
    /// <example>GetFirstEnabledFldName(1) returns "CustomerCode"</example>
    public string GetFirstEnabledFldName(int Frameno)
    {
        var field = Sdef.Flds.FirstOrDefault(f => f.FrameNo == Frameno && !f.Readonly && !f.Hidden);
        return field?.FieldName ?? "";
    }

    /// <param name="lblName">Label name to add to dynamic labels</param>
    /// <example>AddToDynamicLabel("lblCustomerName")</example>
    public void AddToDynamicLabel(string lbName)
    {
        if (!Sdef.DynamicLabels.Contains(lbName))
            Sdef.DynamicLabels += lbName + ",";
    }

    /// <param name="tempFld">TField object to get value from</param>
    /// <param name="rowNo">Row number to retrieve value for</param>
    /// <returns>Field value as string, decrypted if encrypted</returns>
    /// <example>GetFieldValue(customerField, 1) returns "John Doe"</example>
    public string GetFieldValue(TField tempFld, int rowNo)
    {
        string value = string.Empty;
        if (tempFld == null)
            return value;

        if (!tempFld.AsGrid)
        {
            if (tempFld.FieldValue != null)
            {
                //if (tempFld.EncryptValue)
                //    value = AXPEncryptData.Decrypt(tempFld.FieldValue);
                //else
                //    value = tempFld.FieldValue;

                value = tempFld.FieldValue;

                if (tempFld.CustType?.ToUpper() == "DATE")
                    return value.Split(' ')[0];
                else
                    return value;
            }
            else
                return value;
        }

        value = GetValueFromList(tempFld.FieldValList, rowNo.ToString()) ?? string.Empty;

        if (!string.IsNullOrEmpty(value))
        {
            //if (tempFld.EncryptValue)
            //    value = AXPEncryptData.Decrypt(tempFld.FieldValue);
            //else
            //    value = tempFld.FieldValue;

            //value = tempFld.FieldValue;

            if (tempFld.CustType?.ToUpper() == "DATE")
                return value.Split(' ')[0];
            else
                return value;
        }
        return value;
    }

    /// <param name="fieldName">Field name to get value for</param>
    /// <param name="rowNo">Row number to retrieve value for</param>
    /// <returns>Field value as string, empty if field not found</returns>
    /// <example>GetFieldValue("CustomerName", 1) returns "John Doe"</example>
    public string GetFieldValue(string fieldName, int rowNo)
    {
        var tempFld = GetField(fieldName);
        return GetFieldValue(tempFld, rowNo);
    }

    /// <param name="fieldName">Field name to get value for</param>
    /// <param name="rowNo">Row number to retrieve value for</param>
    /// <returns>Field value as string, empty if field not found</returns>
    /// <example>GetFieldValue("CustomerName", 1) returns "John Doe"</example>
    public string GetFieldDataType(string fieldName)
    {
        var tempFld = GetField(fieldName);
        return tempFld.DataType;
    }

    public DbType GetFieldDBType(string fieldName)
    {
        var tempFld = GetField(fieldName);
        return GetFieldDBType(tempFld);
    }

    public DbType GetFieldDBType(TField tempFld)
    {
        var dataType = DbType.String;

        if (tempFld.SourceKey)
            return DbType.Int64;

        switch (tempFld.DataType.ToUpper())
        {
            case "D":
                dataType = DbType.Date;
                break;
            case "N":
                if (tempFld.Dec > 0)
                    dataType = DbType.Decimal;
                else
                    dataType = DbType.Int64;
                break;
            case "T":
            case "C":
                if (tempFld.CustType.ToUpper() == "JSON")
                    dataType = DbType.Object;
                else if (tempFld.CustType.ToUpper() == "GUID")
                    dataType = DbType.Guid;
                else
                    dataType = DbType.String;
                break;
            default:
                dataType = DbType.String;
                break;
        }

        return dataType;
    }

    /// <param name="tempOldFldName">Field name to get old value from</param>
    /// <param name="rowNo">Row number to retrieve old value for</param>
    /// <returns>Previous field value as string, decrypted if encrypted</returns>
    /// <example>GetOldFieldValue(customerFieldname, 1) returns "Jane Smith"</example>
    public string GetOldFieldValue(string tempOldFldName, int rowNo)
    {
        TField tempOldFld = GetField(tempOldFldName);
        return GetOldFieldValue(tempOldFld, rowNo);
    }

    /// <param name="tempOldFld">TField object to get old value from</param>
    /// <param name="rowNo">Row number to retrieve old value for</param>
    /// <returns>Previous field value as string, decrypted if encrypted</returns>
    /// <example>GetOldFieldValue(customerField, 1) returns "Jane Smith"</example>
    public string GetOldFieldValue(TField tempOldFld, int rowNo)
    {
        if (tempOldFld == null)
            return string.Empty;

        return !tempOldFld.AsGrid
            ? tempOldFld.OldFieldValue ?? string.Empty
            : GetValueFromList(tempOldFld.OldFieldValList, rowNo.ToString()) ?? string.Empty;
    }

    /// <param name="tempFld">TField object to get ID value from</param>
    /// <param name="rowNo">Row number to retrieve ID for</param>
    /// <returns>ID value as Int64, 0 if not found</returns>
    /// <example>GetIDValue(customerField, 1) returns 1001.0</example>
    public Int64 GetIDValue(TField tempFld, int rowNo)
    {
        if (tempFld.FieldName == null)
            return 0;

        if (!tempFld.AsGrid)
            return tempFld.IDValue;
        else
        {
            string value = GetValueFromList(tempFld.IDValList, rowNo.ToString());
            return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
        }
    }

    /// <param name="fieldName">Field name to get ID value for</param>
    /// <param name="rowNo">Row number to retrieve ID for</param>
    /// <returns>ID value as Int64, 0 if field not found</returns>
    /// <example>GetIDValue("CustomerCode", 1) returns 1001.0</example>
    public Int64 GetIDValue(string fieldName, int rowNo)
    {
        var Field = GetField(fieldName);

        if (Field == null)
            return 0;
        return GetIDValue(Field, rowNo);
    }

    /// <param name="tempFld">TField object to get ID value from</param>
    /// <param name="rowNo">Row number to retrieve ID for</param>
    /// <returns>ID value as Int64, 0 if not found</returns>
    /// <example>GetIDValue(customerField, 1) returns 1001.0</example>
    public Int64 GetOldIDValue(TField tempFld, int rowNo)
    {
        if (tempFld.FieldName == null)
            return 0;

        if (!tempFld.AsGrid)
            return tempFld.OldIDValue;
        else
        {
            string value = GetValueFromList(tempFld.OldIDValList, rowNo.ToString());
            return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
        }
    }

    /// <param name="fieldName">Field name to get ID value for</param>
    /// <param name="rowNo">Row number to retrieve ID for</param>
    /// <returns>ID value as Int64, 0 if field not found</returns>
    /// <example>GetIDValue("CustomerCode", 1) returns 1001.0</example>
    public Int64 GetOldIDValue(string fieldName, int rowNo)
    {
        var Field = GetField(fieldName);

        if (Field == null)
            return 0;
        return GetOldIDValue(Field, rowNo);
    }

    /// <param name="tempFld">TField object to get record ID from</param>
    /// <param name="rowNo">Row number to retrieve record ID for</param>
    /// <returns>Record ID as Int64, 0 if not found</returns>
    /// <example>GetRecordId(customerField, 1) returns 5001.0</example>
    public Int64 GetRecordId(TField tempFld, int rowNo)
    {
        if (tempFld.FieldName == null)
            return 0;

        if (!tempFld.AsGrid)
            return tempFld.RecId;
        else
        {
            string value = GetValueFromList(tempFld.RecIDList, rowNo.ToString());
            return string.IsNullOrEmpty(value) ? 0 : Int64.Parse(value);
        }
    }

    /// <param name="frameno">Frame number where row was inserted</param>
    /// <param name="RowNo">Row number that was inserted</param>
    /// <example>InsertedRowNoInfo(1, 5) tracks row 5 insertion in frame 1</example>
    public void InsertedRowNoInfo(int frameno, int RowNo)
    {
        if (RowNo == -1) // || !_axProvider._utils.IsService)
            return;
        if (_depCall)
            return;

        string key = "DC" + frameno;
        int ind = Sdef.Ins_del_rows.FindIndex(s => s.StartsWith(key + "="));

        if (ind >= 0)
        {
            string currentValue = Sdef.Ins_del_rows[ind]
                .Substring(Sdef.Ins_del_rows[ind].IndexOf('=') + 1);
            string searchPattern = ",i" + RowNo + ",";

            if (!currentValue.Contains(searchPattern))
            {
                Sdef.Ins_del_rows[ind] = key + "=" + currentValue + "i" + RowNo + ",";
            }
        }
        else
        {
            Sdef.Ins_del_rows.Add(key + "=i" + RowNo + ",");
        }
    }

    /// <param name="FieldName">Field name to find index for</param>
    /// <param name="TableName">Table name for the field</param>
    /// <param name="RowNo">Row number in the data container</param>
    /// <returns>Index in FieldList, -1 if not found</returns>
    /// <example>GetIndex("CustomerName", "Customers", 1) returns 10</example>
    public int GetIndex(string FieldName, string TableName, int RowNo)
    {
        for (int i = 0; i < Sdef.FieldList.Count; i++)
        {
            FieldRec field = Sdef.FieldList[i];
            if (
                field.FieldName.Equals(FieldName, StringComparison.OrdinalIgnoreCase)
                && field.RowNo == RowNo
                && field.TableName.Equals(TableName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return i;
            }
        }
        return -1;
    }

    /// <param name="d">Date string to check for emptiness</param>
    /// <returns>True if date is empty/invalid, false otherwise</returns>
    /// <example>IsDateEmpty("") returns true, IsDateEmpty("2023-12-31") returns false</example>
    public bool IsDateEmpty(string d)
    {
        if (string.IsNullOrEmpty(d))
            return true;

        string dateSeparator = StructDef.ShortDateFormat.DateSeparator;

        // Replace all date separators with a space
        string modified = d.Replace(dateSeparator, " ");

        // Check if the trimmed result is empty
        return string.IsNullOrWhiteSpace(modified);
    }

    public async Task<bool> UpdateLastNo(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return false;

        fieldName = fieldName.ToLower();

        int i = Sdef.SdAutogenFields.IndexOf(fieldName);
        if (i == -1)
            return false;

        string transId = _autogenTransid[i];
        string seqTbl = (GetFieldValue("Axp_SeqTable", 1)?.Trim());
        if (string.IsNullOrEmpty(seqTbl))
            seqTbl = "Sequence";

        string wstr;
        string prefix = string.Empty;

        if (!string.IsNullOrEmpty(_prefixFields[i]))
        {
            wstr =
                $"lower(TRANSTYPE) = {QuotedStr(transId.ToLower())} "
                + $"and lower(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                + $"and lower(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";
        }
        else
        {
            wstr =
                $"lower(TRANSTYPE) = {QuotedStr(transId.ToLower())} "
                + $"and lower(FIELDNAME) = {QuotedStr(fieldName.ToLower())} ";

            prefix = Sdef.AutoGen_Prefix;

            if (string.IsNullOrEmpty(prefix))
            {
                wstr += " and ACTIVESEQUENCE = 'T'";
            }
            else
            {
                wstr += $" and lower(prefix) = {QuotedStr(prefix.ToLower())}";
            }
        }

        string lastNo = Sdef.AutoGen_Lastno;

        // Convert to int safely
        if (!int.TryParse(lastNo, out int lastNoInt))
            lastNoInt = 0;

        string sql = $"UPDATE {seqTbl} SET lastno = {lastNoInt + 1} WHERE {wstr}";

        // Execute the SQL
        try
        {
            var sqlCommands = new List<SQLCommandBuilder>();
            var cmdObj = new SQLCommandBuilder(sql, null, null, null, Constants.DATA_ACTION.UPDATE);
            sqlCommands.Add(cmdObj);

            var updateResult = await _common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands, false);
            if (updateResult.success)
                return true;
            else
                return false;
        }
        catch (Exception ex)
        {
            // Log or handle exception as needed
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error updating last number: {ex.Message}");
            return false;
        }
    }

    /// <param name="fieldName">Field name to generate last number for</param>
    /// <param name="onlyGet">True to only retrieve without incrementing</param>
    /// <returns>Generated last number as string</returns>
    /// <example>GetLastNo("InvoiceNo") returns "INV-10025"</example>
    public async Task<string> GetLastNo(string fieldName, bool onlyGet = false)
    {
        string result = "";
        string sqlWoUpd = "";

        SetAutoGenList();

        string compName = !string.IsNullOrEmpty(Sdef.CompanyName)
            ? Sdef.CompanyName.Substring(0, Sdef.CompanyName.Length - 1)
            : "";

        fieldName = fieldName.ToLower();
        int i = Sdef.SdAutogenFields.IndexOf(fieldName);

        if (i == -1)
            return result;

        string transid = _autogenTransid[i];
        string seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();

        if (string.IsNullOrEmpty(seqtbl))
            seqtbl = "Sequence";

        string sql = "";
        string wstr = "";
        bool bActive = false;
        string sqlLower = GetSqlLower();

        if (!string.IsNullOrEmpty(_prefixFields[i]))
        {
            sql =
                $"SELECT * FROM {compName.ToUpper()}{seqtbl} "
                + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                + $"AND {sqlLower}(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";

            wstr =
                $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())} "
                + $"AND {sqlLower}(prefixfield) = {QuotedStr(_prefixFields[i].ToLower())}";
        }
        else
        {
            string prefix = GetUserActivePrefix(transid, fieldName);
            string DBType = _common._dbType;
            Sdef.AutoGen_Prefix = prefix;

            if (!onlyGet && DBType?.Replace(" ", "") == "mssql")
            {
                string tmpupdstr = " ";
                //if (!PostAutoGen)  // You'll need to implement PostAutoGen property
                //    tmpupdstr += GetForUpdate();

                sql =
                    $"SELECT * FROM {compName.ToUpper()}{seqtbl}{tmpupdstr} "
                    + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";
            }
            else
            {
                sql =
                    $"SELECT * FROM {compName.ToUpper()}{seqtbl} "
                    + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                    + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";
            }

            wstr =
                $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldName.ToLower())}";

            if (string.IsNullOrEmpty(prefix))
            {
                sql += " AND ACTIVESEQUENCE = 'T'";
                wstr += " AND ACTIVESEQUENCE = 'T'";
                bActive = true;
            }
            else
            {
                sql += $" AND {sqlLower}(prefix) = {QuotedStr(prefix.ToLower())}";
                wstr += $" AND {sqlLower}(prefix) = {QuotedStr(prefix.ToLower())}";
                bActive = false;
            }
        }

        try
        {
            // Execute the query using your DBHelper
            var queryResult = ExecuteSQLAsync(sql).Result;

            if (queryResult.data.Rows.Count > 0)
            {
                DataRow row = queryResult.data.Rows[0];
                string lastno = GetRowValue(row, "lastno");
                string prefixfield = GetRowValue(row, "prefixfield")?.Trim() ?? "";
                string prefix = GetRowValue(row, "prefix")?.Trim() ?? "";
                int digits = Convert.ToInt32(GetRowValue(row, "noofdigits"));

                string dstr = new string('0', digits);

                Sdef.AutoGen_Prefix = prefix;
                Sdef.AutoGen_Lastno = lastno;

                if (!string.IsNullOrEmpty(prefixfield))
                {
                    string sval = GetFieldValue(prefixfield, 1);
                    if (sval.StartsWith(":"))
                    {
                        // result = GetPrefixFieldValue(fieldName, sval, transid, onlyGet, digits);
                        return result;
                    }
                    else
                    {
                        result = sval + dstr.Substring(0, digits - lastno.Length) + lastno;
                    }
                }
                else
                {
                    result = prefix + dstr.Substring(0, digits - lastno.Length) + lastno;
                }

                if (!onlyGet)
                {
                    await UpdateLastNo(fieldName);
                }
            }
        }
        catch (Exception ex)
        {
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error in GetLastNo: {ex.Message}");
        }

        return result;
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <param name="rowNo">Row number to set record ID for</param>
    /// <param name="recordId">Record ID value to set</param>
    /// <returns>True if successful, false if invalid DC number</returns>
    /// <example>SubmitRecordId(1, 5, 1000023209304) sets record ID for DC1 row5</example>
    public bool SubmitRecordId(int dcNo, int rowNo, Int64 recordId)
    {
        if (dcNo >= 1 && dcNo <= Sdef.Frames.Count && Sdef.Frames[dcNo - 1] != null)
        {
            TFrame frame = Sdef.Frames[dcNo - 1];

            // Add or update the key-value pair
            if (frame.RecordIdDict.ContainsKey(rowNo))
            {
                frame.RecordIdDict[rowNo] = recordId;
            }
            else
            {
                frame.RecordIdDict.Add(rowNo, recordId);
            }

            return true;
        }
        else
            return false;
    }

    /// <param name="frame">TFrame object to get record ID from</param>
    /// <param name="rowNo">Row number to retrieve record ID for</param>
    /// <returns>Record ID as Int64, 0 if not found</returns>
    /// <example>GetRecordId(customerFrame, 5) returns 1000023209304</example>
    public Int64 GetRecordId(TFrame frame, int rowNo)
    {
        if (frame?.RecordIdDict == null)
            return 0;

        if (frame.RecordIdDict.TryGetValue(rowNo, out Int64 recordId))
        {
            return recordId;
        }
        return 0;
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <param name="rowNo">Row number to retrieve record ID for</param>
    /// <returns>Record ID as Int64, 0 if invalid DC or row not found</returns>
    /// <example>GetRecordId(1, 5) returns 1000023209304</example>
    public Int64 GetRecordId(int dcNo, int rowNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return 0;

        return GetRecordId(Sdef.Frames[dcNo - 1], rowNo);
    }

    /// <param name="fieldName">Field name to set value for during loading</param>
    /// <param name="rowNo">Row number to set value for</param>
    /// <param name="value">Value to set for the field</param>
    /// <returns>True if successful, false if field or frame not found</returns>
    /// <example>SubmitValueOnLoad("CustomerName", 1, "John Doe") loads initial value</example>
    public bool SubmitValueOnLoad(string fieldName, int rowNo, string value, Int64 idValue = 0) //, string encryptValue = "")
    {
        TField fld = GetField(fieldName);

        if (fld == null)
            return false;

        TFrame frame = Sdef.Frames[fld.FrameNo - 1];
        if (frame == null)
            return false;

        // Get recordId for the row using dictionary
        Int64? recordId = GetRecordId(frame, rowNo);

        Int64 fldIdValue = idValue;
        Int64 fldOldIdValue = idValue;

        if (fld.EncryptValue)
        {
            value = AXPEncryptData.Decrypt(value);
        }

        try
        {
            _onLoad = true;
            //if (recordId.HasValue)
            {
                SetFieldValue(
                    FieldName: fieldName,
                    DataType: fld.DataType,
                    CustomDataType: fld.CustType,
                    TableName: fld.Tablename,
                    Value: value, // This need to be handled for Normalized Field
                    OldValue: value,
                    RowNo: rowNo,
                    IdValue: fldIdValue, // Value should be assigned (Normalized Field)
                    OldIdValue: fldOldIdValue, // Value should be assigned (Normalized Field)
                    FrameNo: fld.FrameNo,
                    RecordId: recordId.Value,
                    SourceKey: fld.SourceKey
                //encrypt_value: encryptValue
                );

                return true;
            }
        }
        catch (Exception ex)
        {
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error in SetFieldValue: {ex.Message}");
            throw new Exception("Error in SetFieldValue: " + ex.Message);
        }
        finally
        {
            _onLoad = false;
        }

        //return false;
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <returns>Comma-separated list of field names in the DC</returns>
    /// <example>GetDCFields(1) returns "CustomerCode,CustomerName,Address"</example>
    public string GetDCFields(int dcNo)
    {
        return string.Join(",", GetDCFieldsAsList(dcNo));
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <returns>list of field names in the DC</returns>
    /// <example>GetDCFields(1) returns "CustomerCode,CustomerName,Address"</example>
    public List<string> GetDCFieldsAsList(int dcNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return [];

        TFrame frame = Sdef.Frames[dcNo - 1];
        List<string> fieldNames = new List<string>();

        int startIndex = frame.StartIndex;
        int fieldCount = frame.FieldCount;

        for (int i = startIndex; i < startIndex + fieldCount; i++) // 1. startIndex ? 2. Sdef.Flds[ i - 1 ] needed?
        {
            if (Sdef.Flds[i] != null)
            {
                fieldNames.Add(Sdef.Flds[i].FieldName);
            }
        }

        return fieldNames;
    }

    public List<TField> GetDCFieldObjsAsList(int dcNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return [];

        TFrame frame = Sdef.Frames[dcNo - 1];
        List<TField> fields = new List<TField>();

        int startIndex = frame.StartIndex;
        int fieldCount = frame.FieldCount;

        for (int i = startIndex; i < startIndex + fieldCount; i++) // 1. startIndex ? 2. Sdef.Flds[ i - 1 ] needed?
        {
            if (Sdef.Flds[i] != null)
            {
                fields.Add(Sdef.Flds[i]);
            }
        }

        return fields;
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <returns>Table name for the specified DC, empty string if invalid</returns>
    /// <example>GetDCTable(1) returns "Customers"</example>
    public string GetDCTable(int dcNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return string.Empty;
        else
            return Sdef.Frames[dcNo - 1].TableName ?? string.Empty;
    }

    /// <param name="dcNo">Data container number (1-based index)</param>
    /// <returns>Number of rows in the DC, 0 if invalid DC</returns>
    /// <example>GetRowCount(1) returns 25</example>
    public int GetRowCount(int dcNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return 0;
        else
            return Sdef.Frames[dcNo - 1].RowCount >= 0 ? Sdef.Frames[dcNo - 1].RowCount : 0;
    }

    /// <returns>Comma-separated list of grid DC numbers</returns>
    /// <example>GetGridDcs() returns "1,3,5"</example>
    public string GetGridDcs()
    {
        return string.Join(",", GetGridDcsAsList());
    }

    /// <returns>list of grid DC numbers</returns>
    /// <example>GetGridDcs() returns [1,3,5]</example>
    public List<int> GetGridDcsAsList()
    {
        var GridDcs = Sdef.Frames
            .Where(frame => frame.AsGrid)
            .Select(frame => frame.FrameNo)
            .ToList();

        return GridDcs;
    }

    /// <returns>Comma-separated list of non-grid table names</returns>
    /// <example>GetNGTableNames() returns "Customers,Products"</example>
    public string GetNGTableNames()
    {
        return string.Join(",", GetNGTableNamesAsList());
    }

    /// <returns>list of non-grid table names</returns>
    /// <example>GetNGTableNames() returns ["Customers","Products",..]</example>
    public List<string> GetNGTableNamesAsList()
    {
        var NonGridDcs = Sdef.Frames
            .Where(frame => !frame.AsGrid)
            .Select(frame => frame.TableName)
            .ToList();

        return NonGridDcs;
    }

    public bool SetNonGridRowAction(string tableName, string rowAction)
    {
        // Each frame will have its own RowActionDict.For non-grid cases,
        // we are handling it with both the table name and the row number for easier usage
        TFrame frame = GetFrame(tableName);

        if (frame == null)
            return false;

        if (frame.RowActionDict.ContainsKey(tableName))
        {
            frame.RowActionDict[tableName] = rowAction;
            frame.RowActionDict["1"] = rowAction;
        }
        else
        {
            frame.RowActionDict.Add(tableName, rowAction);
            frame.RowActionDict.Add("1", rowAction);
        }

        return true;
    }

    public bool SetGridRowAction(int dcNo, int rowNo, string rowAction)
    {
        if (dcNo >= 1 && dcNo <= Sdef.Frames.Count && Sdef.Frames[dcNo - 1] != null)
        {
            TFrame frame = Sdef.Frames[dcNo - 1];
            string RowNo = rowNo.ToString();

            if (frame.RowActionDict.ContainsKey(RowNo))
            {
                frame.RowActionDict[RowNo] = rowAction;
            }
            else
            {
                frame.RowActionDict.Add(RowNo, rowAction);
            }

            return true;
        }
        else
            return false;
    }

    public string GetRowAction(int dcNo, int rowNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return string.Empty;

        TFrame frame = Sdef.Frames[dcNo - 1];
        if (frame.RowActionDict.Count > 0 && frame.RowActionDict.ContainsKey(rowNo.ToString()))
            return frame.RowActionDict[rowNo.ToString()];
        else
            return string.Empty;
    }

    public string GetGridRowChanges(int dcNo, int rowNo)
    {
        return string.Join(",", GetGridRowChangesAsList(dcNo, rowNo));
    }

    public List<string> GetGridRowChangesAsList(int dcNo, int rowNo)
    {
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return [];

        TFrame frame = Sdef.Frames[dcNo - 1];

        if (frame.RowActionDict[rowNo.ToString()] == null)
            return [];

        int startIndex = frame.StartIndex;
        int fieldCount = frame.FieldCount;
        List<string> fieldChanges = new List<string>();

        for (int i = startIndex; i < startIndex + fieldCount; i++)
        {
            //if (Sdef.Flds[i] != null)
            {
                //if (Sdef.Flds[i].FrameNo == dcNo)
                {
                    var fldValue = string.Empty;
                    var fldOldValue = string.Empty;
                    if (Sdef.Flds[i].SourceKey)
                    {
                        fldValue = GetIDValue(Sdef.Flds[i], rowNo).ToString();
                        fldOldValue = GetOldIDValue(Sdef.Flds[i], rowNo).ToString();
                    }
                    else
                    {
                        fldValue = GetFieldValue(Sdef.Flds[i], rowNo);
                        fldOldValue = GetOldFieldValue(Sdef.Flds[i], rowNo);
                    }

                    if (fldValue != fldOldValue)
                    {
                        fieldChanges.Add(Sdef.Flds[i].FieldName + "=" + fldValue);
                    }
                }
            }
        }
        return fieldChanges;
    }

    public List<TField> GetGridRowChangedFieldsAsList(int dcNo, int rowNo)
    {
        List<TField> changedFields = new List<TField>();
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return changedFields;

        TFrame frame = Sdef.Frames[dcNo - 1];

        if (frame.RowActionDict[rowNo.ToString()] == null)
            return changedFields;

        int startIndex = frame.StartIndex;
        int fieldCount = frame.FieldCount;

        for (int i = startIndex; i < startIndex + fieldCount; i++)
        {
            var fldValue = string.Empty;
            var fldOldValue = string.Empty;
            if (Sdef.Flds[i].SourceKey)
            {
                fldValue = GetIDValue(Sdef.Flds[i], rowNo).ToString();
                fldOldValue = GetOldIDValue(Sdef.Flds[i], rowNo).ToString();
            }
            else
            {
                fldValue = GetFieldValue(Sdef.Flds[i], rowNo);
                fldOldValue = GetOldFieldValue(Sdef.Flds[i], rowNo);
            }

            if (fldValue != fldOldValue)
            {
                changedFields.Add(Sdef.Flds[i]);
            }
        }
        return changedFields;
    }

    public string GetNGChanges(string tableName)
    {
        return string.Join(",", GetNGChangesAsList(tableName));
    }

    public List<string> GetNGChangesAsList(string tableName)
    {
        int startIndex = 0;
        int fieldCount = Sdef.Flds.Count;
        List<string> fieldChanges = new List<string>();

        for (int i = startIndex; i < fieldCount; i++)
        {
            if (!Sdef.Flds[i].AsGrid)
            {
                if (Sdef.Flds[i].Tablename == tableName)
                {
                    var fldValue = string.Empty;
                    var fldOldValue = string.Empty;
                    if (Sdef.Flds[i].SourceKey)
                    {
                        fldValue = GetIDValue(Sdef.Flds[i], 1).ToString();
                        fldOldValue = GetOldIDValue(Sdef.Flds[i], 1).ToString();
                    }
                    else
                    {
                        fldValue = GetFieldValue(Sdef.Flds[i], 1);
                        fldOldValue = GetOldFieldValue(Sdef.Flds[i], 1);
                    }

                    if (fldValue != fldOldValue)
                    {
                        fieldChanges.Add(Sdef.Flds[i].FieldName + "=" + fldValue);
                    }
                }
            }
        }
        return fieldChanges;
    }

    public List<TField> GetNGChangedFieldsAsList(string tableName, string mode = "")
    {
        int startIndex = 0;
        int fieldCount = Sdef.Flds.Count;
        List<TField> changedFields = new List<TField>();

        for (int i = startIndex; i < fieldCount; i++)
        {
            if (!Sdef.Flds[i].AsGrid)
            {
                if (Sdef.Flds[i].Tablename == tableName)
                {
                    var fldValue = string.Empty;
                    var fldOldValue = string.Empty;
                    if (Sdef.Flds[i].SourceKey)
                    {
                        fldValue = GetIDValue(Sdef.Flds[i], 1).ToString();
                        fldOldValue = GetOldIDValue(Sdef.Flds[i], 1).ToString();

                        if (mode?.ToLower() == "create")
                            fldOldValue = "";
                    }
                    else
                    {
                        fldValue = GetFieldValue(Sdef.Flds[i], 1);
                        fldOldValue = GetOldFieldValue(Sdef.Flds[i], 1);
                    }

                    if (fldValue != fldOldValue)
                    {
                        changedFields.Add(Sdef.Flds[i]);
                    }
                }
            }
        }
        return changedFields;
    }

    public List<TField> GetRowFieldsAsList(int dcNo, int rowNo)
    {
        List<TField> fields = new List<TField>();
        if (dcNo < 1 || dcNo > Sdef.Frames.Count || Sdef.Frames[dcNo - 1] == null)
            return fields;

        TFrame frame = Sdef.Frames[dcNo - 1];

        int startIndex = frame.StartIndex;
        int fieldCount = frame.FieldCount;

        for (int i = startIndex; i < startIndex + fieldCount; i++)
        {
            var fldValue = string.Empty;
            var fldOldValue = string.Empty;
            if (Sdef.Flds[i].SourceKey)
            {
                fldValue = GetIDValue(Sdef.Flds[i], rowNo).ToString();
                fldOldValue = GetOldIDValue(Sdef.Flds[i], rowNo).ToString();
            }
            else
            {
                fldValue = GetFieldValue(Sdef.Flds[i], rowNo);
                fldOldValue = GetOldFieldValue(Sdef.Flds[i], rowNo);
            }

            fields.Add(Sdef.Flds[i]);
        }
        return fields;
    }

    public string GetChangedFields(string tableName)
    {
        return string.Join(",", GetChangedFieldsAsList(tableName));
    }

    public List<string> GetChangedFieldsAsList(string tableName)
    {
        int startIndex = 0;
        int fieldCount = Sdef.Flds.Count;
        List<string> fieldChanges = new List<string>();

        for (int i = startIndex; i < fieldCount; i++)
        {
            if (!Sdef.Flds[i].AsGrid)
            {
                if (Sdef.Flds[i].Tablename == tableName)
                {
                    var fldValue = GetFieldValue(Sdef.Flds[i], 1);
                    var fldOldValue = GetOldFieldValue(Sdef.Flds[i], 1);

                    if (fldValue != fldOldValue)
                    {
                        fieldChanges.Add(Sdef.Flds[i].FieldName);
                    }
                }
            }
        }
        return fieldChanges;
    }

    public void GetParentValue(
    int frmno,
    int rowno,
    string pFields,
    List<KeyValuePair<string, string>> sList)
    {
        sList.Clear();

        int i = 1;

        // pField := Trim(GetNthString(pFields, i))
        string pField = GetNthString(pFields, i)?.Trim() ?? "";

        while (!string.IsNullOrEmpty(pField))
        {
            string fname = "sub" + frmno.ToString() + "_" + pField;

            // tfld := sd.structdef.GetField(pField)
            var tfld = GetField(pField);

            if (tfld != null)
                pField = tfld.FieldName; // actual field name from structdef

            string val = GetFieldValue(fname, rowno);

            // sList.Add(pField + '=' + val)
            sList.Add(new KeyValuePair<string, string>(pField, val));

            i++;
            pField = GetNthString(pFields, i)?.Trim() ?? "";
        }
    }

    public int GetParentActiveRow(int rowNo, int popIndex)
    {
        int result = -1;

        // popgrid := pPopgrid(sd.structdef.Sdef.PopGrids[popindex]);
        var popgrid = Sdef.PopGrids[popIndex];

        string s = popgrid.Parent;

        // pfrmno := StrToInt(copy(s,3,3));
        int pfrmno = int.Parse(s.Substring(2, 3));

        // ParentList := TStringList.create;
        var parentList = new List<KeyValuePair<string, string>>();

        // GetParentValue(SD,popgrid.FrameNo,RowNo,popgrid.ParentField,ParentList);
        GetParentValue(popgrid.FrameNo, rowNo, popgrid.ParentField, parentList);

        int rc = GetRowCount(pfrmno);

        for (int i = 1; i <= rc; i++)
        {
            bool validRow = false;

            for (int j = 0; j < parentList.Count; j++)
            {
                string fieldName = parentList[j].Key;
                string parentValue = parentList[j].Value;

                var tfld = GetField(fieldName);

                string currentValue = GetFieldValue(fieldName, i);

                if (tfld.DataType == "n")    // numeric comparison
                {
                    if (string.IsNullOrEmpty(currentValue) ||
                        double.Parse(currentValue) != double.Parse(parentValue))
                    {
                        validRow = false;
                        break;
                    }
                    else
                    {
                        validRow = true;
                    }
                }
                else                        // string comparison
                {
                    if (currentValue != parentValue)
                    {
                        validRow = false;
                        break;
                    }
                    else
                    {
                        validRow = true;
                    }
                }
            }

            if (validRow)
            {
                result = i;
                break;
            }
        }

        return result;
    }

    public bool IsFieldExists(string fieldName, int rowNo)
    {
        // Default result is false
        bool result = false;

        // If this flag is set, Delphi exits immediately
        //if (workOnQuery)
        //    return result;

        //StructDef sdef = _parentStructDef;

        // Choose correct StoreData based on SourceTransId
        //if (structDefRec.SourceTransId == Transid)
        //{
        //    if (ParentStructDef != null)
        //           sd = ParentStructDef;
        //}
        //else
        //{
        //    //int k = structDefRec.SourceIndex;
        //    int k = structDefRec.SourceIndex;
        //    if (k == -1)
        //        return result;

        //    sd = StoreDataList[k]).StoreData;
        //}

        int kf = GetFieldIndex(fieldName, rowNo);

        return (kf >= 0);
    }

    public bool ValidRow(int rowNo, GenMapField genMapField)
    {
        bool result = true;

        //string rowControlField = map?.Attributes?["control"]?.Value ?? "";
        string rowControlField = genMapField.Control;
        if (string.IsNullOrEmpty(rowControlField))
            return result;

        Int64 fid = _genIdValue;

        if (!IsFieldExists(rowControlField, rowNo))
            rowNo = 1;

        string fv = GetFieldValueFromParent(rowControlField, rowNo)?.ToLower() ?? "";

        result = (fv == "t");

        //idValue = fid;

        _rowExists = (_oldFieldValue?.ToLowerInvariant() == "t") && (fv != "t");

        return result;
    }

    public void FillSpecificRow(GenMapField genMapField)
    {
        //StructDef sdef = _parentStructDef;
        //TField fld = _parentStructDef.GetField(genMapField.TargetField);
        TField fld = GetField(genMapField.TargetField);

        if (fld == null) return;
        string fieldName = fld.FieldName;
        string dataType = fld.DataType;
        string saveTableName = fld.Tablename;
        int frameNo = fld.FrameNo;
        bool sourceKey = fld.SourceKey;
        var customDataType = fld.CustType;


        bool validity;
        string s, fv;
        int i;
        int x = 0;

        // --- Field value lookup ---


        //if (map.Attributes["type" == "field" && Convert.ToInt32(map.Attributes["srow"]) == 0)
        if (genMapField.Type == "field" && Convert.ToInt32(genMapField.SourceRow) == 0)
        {

            GetGenFieldValue(fieldName, _parentRowNo, genMapField);
            validity = ValidRow(_parentRowNo, genMapField);
        }
        else
        {
            //int srow = Convert.ToInt32(map.Attributes["srow"]);
            int srow = genMapField.SourceRow;
            GetGenFieldValue(fieldName, srow, genMapField);

            if (srow != 0)
                validity = ValidRow(srow, genMapField);
            else
                validity = ValidRow(_parentRowNo, genMapField);
        }


        // --- Numeric auto ID handling ---
        if (dataType == "n" && _genIdValue > 0)
        {
            _genFieldValue = _genIdValue.ToString();
            _genIdValue = 0;
        }
        else if (dataType == "d")
        {
            _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, Sdef.clientDateFormat);
        }

        if (!fld.SourceKey)
        {
            _genIdValue = 0;
        }
        else if (genMapField.IsID)
        {
            _genIdValue = Int64.Parse(_genFieldValue);

            _genFieldValue = string.Empty;
        }

        // --- Compute TRow (x) while skipping invalid rows ---
        //if (map != null || map.Attributes != null)
        //x = Convert.ToInt32(map?.Attributes?["trow"]?.Value);
        x = Convert.ToInt32(genMapField.TargetRow);
        //for (i = 1; i <= Convert.ToInt32(map?.Attributes?["trow"]?.Value) - 1; i++)
        for (i = 1; i <= x - 1; i++)
        {
            //s = structObj.axprovider.dbm.gf.Pad(frameNo.ToString(), 2, ' ')
            //    + structObj.axprovider.dbm.gf.Pad(i.ToString(), 2, ' ');
            s = _utils.Pad(frameNo.ToString(), 2, ' ')
                + _utils.Pad(i.ToString(), 2, ' ');



            if (_invalidRows.Contains(s))
                x--;
        }

        // --- Find or add field to FieldsAdded ---
        int fpos = _fieldsAdded.IndexOf(fieldName);
        if (fpos == -1)
        {
            _fieldsAdded.Add(fieldName);
            _rowsAdded.Add("0");
            fpos = _fieldsAdded.Count - 1;
        }

        // --- Exit if invalid row ---
        if (!validity)
        {
            //s = structObj.axprovider.dbm.gf.Pad(frameNo.ToString(), 2, ' ')
            //    + structObj.axprovider.dbm.gf.Pad(map.Attributes["trow"], 2, ' ');
            //s = _utils.Pad(frameNo.ToString(), 2, ' ')
            //   + _utils.Pad(map?.Attributes?["trow"]?.Value, 2, ' ');
            s = _utils.Pad(frameNo.ToString(), 2, ' ')
               + _utils.Pad(genMapField.TargetRow.ToString(), 2, ' ');

            if (!_invalidRows.Contains(s))
            {
                _invalidRows.Add(s);

                if (_rowExists)
                    DeleteRow(frameNo, x);
            }
            return;
        }

        // --- Update max row list ---
        //if (int.Parse(structDefRec.MaxRowList[frameNo - 1]) < x)
        //    structDefRec.MaxRowList[frameNo - 1] = x.ToString();

        _rowsAdded[fpos] = x.ToString();

        // --- Auto-generate mode field value ---
        if (fld.ModeofEntry == "autogenerate")
        {
            fv = GetFieldValue(fieldName, 1);
            if (!string.IsNullOrEmpty(fv))
                _genFieldValue = fv;
        }

        // --- Save field ---
        //SetFieldValue(
        //     fieldName,
        //     dataType,
        //     saveTableName,
        //     _genFieldValue,
        //     "",
        //     x,
        //     _genIdValue,
        //     0,
        //     frameNo,
        //     0,
        //     sourceKey
        // );

        SubmitValue(
                fieldName,
                x,
                _genFieldValue,
                "",
                _genIdValue,
                0,
                0
            );

        // NOTE: Handle Parser here
        //structDefRec.Validate.Parser.RegisterVar(fieldName, dataType[0], fieldValue);
        Sdef.AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValue);

        //structObj.axprovider.dbm.gf.DoDebug.Msg(
        //    $"Mapped {fieldName} Row = {x} value = {FieldValue} idvalue {IdValue}"
        //);
    }

    public int GetSourceFrameNo(TField field, GenMapField genMapField)
    {
        int result = 0;
        int k, fno, i, j;

        //StructDef sd = new StructDef();

        //if (_parentStructDef != null && _parentStructDef._xml != null)
        //sd = _parentStructDef;

        // Pick correct StoreData
        //if (StoreDataRec.SourceTransId == fTransId)
        //if (ParentStructDef?.Transid == Transid)
        //    {
        //    sd = ParentStructDef;
        //}
        //else
        //{
        //    k = StoreDataRec.SourceIndex;
        //    if (k == -1)
        //        return result;

        //    sd = _structDefList[k];
        //}

        fno = field.FrameNo;

        TField fpb = new TField(); // MUST be declared so Delphi behavior matches

        // Loop through XML child nodes
        //var children = StoreDataRec.Node.ChildNodes;

        //for (i = 0; i < Sdef.GenMaps.Count - 1; i++)
        //{
        //XmlNode na = children[i];

        // Check type="field" and trow=0
        //string typeAttr = na.Attributes?["type"]?.Value;
        string typeAttr = genMapField.Type;
        //string trowStr = na.Attributes?["trow"]?.Value ?? "0";
        int trow = genMapField.TargetRow;

        if (typeAttr == "field" && trow == 0)
        {
            //string tfname = na.Name.ToLower();
            string tfname = genMapField.TargetField.ToLower();
            //string sfname = (na.Attributes?["value"]?.Value ?? "").ToLower();
            //string sfname = _genFieldValue.ToLower();
            string sfname = genMapField.SourceField.ToLower();

            // fpa := StoreDataRec.StoreData.structdef.GetField(tfname)
            var fpa = GetField(tfname);

            if (fpa?.FrameNo == fno)
            {
                // fpb := sd.structdef.GetField(sfname)

                fpb = _parentStructDef.GetField(sfname);

                if (fpb != null && fpb.AsGrid)
                {
                    result = fpb.FrameNo;
                    //break;
                }
                else
                {
                    // Try <tablename>id matching
                    //for (j = 0; j < sd.StructDef.Sdef.Frames.Count; j++)
                    for (j = 0; j < _parentStructDef.Frames.Count; j++)
                    {
                        //var frame = (FrameDef)sd.StructDef.Sdef.Frames[j];
                        var frame = _parentStructDef.Frames[j];

                        if (sfname == (frame.TableName.Trim().ToLower() + "id"))
                        {
                            result = frame.FrameNo;
                            break;
                        }
                    }
                }
            }
        }
        //}


        // Delphi last line:
        // if assigned(fpb) and (result = 0) then result := fpb.FrameNo;
        if (fpb != null && result == 0)
            result = fpb.FrameNo;

        return result;
    }

    public void FillGroupedGridToGrid(GenMapField genMapField)
    {
        //StructDef? sdef = _parentStructDef;

        //if (_parentStructDef == null) return;
        var fld = GetField(genMapField.TargetField);

        if (fld == null) return;
        string fieldName = fld.FieldName;
        string dataType = fld.DataType;
        string saveTableName = fld.Tablename;
        int frameNo = fld.FrameNo;
        bool sourceKey = fld.SourceKey;
        string customDataType = fld.CustType;

        int i = 0, k = 0, fpos = 0, fno = 0;
        int r = 0;
        string groupFieldValue;



        //if (StoreDataRec.SourceTransId.ToLower() ==Transid.ToLower())
        //if (ParentStructDef?.Transid.ToLower() ==Transid.ToLower())
        //{
        //    SD = ParentStructDef;
        //}
        //else
        //{
        //    k = StoreData.SourceIndex;
        //    //SD = ((StoreDataRecWrapper)StoreDataList[k]).StoreData;
        //    SD = _structDefList[k];
        //}

        fno = GetSourceFrameNo(fld, genMapField);
        int maxRow = _parentStructDef.GetRowCount(fno);

        _groupFieldValues.Clear();
        _genFieldValueList.Clear();
        _genIdValueList.Clear();

        // --------- Collect grouped values ----------
        for (i = 1; i <= maxRow; i++)
        {
            if (!ValidRow(i, genMapField))
                continue;

            GetGenFieldValue(fieldName, i, genMapField);

            groupFieldValue = GetFieldValueFromParent(genMapField.Group, i);
            k = _groupFieldValues.IndexOf(groupFieldValue);

            if (k == -1)
            {
                _groupFieldValues.Add(groupFieldValue);

                if (dataType == "n")
                    _genFieldValueList.Add("0");
                else
                    _genFieldValueList.Add("");

                _genIdValueList.Add(0);
                k = _groupFieldValues.Count - 1;
            }

            //GetFieldValue(map.Name, i);
            GetGenFieldValue(genMapField.TargetField, i, genMapField);

            // ID logic
            if (!fld.SourceKey)
            {
                _genIdValue = 0;
            }
            //else if (map.Attributes["id"].Value == "True")
            else if (genMapField.IsID)
            {
                _genIdValue = Int64.Parse(_genFieldValue);
                _genFieldValue = "";
            }

            // Grouped number aggregation or normal value
            if (dataType == "n" && _genFieldValue != "")
            {
                double sum = double.Parse(_genFieldValueList[k]) + double.Parse(_genFieldValue);
                _genFieldValueList[k] = sum.ToString();
            }
            else if (dataType == "d")
            {
                _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, Sdef.clientDateFormat);
            }
            else if (_genFieldValueList[k] == "")
            {
                _genFieldValueList[k] = _genFieldValue;
            }

            _genIdValueList[k] = _genIdValue;
        }

        // ---- Write aggregated rows ----
        fpos = _fieldsAdded.IndexOf(fieldName);
        if (fpos == -1)
        {
            _fieldsAdded.Add(fieldName);
            _rowsAdded.Add("0");
            fpos = _fieldsAdded.Count - 1;
        }

        r = int.Parse(_rowsAdded[fpos]);

        for (i = 0; i < _genFieldValueList.Count; i++)
        {
            r++;

            //SetFieldValue(
            //    fieldName,
            //    dataType,
            //    saveTableName,
            //    _genFieldValueList[i],
            //    "",
            //    r,
            //    _genIdValueList[i],
            //    0,
            //    frameNo,
            //    0,
            //    sourceKey
            //);

            SubmitValue(
               fieldName,
               r,
              _genFieldValueList[i],
               "",
               _genIdValueList[i],
               0,
               0
           );

            //    SetFieldValue(
            //    fieldName,
            //    dataType,
            //    saveTableName,
            //    _genFieldValueList[i],
            //    "",
            //    r,
            //    _genIdValueList[i],
            //    0,
            //    frameNo,
            //    0,
            //    sourceKey
            //);

            //structDefRec.Validate.Parser.RegisterVar(
            //    fieldName,
            //    dataType[0],
            //    fieldValueList[i]
            //);

            Sdef.AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValueList[i]);


            //structObj.axprovider.dbm.gf.DoDebug.Msg(
            //    $"Mapped {fieldName} Row = {r} value = {FieldValueList[i]} idvalue {IdValueList[i]}"
            //);
        }

        //RowsAdded[fpos] = r.ToString();

        // Update MaxRowList
        // Fix: This code should be fixed 
        // ----------Start-------------
        //if (int.Parse(structDefRec.MaxRowList[frameNo - 1]) < r)
        //{
        //    structDefRec.MaxRowList[frameNo - 1] = r.ToString();
        //}
        // --------------End------------
    }

    public void FillGridToGrid(GenMapField genMapField)
    {
        //StructDef? sdef = _parentStructDef;
        TField? fld = GetField(genMapField.TargetField!);
        if (fld == null) return;
        string? fieldName = fld?.FieldName;
        string? dataType = fld?.DataType;
        string? saveTableName = fld?.Tablename;
        int? frameNo = fld?.FrameNo;
        bool? sourceKey = fld?.SourceKey;
        string? customDataType = fld?.CustType;

        int k, i, r = 0;

        // Select correct StoreData
        //if (StoreDataRec.SourceTransId.ToLower() == fTransid.ToLower())
        //{
        //    SD = ParentStoreData;
        //}
        //else
        //{
        //    k = StoreDataRec.SourceIndex;
        //    SD = ((StoreDataRecWrapper)StoreDataList[k]).StoreData;
        //}

        int fno = GetSourceFrameNo(fld!, genMapField);
        int? maxRow = _parentStructDef.GetRowCount(fno);

        // Find or add field
        int fpos = _fieldsAdded.IndexOf(fieldName);
        if (fpos == -1)
        {
            _fieldsAdded.Add(fieldName);
            _rowsAdded.Add("0");
            fpos = _fieldsAdded.Count - 1;
        }

        r = int.Parse(_rowsAdded[fpos]);

        // ------------------ MAIN LOOP ------------------
        for (i = 1; i <= maxRow; i++)
        {
            if (!ValidRow(i, genMapField))
                continue;

            GetGenFieldValue(fieldName, i, genMapField);

            // If numeric auto-id value
            if (dataType == "n" && _genIdValue > 0)
            {
                _genFieldValue = _genIdValue.ToString();
                _genIdValue = 0;
            }
            else if (dataType == "d")
            {
                _genFieldValue = _common.ConvertDBDateToClientDateFormat(customDataType, _genFieldValue, Sdef.clientDateFormat);
            }

            // ID logic
            if (!fld.SourceKey)
            {
                _genIdValue = 0;
            }
            //else if (map.Attributes["id"] == "True")
            else if (genMapField.IsID)
            {
                _genIdValue = Int64.Parse(_genFieldValue);
                _genFieldValue = "";
            }

            r++;

            //SetFieldValue(
            //     fieldName,
            //     dataType,
            //     saveTableName,
            //     _genFieldValue,
            //     "",
            //     r,
            //     _genIdValue,
            //     0,
            //     frameNo,
            //     0,
            //     sourceKey
            // );

            SubmitValue(
               fieldName,
               r,
               _genFieldValue,
               "",
               _genIdValue,
               0,
               0
           );

            //StoreDataRec.Validate.Parser.RegisterVar(
            //    fieldName,
            //    dataType[0],
            //    FieldValue
            //);

            Sdef.AxParser.RegisterVar(fieldName, dataType[0].ToString(), _genFieldValue);


            //structObj.axprovider.dbm.gf.DoDebug.Msg(
            //    $"Mapped {fieldName} Row = {r} value = {FieldValue} idvalue {IdValue}"
            //);

            // update max row count
            //if (int.Parse(StoreDataRec.MaxRowList[frameNo - 1]) < r)
            //{
            //    StoreDataRec.MaxRowList[frameNo - 1] = r.ToString();
            //}
        }

        // Save latest row index
        //RowsAdded[fpos] = r.ToString();
    }

    public List<string> GetChildTransAsList()
    {
        return Sdef.GenMaps.Select(genM => genM.TargetTransId).ToList() ?? [];
    }

    public string GetGenMapName()
    {
        return Sdef.GenMaps?
            .Where(gm => gm.TargetTransId.Equals(Sdef.Struct.TransId, StringComparison.OrdinalIgnoreCase))
            .Select(gm => gm.Name)
            .SingleOrDefault() ?? string.Empty;
    }

    public string GetGenMapName(StructDef parentStructDef)
    {
        return parentStructDef.GenMaps?
            .Where(gm => gm.TargetTransId.Equals(Sdef.Struct.TransId, StringComparison.OrdinalIgnoreCase))
            .Select(gm => gm.Name)
            .SingleOrDefault() ?? string.Empty;
    }

    public void DeleteRow(int dcNo, int RowNo)
    {
        for (int i = 0; i < Sdef.FieldList.Count; i++)
        {
            if (Sdef.FieldList[i].FrameNo == dcNo && Sdef.FieldList[i].RowNo == RowNo)
            {
                Sdef.FieldList[i].OldRow = Sdef.FieldList[i].RowNo;
                Sdef.FieldList[i].RowNo = -1;
                SubmitValue(Sdef.FieldList[i].FieldName, -1, "", Sdef.FieldList[i].OldValue, 0, Sdef.FieldList[i].OldIdValue, 0);
            }

            if (Sdef.FieldList[i].FrameNo == dcNo && Sdef.FieldList[i].RowNo > RowNo)
            {
                Sdef.FieldList[i].RowNo = Sdef.FieldList[i].RowNo - 1;
            }
        }
    }

    public void SaveGenMap(StructDef parentStructDef)
    {
        if (parentStructDef == null) // || parentStructDef._xml == null)
            throw new ArgumentNullException(nameof(parentStructDef), "Parent structdef is not assigned.");

        _parentStructDef = parentStructDef;
        TGenMap currentGenMap = _parentStructDef.GenMaps?.FirstOrDefault(gm => gm.TargetTransId.Equals(Sdef.Struct.TransId, StringComparison.OrdinalIgnoreCase));

        //genMap := Findgenmap using current trsuctdef transid (structdef.transid=Parentstructdef.genmap.targettransid)
        //loop genmap.map and call Fill methods

        Sdef.MapName = currentGenMap.Name;
        Sdef.SourceId = _parentStructDef.LastSavedRecordid;

        if (Sdef.AxParser == null)
            throw new ArgumentNullException(nameof(Sdef.AxParser), "Sdef.AxParser not assigned");

        foreach (var genFld in currentGenMap.FieldMappings)
        {

            if (ShouldProcessAsSpecificRow(genFld))
            {
                FillSpecificRow(genFld);
            }
            else if (ShouldProcessAsGroupedGrid(genFld))
            {
                FillGroupedGridToGrid(genFld);
            }
            else
            {
                FillGridToGrid(genFld);
            }
        }

        // [feat]: Adds rowAction "create" to each new row in genMap edit if not added
        foreach (var frm in Sdef.Frames)
        {
            AddRowAction(frm);
        }

    }

    public void AddRowAction(TFrame frame)
    {
        //var frame = Frames[frameNo];
        var frameNo = frame.FrameNo; ;

        for (int i = 1; i <= frame.RowCount; i++)
        {
            var curRowAction = GetRowAction(frameNo, i); // i = row number

            if (string.IsNullOrEmpty(curRowAction))
            {
                var rowAction = "create";

                if (frame.AsGrid)
                {
                    SetGridRowAction(frameNo, i, rowAction);
                }
                else
                {
                    SetNonGridRowAction(frame.TableName, rowAction);
                }
            }
        }
    }

    public string GetNthString(string SrcString, int StrPos)
    {
        string result = "";
        int i = 0;
        int k = 1;

        while (k <= StrPos && i < SrcString.Length)
        {
            if (SrcString[i] == ',')
            {
                k++;
            }
            else if (k == StrPos)
            {
                result += SrcString[i];
            }
            i++;
        }

        return result;
    }

    public string GetNthString(string SrcString, int StrPos, char Separator)
    {
        string result = "";
        int i = 0;
        int k = 1;

        while (k <= StrPos && i < SrcString.Length)
        {
            if (SrcString[i] == Separator)
            {
                k++;
            }
            else if (k == StrPos)
            {
                result += SrcString[i];
            }
            i++;
        }

        return result;
    }

    public string GetNthString(string SrcString, int StrPos, string Separator)
    {
        if (Separator.Length == 1)
        {
            return GetNthString(SrcString, StrPos, Separator[0]);
        }
        else
        {
            string result = "";
            int i = 0;
            int k = 1;
            int searchLength = Separator.Length;

            while (k <= StrPos && i < SrcString.Length)
            {
                if (
                    i + searchLength <= SrcString.Length
                    && SrcString.Substring(i, searchLength) == Separator
                )
                {
                    k++;
                    i += searchLength;
                }
                else if (k == StrPos)
                {
                    result += SrcString[i];
                    i++;
                }
                else
                {
                    i++;
                }
            }

            return result;
        }
    }

    public bool IsKeyword(string fname)
    {
        var _keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string keywordList =
            "ADD,ALTER,AND,ANY,AS,ASC,AUDIT,BETWEEN,BY,CHAR,CHECK,CLUSTER,COLUMN,COMMENT"
            + ",COMPRESS,CONNECT,CREATE,CURRENT,DATE,DECIMAL,DEFAULT,DELETUE"
            + ",DESC,DISTINCT,DROP,ELSE,EXCLUSIVE,EXISTS,FILE,FLOAT,FOR,FROM"
            + ",GRANT,GROUP,HAVING,IDENTIFIED,IMMEDIATE,IN,INCREMENT,INDEX,INITIAL"
            + ",INSERT,INTEGER,INTERSECT,INTO,IS,LEVEL,LIKE,LOCK,LONG,MAXEXTENTS,MINUS"
            + ",MODE,MODIFY,NOAUDIT,NOCOMPRESS,NOT,NOWAIT,NULL,NUMBER,OF,OFFLINE,OPTION"
            + ",OR,ORDER,PCTFREE,PRIOR,PRIVILEGES,PUBLIC,RAW,RENAME,RESOURCE,REVOKE"
            + ",ROW,ROWID,ROWLABEL,ROWNUM,ROWS,SELECT,SESSION,SET,SHARE,SIZE,SMALLINT"
            + ",START,SUCCESSFUL,SYNONYM,SYSDATE,TABLE,THEN,TO,TRIGGER,UID,UNION"
            + ",UNIQUE,USER,VALIDATE,VALUES,VARCHAR,VARCHAR2,VIEW,WHENEVER,WHERE,WITH"
            + ",SIZE,SELECT,DROP,AS,IN,ADD,UPDATE,ALTER,FIELD,COLUMN,ORDER,VIEW"
            + ",TABLE,TAB,FROM,DATABASE,VALUES,ORDER BY,DEFAULT,VALUE,JOINT,PASSWORD"
            + ",GROUP,NO,NOT,TO,FieldChanged,GetRCell,Upper,Lower,DTOC"
            + ",CTOD,RND,ROUND,STUFF,IIF,AmtWord,Val,STR,SUBSTR,Convert,RevConvert"
            + ",Encode,Decode,HashChar,MandY,DaysElapsed,TimeElapsed,AddToTime"
            + ",AddToDate,AddToMonth,IsEmptyValue,LastDayOfMonth,ValidEncodeDate"
            + ",Eval,IsEmpty,Abs,MakeDate,Date,FormatDateTime,Time,DayofDate"
            + ",MonthofDate,YearofDate,Gen_id,Total,Sum,GetMax,GetValue,GetId"
            + ",GetRow,GetRowCount,GetCostRate,GetAdjustedAmount,SumTill"
            + ",GetCell,GETINTEGER,Power,CreatedBy,CreatedOn"
            + ",AxpCeil,AxpFloor,Days360,data";

        foreach (var keyword in keywordList.Split(','))
        {
            _keywords.Add(keyword.Trim().ToUpper());
        }

        return _keywords.Contains(fname?.ToUpper() ?? string.Empty);
    }

    /// <summary>
    /// Generates SQL & prepares parameter for MDMap
    /// </summary>
    public async Task<List<SQLCommandBuilder>> DoMDMapAsync()
    {
        foreach (var mdMap in Sdef.MdMaps)
        {
            if (mdMap.FieldMappings.Count == 0)
                continue;

            // Check control field
            if (!string.IsNullOrEmpty(mdMap.ControlField))
            {
                TField controlField = GetField(mdMap.ControlField);
                if (controlField != null)
                {
                    string cfValue = GetFieldValue(controlField, mdMap.SourceFrame);
                    if (cfValue != "T") continue;
                }
            }

            var command = await BuildMdMapOperationAsync(mdMap);
            if (command != null && (!string.IsNullOrEmpty(command.Query)))
            {
                Sdef.PreparedSqlOperations.Add(command);
            }
        }

        return Sdef.PreparedSqlOperations;
    }

    /// <summary>
    /// Returns DbType for equelent .Net type
    /// </summary>
    public static DbType GetFldDbType(Type type)
    {
        // Handle nullable types by getting the underlying type
        Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (_typeMap.TryGetValue(effectiveType, out DbType value))
        {
            return value;
        }
        else
        {
            throw new ArgumentException($"Type {effectiveType.Name} is not mapped to a DbType.");
        }
    }

    #endregion

    #region Private methods

    /// <param name="list">TStringList to get value from</param>
    /// <param name="key">Key to find value for</param>
    /// <returns>Value associated with key, null if not found</returns>
    /// <example>GetValueFromList(fieldValList, "5") returns "John Doe"</example>
    private string GetValueFromList(List<string> list, string key)
    {
        foreach (string item in list)
        {
            if (item.StartsWith(key + "="))
                return item.Substring(key.Length + 1);
        }
        return "";
    }

    private string GetValueFromList(Dictionary<string, string> list, string key)
    {
        if (list.TryGetValue(key, out string value))
            return value;

        return "";
    }

    private int FindIndexInList(Dictionary<string, string> dict, string key)
    {
        int i = 0;
        foreach (var kvp in dict)
        {
            if (kvp.Key == key)
                return i;
            i++;
        }
        return -1; // key not found
    }

    private void SetAutoGenList()
    {
        if (Sdef.SdAutogenFields.Count > 0)
            return;
        if (!Sdef.HasAutoGenFields)
            return;

        try
        {
            string seqtbl = GetFieldValue("Axp_SeqTable", 1)?.Trim();
            if (string.IsNullOrEmpty(seqtbl))
                seqtbl = "Sequence";
            string sqlLower = GetSqlLower();

            string sql =
                $"SELECT * FROM {Sdef.CompanyName.ToUpper()}{seqtbl} "
                + $"WHERE {sqlLower}(TRANSTYPE) = {QuotedStr(_transType.ToLower())} "
                + "ORDER BY activesequence DESC";

            // Execute SQL using your DBHelper
            var result = ExecuteSQLAsync(sql).Result;

            if (result.data.Rows.Count > 0)
            {
                foreach (DataRow row in result.data.Rows)
                {
                    string prefix = GetRowValue(row, "prefix")?.ToLower() ?? "";
                    string prefixField = GetRowValue(row, "prefixfield")?.ToLower() ?? "";
                    string fieldName = GetRowValue(row, "fieldname")?.ToLower() ?? "";

                    _prefixes.Add(prefix);
                    _prefixFields.Add(prefixField);
                    Sdef.SdAutogenFields.Add(fieldName);
                    _autogenTransid.Add(_transType.ToLower());
                    _autoGenVals.Add("");
                }
            }
        }
        catch (Exception ex)
        {
            // Handle error - log or throw
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error in SetAutoGenList: {ex.Message}");
        }
    }

    /// <param name="value">String value to quote</param>
    /// <returns>SQL-quoted string with escaped single quotes</returns>
    /// <example>QuotedStr("O'Reilly") returns "'O''Reilly'"</example>
    private string QuotedStr(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    /// <param name="row">DataRow to get value from</param>
    /// <param name="columnName">Column name to retrieve value from</param>
    /// <returns>Column value as string, empty if column doesn't exist</returns>
    /// <example>GetRowValue(dataRow, "CustomerName") returns "John Doe"</example>
    private string GetRowValue(DataRow row, string columnName)
    {
        return row.Table.Columns.Contains(columnName) ? row[columnName]?.ToString() ?? "" : "";
    }

    /// <param name="transid">Transaction ID for prefix lookup</param>
    /// <param name="fieldname">Field name to get prefix for</param>
    /// <returns>User-specific active prefix for the field</returns>
    /// <example>GetUserActivePrefix("SALES", "InvoiceNo") returns "INV-"</example>
    private string GetUserActivePrefix(string transid, string fieldname)
    {
        try
        {
            string sqlLower = GetSqlLower();
            string username = Sdef.UserName?.ToLower() ?? "";

            string wstr =
                $"{sqlLower}(TRANSTYPE) = {QuotedStr(transid.ToLower())} "
                + $"AND {sqlLower}(FIELDNAME) = {QuotedStr(fieldname.ToLower())} "
                + $"AND {sqlLower}(uname) = {QuotedStr(username)}";

            string sql = $"SELECT prefix FROM {Sdef.CompanyName.ToUpper()}USERSEQUENCE WHERE {wstr}";

            // Execute the query
            var result = ExecuteSQLAsync(sql).Result;

            if (result.data.Rows.Count == 0)
                return "";

            return GetRowValue(result.data.Rows[0], "prefix") ?? "";
        }
        catch (Exception ex)
        {
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error in GetUserActivePrefix: {ex.Message}");
            return "";
        }
    }

    private string GetSqlLower()
    {
        //if (_common._dbHelper == null)
        //    await _common.OpenDBConnectionAsync();

        //var dbVars = DBHelper.GetDBVars(_common._dbHelper.GetDBType());

        return _common._dbVars.ContainsKey("sqllower") ? _common._dbVars["sqllower"] : "LOWER";
    }

    private async Task<SQLResult> ExecuteSQLAsync(
        string sql,
        string[] paramNames = null,
        DbType[] paramTypes = null,
        object[] paramValues = null
    )
    {
        if (_common._dbHelper == null)
            await _common.OpenDBConnectionAsync();

        var sqlResult = await _common._dbHelper?.ExecuteSQLAsync(
            sql,
            paramNames,
            paramTypes,
            paramValues
        );

        return sqlResult;
    }

    private bool ShouldProcessAsSpecificRow(GenMapField genFld)
    {
        return genFld.TargetRow > 0;
    }

    private bool ShouldProcessAsGroupedGrid(GenMapField genFld)
    {
        return !string.IsNullOrEmpty(genFld?.Group);
    }

    private bool ShouldProcessAsGridToGrid(GenMapField genFld)
    {
        return genFld.TargetRow == 0 && string.IsNullOrEmpty(genFld?.Group);
    }

    private void GetGenFieldValue(string fieldName, int rowNo, GenMapField genMapField)
    {
        //string nodeValue = _currentMap.InnerText.ToLower();
        //string fieldType = _currentMap.Attributes?["type"]?.Value ?? "";
        //string fieldType = _currentMap.Attributes?["type"]?.Value ?? "";

        //if (fieldName == "primaryrecordid")
        if (genMapField.SourceField == "primaryrecordid")
        {
            //_fieldValue = _parentPrimaryId.ToString();
            _genFieldValue = "primaryrecordid";
            _genIdValue = 0;
        }
        else if (genMapField.SourceField == "sourcerecordid")
        {
            _genFieldValue = "sourcerecordid";
            int fieldIndex = GetFieldIndex(fieldName, rowNo);

            if (fieldIndex != -1)
            {
                //var fieldRec = FieldList[fieldIndex];
                //if (!string.IsNullOrEmpty(fieldRec.OldValue))
                //    _genFieldValue = fieldRec.OldValue;
                _genFieldValue = GetOldFieldValue(fieldName, rowNo);
            }
            _genIdValue = 0;
        }
        else if (genMapField.SourceField == "parentrecordid")
        {
            _genFieldValue = "parentrecordid";
            _genIdValue = 0;
        }
        else if (genMapField.Type == "field")
        {
            _genFieldValue = GetFieldValueFromParent(genMapField.SourceField, rowNo);

            // Handle auto generation records
            //if (PostAutoGen && !string.IsNullOrEmpty(_parentTransId))
            //{
            //    if (IsParentAutoRecFound(_parentTransId, _parentFldName))
            //    {
            //        var autoRec = new AutoGenRec
            //        {
            //            TransId = _storeDataRec.StoreData._transType,
            //            FieldName = fieldName,
            //            RType = "dep",
            //            ParentTransId = _parentTransId,
            //            ParentFldName = _parentFldName,
            //            RowNo = 1,
            //            Schema = Sdef.CompanyName,
            //            TableName = _currentField.Tablename,
            //            ParentList = null,
            //            RecordIdUpdated = false
            //        };

            //        _struct.AxProvider.Dbm.Gf.AutoGenData.Add(autoRec);
            //        AddDepsToAutoGenList(fieldName);
            //    }
            //}
        }
        else
        {
            //_genFieldValue = genMapField.TargetField;
            _genFieldValue = genMapField.SourceField;
            //string idValueStr = _currentMap.Attributes?["idvalue"]?.Value;
            //string idValueStr = _genIdValue.ToString();
            string idValueStr = genMapField.IdValue;
            _genIdValue = Sdef.StrToInt64(idValueStr);
        }
    }

    private string GetFieldValueFromParent(string fieldName, int rowNo)
    {
        // This is Handle in SaveGenMap
        //if (_parentStructDef == null || _parentStructDef?._xml == null)
        //    return string.Empty;
        //throw new ArgumentNullException(nameof(_parentStructDef), "Parent structdef is not assigned.");

        return _parentStructDef?.GetFieldValue(fieldName, rowNo);
    }

    /// <summary>
    /// Gets the source frame number from MDMap list
    /// </summary>
    private int GetSourceFrameNo(XmlNode mapNode)
    {
        int result = -1;
        string operationType = GetNodeValue(mapNode, Consts.xml_Type);

        // For non-increment/decrement operations
        if (operationType != "Inc" && operationType != "Dec")
        {
            string detailField = GetNodeValue(mapNode, Consts.xml_DField);
            TField field = GetField(detailField);

            if (field == null)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"DetailField of MdMap does not exist: ({field})");
                throw new Exception("DetailField of MdMap does not exist");
            }

            result = field.FrameNo;
            bool isGridField = field.AsGrid;

            // Check detail search field
            string detailSearchField = GetNodeValue(mapNode, Consts.xml_DSField);
            field = GetField(detailSearchField);

            if (field == null)
            {
                if (_traceService.IsTracingEnabled())
                    _traceService.LogMessage($"DetailSearchField of MdMap does not exist: ({field})");
                throw new Exception("DetailSearchField of MdMap does not exist");
            }

            if (field.AsGrid && !isGridField)
            {
                result = field.FrameNo;
                isGridField = true;
            }

            // Check control field
            string controlField = GetNodeValue(mapNode, Consts.xml_MDControl);
            if (!string.IsNullOrEmpty(controlField))
            {
                field = GetField(controlField);

                if (field == null)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogError($"ControlField of MdMap does not exist: ({field})");
                    throw new Exception("ControlField of MdMap does not exist");
                }

                if (field.AsGrid && !isGridField)
                {
                    result = field.FrameNo;
                    isGridField = true;
                }
            }
        }
        else
        {
            // For increment/decrement operations
            result = 1;
        }

        return result;
    }

    /// <param name="parent">Parent XML node to search under</param>
    /// <param name="nodeName">Name of child node to find</param>
    /// <returns>Inner text of the node, empty string if not found</returns>
    /// <example>GetNodeValue(xmlNode, "Caption") returns "Customer Details"</example>
    private string GetNodeValue(XmlNode parent, string nodeName)
    {
        XmlNode node = parent?.SelectSingleNode(nodeName);
        return node?.InnerText ?? "";
    }


    /// <summary>
    /// Builds a complete MDMap operation with proper SQL and parameters
    /// </summary>
    private async Task<SQLCommandBuilder> BuildMdMapOperationAsync(TMdMap mdMap)
    {
        var columnNames = new List<string>();
        var values = new List<object>();
        var parameters = new List<string>();
        var fldDbTypes = new List<DbType>();

        SetSourceValuesAsync(mdMap);

        string targetTable = GetFullTableName(mdMap);
        string whereClause = $"WHERE {mdMap.MasterSearchField} = @DetailSearchValue";

        //columnNames.Add(mdMap.MasterSearchField);
        parameters.Add("@DetailSearchValue");

        var dsField = GetField(mdMap.DetailSearchField);

        if (dsField.SourceKey)
            values.Add(GetIDValue(dsField, 1));
        else
            values.Add(GetFieldValue(dsField, 1));

        fldDbTypes.Add(GetFieldDBType(dsField));

        // Check if record exists
        string sql = $"SELECT * FROM {targetTable} {whereClause}";
        bool isRecordExist = false;
        SQLResult result = null;

        try
        {
            result = await ExecuteSQLAsync(sql, parameters.ToArray(), fldDbTypes.ToArray(), values.ToArray());

            if (result != null && result.data.Rows.Count > 0)
            {
                isRecordExist = Convert.ToInt64(result.data.Rows[0][0]) > 0;
            }
        }
        catch (Exception ex)
        {
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error checking record existence: {ex.Message}");
        }

        if (!isRecordExist && !mdMap.Append)
            return null;

        string query = "";

        // Build SQL and parameters
        if (isRecordExist)
        {
            query = BuildUpdateSql(mdMap, targetTable, whereClause, columnNames, parameters, values, fldDbTypes, result.data);
        }
        else
        {
            query = BuildInsertSql(mdMap, targetTable, columnNames, parameters, values, fldDbTypes, result.data);
        }

        var sqlCommand =
              new SQLCommandBuilder(
                query,
                parameters.ToArray(),
                fldDbTypes.ToArray(),
                values.ToArray(),
                isRecordExist ? Constants.DATA_ACTION.UPDATE : Constants.DATA_ACTION.INSERT
              );


        return sqlCommand;
    }

    /// <summary>
    /// Builds INSERT SQL with numeric column logic
    /// </summary>
    private string BuildInsertSql(
        TMdMap mdMap,
        string tableName,
        List<string> columnNames,
        List<string> parameters,
        List<object> values,
        List<DbType> fldDbTypes,
        DataTable data)
    {
        // Always include master search field
        if (!string.IsNullOrEmpty(mdMap.MasterSearchField))
        {
            columnNames.Add(mdMap.MasterSearchField);
            //parameters.Add("@detailSearchValue");
            //values.Add(value);
            //dbTypes.Add(dbType);
        }

        // Process each field mapping
        for (int i = 0; i < mdMap.FieldMappings.Count; i++)
        {
            var (columnClause, parameterClause, value, fldDbType) = FormatParams(mdMap.FieldMappings[i]);

            columnNames.Add(mdMap.FieldMappings[i].TargetField);
            parameters.Add(parameterClause);
            values.Add(value);
            fldDbTypes.Add(fldDbType);
        }

        foreach (DataColumn clm in data.Columns)
        {
            DbType fldDbType = GetFldDbType(clm.DataType);

            if (IsNumericType(fldDbType))
            {
                if (!columnNames.Contains(clm.ToString()))
                {
                    columnNames.Add(clm.ToString());
                    parameters.Add($"@{clm.ToString()}");

                    if (clm.ColumnName.ToLower() == tableName + "id")
                    {
                        var dt = GetRecordIdsFromDB(1).GetAwaiter().GetResult();
                        var recId = Convert.ToInt64(dt.data.Rows[0][0]);
                        values.Add(recId);
                        fldDbTypes.Add(DbType.Int64);
                    }
                    else
                    {
                        values.Add(0);
                        fldDbTypes.Add(fldDbType);
                    }
                }
            }
        }

        if (columnNames.Count == 0)
        {
            return string.Empty;
        }

        return $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) " +
               $"VALUES ({string.Join(", ", parameters)})";
    }

    /// <summary>
    /// Builds UPDATE SQL with operation-specific logic
    /// </summary>
    private string BuildUpdateSql(
    TMdMap mdMap,
        string tableName,
        string whereClause,
        List<string> columnNames,
        List<string> parameters,
        List<object> values,
        List<DbType> fldDbTypes,
        DataTable mappedData)
    {
        var setClauses = new List<string>();

        for (int i = 0; i < mdMap.FieldMappings.Count; i++)
        {
            var setClause = BuildSetClause(mdMap.FieldMappings[i], mappedData);

            if (mdMap.FieldMappings[i].Operation != OperationType.Inc &&
                mdMap.FieldMappings[i].Operation != OperationType.Dec)
            {
                var (columnClause, parameterClause, value, fldDbType) = FormatParams(mdMap.FieldMappings[i]);
                columnNames.Add(columnClause);
                parameters.Add(parameterClause);
                values.Add(value);
                fldDbTypes.Add(fldDbType);
            }

            if (!string.IsNullOrEmpty(setClause))
            {
                setClauses.Add(setClause);

            }
        }

        if (setClauses.Count == 0)
        {
            return string.Empty;
        }

        return $"UPDATE {tableName} SET {string.Join(", ", setClauses)} {whereClause}";
    }

    /// <summary>
    /// Sets the source field value in each FieldMapping
    /// </summary>
    private void SetSourceValuesAsync(TMdMap mdMap)
    {
        foreach (var field in mdMap.FieldMappings)
        {
            field.SourceValue = GetFieldValue(field.SourceField, 1);
        }
    }

    /// <summary>
    /// Builds operation-specific SET clause
    /// </summary>
    private string BuildSetClause(MdMapField field, DataTable mappedData)
    {
        string paramName = $"@{field.SourceField}";

        return field.Operation switch
        {
            OperationType.Normal or OperationType.Replace =>
                $"{field.TargetField} = {paramName}",

            OperationType.Add =>
                $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} + {paramName}",

            OperationType.Less =>
                $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} - {paramName}",

            OperationType.Highest =>
                BuildHighestClause(field.TargetField, paramName),

            OperationType.Lowest =>
                BuildLowestClause(field.TargetField, paramName),

            OperationType.Inc =>
                $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} + 1",

            OperationType.Dec =>
                $"{field.TargetField} = {GetFieldValueFromDT(mappedData, field.TargetField)} - 1",

            _ => $"{field.TargetField} = {paramName}"
        };
    }

    /// <summary>
    /// Builds highest value clause
    /// </summary>
    private string BuildHighestClause(string field, string paramName)
    {
        switch (_common._dbType)
        {
            case "sqlserver":
                return $"{field} = CASE WHEN {paramName} > {field} THEN {paramName} ELSE {field} END";

            case "oracle":
                // Oracle has GREATEST function but handles NULLs differently
                return $"{field} = COALESCE(GREATEST({field}, {paramName}), {field}, {paramName})";

            default:
                // MySQL, PostgreSQL, SQLite, etc.
                return $"{field} = GREATEST({field}, {paramName})";
        }
    }

    /// <summary>
    /// Builds lowest value clause
    /// </summary>
    private string BuildLowestClause(string field, string paramName)
    {
        switch (_common._dbType)
        {
            case "sqlserver":
                // Handle empty strings and NULLs properly
                return $"{field} = CASE WHEN {field} IS NULL THEN {paramName} " +
                       $"WHEN {field} = '' THEN CASE WHEN {paramName} IS NULL THEN '' ELSE {paramName} END " +
                       $"WHEN {paramName} < {field} THEN {paramName} ELSE {field} END";

            case "oracle":
                // Oracle's LEAST function returns NULL if any argument is NULL
                return $"{field} = COALESCE(LEAST({field}, {paramName}), {field}, {paramName})";

            default:
                // MySQL, PostgreSQL, SQLite, etc.
                return $"{field} = LEAST({field}, {paramName})";
        }
    }

    /// <summary>
    /// Creates parameter for a field
    /// </summary>
    private (string columnName, string paramValue, object value, DbType paramDbType)
        FormatParams(MdMapField mdField)
    {
        if (string.IsNullOrEmpty(mdField.SourceField))
            return ("", "", null, DbType.String);

        DbType fldDbType = GetFieldDBType(mdField.SourceField);
        string dataType = GetFieldDataType(mdField.SourceField);
        object formattedValue = FormatValueForSql(
            mdField.SourceValue,
            dataType,
            mdField.DecimalPlaces
        );

        return (mdField.SourceField, $"@{mdField.SourceField}", formattedValue ?? DBNull.Value, fldDbType);
    }

    /// <summary>
    /// Formats value for parameter using field type
    /// </summary>
    private object FormatValueForSql(string value, string dataType, int decimalPlaces) //, bool isInsert)
    {
        if (string.IsNullOrEmpty(value))
            return DBNull.Value;
        //return isInsert && dataType?.ToUpper() == "N" ? 0 : DBNull.Value;

        try
        {
            switch (dataType?.ToUpper())
            {
                case "D": // Date
                    if (DateTime.TryParse(value, out DateTime dateValue))
                        return dateValue;
                    return DBNull.Value;
                //return isInsert ? DateTime.Now : DBNull.Value;

                case "N": // Numeric
                    if (decimalPlaces > 0)
                    {
                        if (decimal.TryParse(value, out decimal decValue))
                            return Math.Round(decValue, decimalPlaces);
                    }
                    else
                    {
                        if (long.TryParse(value, out long longValue))
                            return longValue;
                    }
                    return 0;

                default: // String and other types
                    return value;
            }
        }
        catch (Exception ex)
        {
            if (_traceService.IsTracingEnabled())
                _traceService.LogError($"Error formatting value: {ex.Message}");
            return DBNull.Value;
            //return isInsert && dataType?.ToUpper() == "N" ? 0 : DBNull.Value;
        }
    }

    public static string GetFieldValueFromDT(DataTable table, string columnName)
    {
        if (table == null || table.Rows.Count == 0)
            return null;

        // Find column with case-insensitive match
        var column = table.Columns.Cast<DataColumn>()
            .FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

        if (column == null)
            return null;

        return table.Rows[0][column.ColumnName]?.ToString() ?? "";
    }

    public async Task<SQLResult> GetRecordIdsFromDB(Int32 rowsCount)
    {
        if (_common._dbHelper == null)
            await _common.OpenDBConnectionAsync();

        int siteNo = 0;
        try
        {
            siteNo = Convert.ToInt32(_common._sessionVars[ARMCommon.Helpers.Constants.SESSION_DATA.SITENO.ToString()].ToString());
            if (siteNo < 0)
                siteNo = 1;
        }
        catch
        {
        }

        string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

        string sql = string.Empty;

        if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
        {
            sql = Constants_SQL.AXPUT_GETRECORDIDS_MSSQL;
            string[] paramName = { "@psiteno", "@pnoofrows" };
            DbType[] paramType = { DbType.Int32, DbType.Int32 };
            object[] paramValue = { siteNo, rowsCount };

            var dsResult = await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue);
            return dsResult;
        }
        else
        {
            sql = Constants_SQL.AXPUT_GETRECORDIDS.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.AXPUT_GETRECORDIDS_ORACLE.ToString();

            string[] paramName = { "@siteno", "@rowscount" };
            DbType[] paramType = { DbType.Int32, DbType.Int32 };
            object[] paramValue = { siteNo, rowsCount };

            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }



    }

    /// <summary>
    /// Gets full table name with company/schema prefix
    /// </summary>
    private string GetFullTableName(TMdMap mdMap)
    {
        if (!string.IsNullOrEmpty(mdMap.CompanyName))
            return $"{mdMap.CompanyName}.{mdMap.Table}";
        return mdMap.Table;
    }

    /// <summary>
    /// Checks the DbType is numeric or not
    /// </summary>
    private bool IsNumericType(DbType dbType)
    {
        switch (dbType)
        {
            case DbType.Byte:
            case DbType.SByte:
            case DbType.Int16:
            case DbType.UInt16:
            case DbType.Int32:
            case DbType.UInt32:
            case DbType.Int64:
            case DbType.UInt64:
            case DbType.Decimal:
            case DbType.Double:
            case DbType.Single:
            case DbType.VarNumeric:
                return true;
            default:
                return false;
        }
    }

    private void InitializeFldLookup()
    {
        if (_fieldLookup == null || !_lookupInitialized)
        {
            _fieldLookup = Sdef.Flds?.ToDictionary(
                f => f.FieldName,
                StringComparer.OrdinalIgnoreCase
            ) ?? new Dictionary<string, TField>();
            _lookupInitialized = true;
        }
    }


    #endregion

}