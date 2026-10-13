using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxDataObject.Models;
using AxDataObject.Models.Dto;
using AxStructDef;
using AxStructDef.Models;
using AxValidate;
using AxValidate.Dto;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Globalization;
using static ARMCommon.Helpers.Constants;
using AxDataObject.Interface;

namespace AxDataObject.Repository;

public class DataObjectRepository : IDataObjectRepository
{
    private readonly Utils _common;
    private readonly IARMLogTraceService _traceService;

    // DataObject TTL: tied to a session window (e.g. 8 hours).
    // Adjust or make configurable via appsettings.
    private static readonly int DataObjectTtl = 28800;
    //private static readonly TimeSpan DataObjectTtl = TimeSpan.FromHours(8);
    private Validate _validate;
    private bool _track;
    //private TrackHistory _trackHistory;
    public DataObjectRepository(Utils common, IARMLogTraceService traceService)
    {
        _common = common;
        _traceService = traceService;
    }

    //public void SetValidationContext(Validate validate)
    //{
    //    _validate = validate;
    //}

    // ── Redis key helpers ──────────────────────────────────
    private string DataObjectKey(string schemaName, string transId, string userName, string recordId) =>
       _common.GenerateRedisKeyString(new[] { schemaName, "ARM", transId, userName, recordId.ToString(), "ARM-AX-DATAOBJECT" });

    private string StructDefKey(string transId) =>
        _common.GenerateRedisKeyString(new[] { _common.GetSchemaName(), "ARM", transId, "ARM-AX-STRUCTDEF" });

    // ── DataObject Redis CRUD ──────────────────────────────


    public async Task<DataObject> GetDataObjectAsync(string schemaName, string transId, string userName, string recordId)
    {
        var connected = await _common.OpenRedisConnectionAsync();
        if (!connected) return null;
        var key = DataObjectKey(schemaName, transId, userName, recordId);
        var json = await _common._redisCache.StringGetAsync(key);
        if (string.IsNullOrEmpty(json)) return null;
        return JsonConvert.DeserializeObject<DataObject>(json);
    }

    public async Task<bool> SaveDataObjectAsync(DataObject dataObject, string schemaName, string transId, string userName, string recordId)
    {
        var connected = await _common.OpenRedisConnectionAsync();
        if (!connected) return false;
        var key = DataObjectKey(schemaName, transId, userName, recordId);
        return await _common._redisCache.StringSetAsync(key, JsonConvert.SerializeObject(dataObject), DataObjectTtl);
    }

    public async Task<bool> DeleteDataObjectAsync(string schemaName, string transId, string userName, string recordId)
    {
        var connected = await _common.OpenRedisConnectionAsync();
        if (!connected) return false;
        return await _common._redisCache.KeyDeleteAsync(DataObjectKey(schemaName, transId, userName, recordId));
    }
   
    // ── StructDef (shared, read-only) ─────────────────────

    public async Task<StructDef> GetStructDefAsync(string transId, bool refreshCache = false)
    {
        return await StructDef.LoadStructure(_common, transId, null, _traceService, refreshCache);
    }

    // ── Permissions ───────────────────────────────────────

    public async Task<SQLResult> GetViewPermissionAsync(GetDataRequest req)
    {
        await EnsureDBOpenAsync();
        string dbType = GetDbType();
        string sql;

        if (req.Roles?.Count == 0)
            req.Roles.Add("All");

        if (dbType == Constants.DBTYPE.MSSQL.ToString())
        {
            sql = Constants_SQL.GET_AXGET_VIEWPERMISSION_MSSQL;
            return await _common._dbHelper.ExecuteStoredProcedureAsync(sql,
                new[] { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles", "@pglobalvars" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.CalledFrom, req.TransId, req.KeyField, $"'{req.KeyValue}'", GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
        else
        {
            sql = dbType == Constants.DBTYPE.ORACLE.ToString()
                ? Constants_SQL.GET_AXGET_VIEWPERMISSION_ORACLE
                : Constants_SQL.GET_AXGET_VIEWPERMISSION;
            return await _common._dbHelper.ExecuteSQLAsync(sql,
                new[] { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles", "@dimensions" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.CalledFrom, req.TransId, req.KeyField, $"'{req.KeyValue}'", GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
    }

    public async Task<SQLResult> GetCreatePermissionAsync(SetDataRequest req)
    {
        await EnsureDBOpenAsync();
        string dbType = GetDbType();
        string sql;

        if (req.Roles?.Count == 0)
            req.Roles.Add("All");

        if (dbType == Constants.DBTYPE.MSSQL.ToString())
        {
            sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION_MSSQL;
            return await _common._dbHelper.ExecuteStoredProcedureAsync(sql,
                new[] { "@pmode", "@ptransid", "@pusername", "@proles", "@pglobalvars" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.Action, req.TransId, GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
        else
        {
            sql = dbType == Constants.DBTYPE.ORACLE.ToString()
                ? Constants_SQL.GET_AXPUT_CREATEPERMISSION_ORACLE
                : Constants_SQL.GET_AXPUT_CREATEPERMISSION;
            return await _common._dbHelper.ExecuteSQLAsync(sql,
                new[] { "@action", "@transid", "@username", "@roles", "@dimensions" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.Action, req.TransId, GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
    }

    public async Task<SQLResult> GetEditPermissionAsync(SetDataRequest req)
    {
        await EnsureDBOpenAsync();
        string dbType = GetDbType();
        string sql;

        if (req.Roles?.Count == 0)
            req.Roles.Add("All");

        if (dbType == Constants.DBTYPE.MSSQL.ToString())
        {
            sql = Constants_SQL.GET_AXPUT_EDITPERMISSION_MSSQL;
            return await _common._dbHelper.ExecuteStoredProcedureAsync(sql,
                new[] { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles", "@pglobalvars" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.Action, req.TransId, "", $"'{req.RecordId}'", GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
        else
        {
            sql = dbType == Constants.DBTYPE.ORACLE.ToString()
                ? Constants_SQL.GET_AXPUT_EDITPERMISSION_ORACLE
                : Constants_SQL.GET_AXPUT_EDITPERMISSION;
            return await _common._dbHelper.ExecuteSQLAsync(sql,
                new[] { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles", "@dimensions" },
                new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { req.Action, req.TransId, "", $"'{req.RecordId}'", GetUserName(), string.Join(",", req.Roles), req.Dimensions });
        }
    }

    // ── DC-wise SQL ops ───────────────────────────────────

    public async Task<SQLResult> GetDCWiseSQLsAsync(string transId, List<string> roles)
    {
        await EnsureDBOpenAsync();
        string dbType = GetDbType();
        string sql;

        if (roles?.Count == 0)
            roles.Add("All");

        if (dbType == Constants.DBTYPE.MSSQL.ToString())
        {
            sql = Constants_SQL.AXGET_DBGET_MSSQL;
            return await _common._dbHelper.ExecuteSQLAsync(sql,
                new[] { "@ptransid", "@precordid" },
                new[] { DbType.String, DbType.Int64 },
                new object[] { transId, 0L });
        }
        else
        {
            sql = dbType == Constants.DBTYPE.ORACLE.ToString()
                ? Constants_SQL.AXGET_DBGET_ORACLE
                : Constants_SQL.AXGET_DBGET;
            return await _common._dbHelper.ExecuteSQLAsync(sql,
                new[] { "@transid", "@recordid" },
                new[] { DbType.String, DbType.Int64 },
                new object[] { transId, 0L });
        }
    }

    public async Task<SQLResult> ExecuteDCWiseSQLAsync(long recordId, string sql)
    {
        // Normalize Oracle-style :recordid to @recordid for MSSQL
        if (GetDbType() == Constants.DBTYPE.MSSQL.ToString())
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"(?<!\w):recordid(?!\w)", "@recordid");

        return await _common._dbHelper.ExecuteSQLAsync(sql,
            new[] { "@recordid" },
            new[] { DbType.Int64 },
            new object[] { recordId });
    }

    // ── Save ──────────────────────────────────────────────

    public async Task<TransactionResult> ExecuteSaveCommandsAsync(List<SQLCommandBuilder> commands)
    {
        await EnsureDBOpenAsync();
        return await _common._dbHelper.ExecuteSQLsInTransactionAsync(commands, false);
    }

    public async Task<SQLResult> GetChildTransRecordIdAsync(string parentRecId, string table, string mapName)
    {
        await EnsureDBOpenAsync();
        var sql = $"SELECT {table}id FROM {table} WHERE sourceid = {parentRecId} AND mapname = '{mapName}'";
        return await _common._dbHelper.ExecuteSQLAsync(sql);
    }

    // ── Table‑wise SQL generation ──────

    //public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForCreate(AxPut putData, bool isGenMapRecord = false)
    //{
    //    if (_validate._sdef.Struct.TrackChange)
    //    {
    //        _track = true;
    //        _trackHistory = new TrackHistory();
    //    }

    //    var sqlCommands = new List<SQLCommandBuilder>();

    //    Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();
    //    var primaryTable = _validate._sdef.Frames?[0].TableName ?? "";
    //    for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
    //    {
    //        var rowNo = 1;
    //        var currentFrame = _validate._sdef.Frames[fidx];
    //        int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
    //        var tableName = currentFrame.TableName;
    //        var dcName = "dc" + currentFrame.FrameNo;
    //        if (tableWiseSQLCommands.ContainsKey(tableName) || (totalRowCount == 0 && tableName != primaryTable))
    //            continue;
    //        else
    //            tableWiseSQLCommands[tableName] = totalRowCount;

    //        List<TrackField> trackFields = new List<TrackField>();
    //        if (currentFrame.AsGrid)
    //        {
    //            for (int idx = 1; idx <= totalRowCount; idx++)
    //            {
    //                rowNo = idx;

    //                int startFldIdx = currentFrame.StartIndex;
    //                int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
    //                Dictionary<string, string> fieldData = new Dictionary<string, string>();

    //                Int64 gridRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

    //                for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
    //                {
    //                    TField field = _validate._sdef.Flds[fldIdx];
    //                    if (field.SaveValue)
    //                    {
    //                        if (field.SourceKey)
    //                            fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
    //                        else
    //                            fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
    //                    }

    //                    if (_track)
    //                    {
    //                        var value = _validate._sdef.GetFieldValue(field, rowNo);
    //                        if (field.DataType == "d")
    //                            value = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);

    //                        TrackField trackFld = new TrackField(field.FieldName, "", value, rowNo, field.FrameNo, gridRecId, (field.SourceKey ? Convert.ToInt64(fieldData[field.FieldName]) : 0), 0, "Create");
    //                        trackFields.Add(trackFld);
    //                    }
    //                }

    //                var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRecId), rowNo, true, fieldData);
    //                if (sqlCommand != null)
    //                    sqlCommands.Add(sqlCommand);
    //            }
    //        }
    //        else
    //        {
    //            var changedFields = _validate._sdef.GetNGChangedFieldsAsList(tableName, "create");

    //            Int64 rowRecId = 0;
    //            if (primaryTable == tableName)
    //            {
    //                if (putData.RecordId == 0)
    //                {
    //                    rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();
    //                    putData.RecordId = rowRecId;
    //                }
    //            }
    //            else
    //                rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

    //            Dictionary<string, string> fieldData = new Dictionary<string, string>();

    //            foreach (var changedField in changedFields)
    //            {
    //                if (changedField.SaveValue)
    //                {
    //                    if (changedField.SourceKey)
    //                        fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
    //                    else
    //                        fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                }

    //                if (_track)
    //                {
    //                    var value = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                    if (changedField.DataType == "d")
    //                        value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);

    //                    TrackField trackFld = new TrackField(changedField.FieldName, "", value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), 0, "Create");
    //                    trackFields.Add(trackFld);
    //                }
    //            }

    //            if (!string.IsNullOrEmpty(_validate._sdef.MapName))
    //                fieldData.Add("mapname", _validate._sdef.MapName);

    //            if (_validate._sdef.SourceId > 0)
    //                fieldData.Add("sourceid", _validate._sdef.SourceId.ToString());

    //            var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, rowNo, false, fieldData);
    //            if (sqlCommand != null)
    //                sqlCommands.Add(sqlCommand);
    //        }

    //        if (_track && trackFields.Count > 0)
    //            _trackHistory.ChangedFields.AddRange(trackFields);
    //    }
    //    return sqlCommands;
    //}

    //public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForEdit(AxPut putData, bool isGenMapRecord = false)
    //{
    //    if (_validate._sdef.Struct.TrackChange)
    //    {
    //        _track = true;
    //        _trackHistory = new TrackHistory();
    //    }

    //    var sqlCommands = new List<SQLCommandBuilder>();

    //    Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();
    //    var primaryTable = _validate._sdef.Frames?[0].TableName ?? "";
    //    for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
    //    {
    //        var rowNo = 1;
    //        var currentFrame = _validate._sdef.Frames[fidx];
    //        int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
    //        var tableName = currentFrame.TableName;
    //        var dcName = "dc" + currentFrame.FrameNo;
    //        if (tableWiseSQLCommands.ContainsKey(tableName) || (totalRowCount == 0 && tableName != primaryTable))
    //            continue;
    //        else
    //            tableWiseSQLCommands[tableName] = totalRowCount;

    //        List<TrackField> trackFields = new List<TrackField>();
    //        var deletedRows = new List<int>();
    //        if (currentFrame.AsGrid)
    //        {
    //            if (_validate.deletedRowsDict.ContainsKey(currentFrame.FrameNo) && _validate.deletedRowsDict[currentFrame.FrameNo] == true)
    //            {
    //                for (int idx = 1; idx <= totalRowCount; idx++)
    //                {
    //                    rowNo = idx;
    //                    var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo);
    //                    rowAction = rowAction.ToUpper();

    //                    if (rowAction == "DELETE")
    //                    {
    //                        Dictionary<string, string> fieldData = new Dictionary<string, string>();
    //                        var rowRecordId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
    //                        deletedRows.Add(rowNo);
    //                        var sqlCommand = GenerateDeleteStatement(putData, tableName, Convert.ToInt64(rowRecordId));
    //                        if (sqlCommand != null)
    //                            sqlCommands.Add(sqlCommand);

    //                        if (_track)
    //                        {
    //                            var deletedFields = _validate._sdef.GetRowFieldsAsList(currentFrame.FrameNo, rowNo);

    //                            foreach (var deletedField in deletedFields)
    //                            {
    //                                if (deletedField.SaveValue)
    //                                {
    //                                    if (deletedField.SourceKey)
    //                                        fieldData[deletedField.FieldName] = _validate._sdef.GetIDValue(deletedField, rowNo).ToString();
    //                                    else
    //                                        fieldData[deletedField.FieldName] = _validate._sdef.GetFieldValue(deletedField, rowNo);
    //                                }

    //                                if (_track)
    //                                {
    //                                    var oldValue = _validate._sdef.GetOldFieldValue(deletedField, rowNo);
    //                                    if (deletedField.DataType == "d")
    //                                        oldValue = _common.ConvertDBDateToClientDateFormat(deletedField.CustType, oldValue, _validate._sdef.clientDateFormat);

    //                                    TrackField trackFld = new TrackField(deletedField.FieldName, oldValue, "", rowNo, deletedField.FrameNo, Convert.ToInt64(rowRecordId), (deletedField.SourceKey ? Convert.ToInt64(fieldData[deletedField.FieldName]) : 0), (deletedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(deletedField, rowNo)) : 0), rowAction);

    //                                    trackFields.Add(trackFld);
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }

    //            int tempRowNo = 1;
    //            for (int idx = 1; idx <= totalRowCount; idx++)
    //            {
    //                rowNo = idx;

    //                if (deletedRows.Contains(rowNo))
    //                    continue;

    //                var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo);
    //                rowAction = rowAction.ToUpper();

    //                if (rowAction == "CREATE")
    //                {
    //                    int startFldIdx = currentFrame.StartIndex;
    //                    int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
    //                    Dictionary<string, string> fieldData = new Dictionary<string, string>();

    //                    Int64 gridRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

    //                    for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
    //                    {
    //                        TField field = _validate._sdef.Flds[fldIdx];
    //                        if (field.SaveValue)
    //                        {
    //                            if (field.SourceKey)
    //                                fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
    //                            else
    //                                fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
    //                        }

    //                        if (_track)
    //                        {
    //                            var value = _validate._sdef.GetFieldValue(field, rowNo);
    //                            if (field.DataType == "d")
    //                                value = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);

    //                            TrackField trackFld = new TrackField(field.FieldName, "", value, tempRowNo, field.FrameNo, gridRecId, (field.SourceKey ? Convert.ToInt64(fieldData[field.FieldName]) : 0), 0, rowAction);
    //                            trackFields.Add(trackFld);
    //                        }
    //                    }

    //                    var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRecId), tempRowNo, true, fieldData);
    //                    if (sqlCommand != null)
    //                        sqlCommands.Add(sqlCommand);
    //                }
    //                else if (rowAction == "EDIT")
    //                {
    //                    var changedFields = _validate._sdef.GetGridRowChangedFieldsAsList(currentFrame.FrameNo, rowNo);
    //                    if (changedFields == null || changedFields.Count == 0)
    //                    {
    //                        if (tempRowNo != rowNo)
    //                        {
    //                            var tempGridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
    //                            var sqlCommandRowNoUpdate = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(tempGridRowRecId), tempRowNo, true, new Dictionary<string, string>());
    //                            if (sqlCommandRowNoUpdate != null)
    //                                sqlCommands.Add(sqlCommandRowNoUpdate);
    //                        }
    //                        tempRowNo++;
    //                        continue;
    //                    }

    //                    Dictionary<string, string> fieldData = new Dictionary<string, string>();
    //                    var gridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();

    //                    foreach (var changedField in changedFields)
    //                    {
    //                        if (changedField.SaveValue)
    //                        {
    //                            if (changedField.SourceKey)
    //                                fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
    //                            else
    //                                fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                        }

    //                        if (_track)
    //                        {
    //                            var oldValue = _validate._sdef.GetOldFieldValue(changedField, rowNo);
    //                            var value = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                            if (changedField.DataType == "d")
    //                            {
    //                                oldValue = _common.ConvertDBDateToClientDateFormat(changedField.CustType, oldValue, _validate._sdef.clientDateFormat);
    //                                value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);
    //                            }

    //                            TrackField trackFld = new TrackField(changedField.FieldName, oldValue, value, tempRowNo, changedField.FrameNo, Convert.ToInt64(gridRowRecId), (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), (changedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(changedField, rowNo)) : 0), rowAction);

    //                            trackFields.Add(trackFld);
    //                        }
    //                    }
    //                    var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRowRecId), tempRowNo, true, fieldData);
    //                    if (sqlCommand != null)
    //                        sqlCommands.Add(sqlCommand);
    //                }
    //                else if (rowAction == "" && tempRowNo != rowNo)
    //                {
    //                    var gridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
    //                    var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRowRecId), tempRowNo, true, new Dictionary<string, string>());
    //                    if (sqlCommand != null)
    //                        sqlCommands.Add(sqlCommand);
    //                }

    //                tempRowNo++;
    //            }
    //        }
    //        else
    //        {
    //            var changedFields = _validate._sdef.GetNGChangedFieldsAsList(tableName, "edit");
    //            Int64 rowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, 1);
    //            var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo).ToUpper();

    //            if (tableName == primaryTable && (rowAction == "DELETE" || rowAction == "CREATE"))
    //                throw new Exception($"DC rows using same table as Header DC can't be {rowAction.ToLower()}d.");

    //            if (fidx == 0 && string.IsNullOrEmpty(rowAction))
    //                rowAction = "EDIT";

    //            if (rowAction == "DELETE")
    //            {
    //                var sqlCommand = GenerateDeleteStatement(putData, tableName, rowRecId);
    //                if (sqlCommand != null)
    //                    sqlCommands.Add(sqlCommand);

    //                if (_track)
    //                {
    //                    Dictionary<string, string> trkFieldData = new Dictionary<string, string>();
    //                    var deletedFields = _validate._sdef.GetRowFieldsAsList(currentFrame.FrameNo, 1);

    //                    foreach (var deletedField in deletedFields)
    //                    {
    //                        if (deletedField.SaveValue)
    //                        {
    //                            if (deletedField.SourceKey)
    //                                trkFieldData[deletedField.FieldName] = _validate._sdef.GetIDValue(deletedField, 1).ToString();
    //                            else
    //                                trkFieldData[deletedField.FieldName] = _validate._sdef.GetFieldValue(deletedField, 1);
    //                        }

    //                        var oldValue = _validate._sdef.GetOldFieldValue(deletedField, 1);
    //                        if (deletedField.DataType == "d")
    //                        {
    //                            oldValue = _common.ConvertDBDateToClientDateFormat(deletedField.CustType, oldValue, _validate._sdef.clientDateFormat);
    //                        }

    //                        TrackField trackFld = new TrackField(deletedField.FieldName, oldValue, "", 1, deletedField.FrameNo, rowRecId, (deletedField.SourceKey ? Convert.ToInt64(trkFieldData[deletedField.FieldName]) : 0), (deletedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(deletedField, 1)) : 0), rowAction);

    //                        trackFields.Add(trackFld);
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                Dictionary<string, string> fieldData = new Dictionary<string, string>();
    //                if (rowAction == "CREATE")
    //                {
    //                    rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();
    //                }

    //                foreach (var changedField in changedFields)
    //                {
    //                    if (changedField.SaveValue)
    //                    {
    //                        if (changedField.SourceKey)
    //                            fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
    //                        else
    //                            fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                    }

    //                    if (_track)
    //                    {
    //                        var value = _validate._sdef.GetFieldValue(changedField, rowNo);
    //                        if (changedField.DataType == "d")
    //                            value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);

    //                        if (rowAction == "CREATE")
    //                        {
    //                            TrackField trackFld = new TrackField(changedField.FieldName, "", value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), 0, rowAction);
    //                            trackFields.Add(trackFld);
    //                        }
    //                        else if (rowAction == "EDIT")
    //                        {
    //                            var oldValue = _validate._sdef.GetOldFieldValue(changedField, rowNo);
    //                            if (changedField.DataType == "d")
    //                                oldValue = _common.ConvertDBDateToClientDateFormat(changedField.CustType, oldValue, _validate._sdef.clientDateFormat);

    //                            TrackField trackFld = new TrackField(changedField.FieldName, oldValue, value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), (changedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(changedField, rowNo)) : 0), rowAction);
    //                            trackFields.Add(trackFld);
    //                        }
    //                    }
    //                }

    //                if (rowAction == "CREATE")
    //                {
    //                    var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, rowNo, false, fieldData);
    //                    if (sqlCommand != null)
    //                        sqlCommands.Add(sqlCommand);
    //                }
    //                else if (rowAction == "EDIT")
    //                {
    //                    var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, 0, false, fieldData);
    //                    if (sqlCommand != null)
    //                        sqlCommands.Add(sqlCommand);
    //                }
    //            }
    //        }

    //        if (_track && trackFields.Count > 0)
    //            _trackHistory.ChangedFields.AddRange(trackFields);
    //    }

    //    return sqlCommands;
    //}

    // ── Helpers ───────────────────────────────────────────

    private async Task EnsureDBOpenAsync()
    {
        if (_common._dbHelper == null)
            await _common.OpenDBConnectionAsync();
    }

    private string GetDbType() =>
        _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

    private string GetUserName() =>
        _common._sessionVars[ARMCommon.Helpers.Constants.SESSION_DATA.USERNAME.ToString()]?.ToString() ?? "";

    //public Int64 GetRecordIdForCreate()
    //{
    //    if (_recordIds == null || _recordIds.Count == 0)
    //    {
    //        _recordIds = GetNewRecordIds(1).Result;
    //    }

    //    Int64 id = _recordIds[0];
    //    _recordIds.RemoveAt(0);
    //    return id;
    //}

    //private SQLCommandBuilder GenerateInsertStatement(AxPut putData, string primaryTableName, string tableName, Int64 recordId, Int64 rowRecordId, int rowNo, bool isGrid, Dictionary<string, string> rowData)
    //{
    //    if (_common._milliSecsInTimeStamp)
    //        putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
    //    else
    //        putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    //    var columnNames = new List<string>();
    //    var parametersForSQL = new List<string>();
    //    var parameters = new List<string>();
    //    var values = new List<object>();
    //    var paramTypes = new List<DbType>();
    //    var recordIdParam = "@recordid";
    //    var rowRecIdParam = $"@{tableName}_row{rowNo}_id";

    //    if (!_recordIdParams.Contains(recordIdParam))
    //        _recordIdParams.Add(recordIdParam);

    //    if (!isGrid)
    //    {
    //        if (primaryTableName == tableName)
    //        {
    //            columnNames = new List<string>
    //                {
    //                    $"{tableName}id", "mapname", "sourceid", "cancel", "createdby",
    //                    "createdon", "username", "modifiedon", "app_desc", "app_level"
    //                };

    //            if (_common._milliSecsInTimeStamp)
    //            {
    //                parametersForSQL = new List<string>
    //                    {
    //                        recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
    //                        _common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@createdon") , "@username",
    //                        _common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@modifiedon") , "@app_desc", "@app_level"
    //                    };
    //            }
    //            else
    //            {
    //                parametersForSQL = new List<string>
    //                    {
    //                        recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
    //                        _common._dbVars["dbdatestring"].Replace("$DATE$", "@createdon") , "@username",
    //                        _common._dbVars["dbdatestring"].Replace("$DATE$", "@modifiedon") , "@app_desc", "@app_level"
    //                    };
    //            }



    //            parameters = new List<string>
    //                {
    //                    recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
    //                    "@createdon" , "@username","@modifiedon" , "@app_desc", "@app_level"
    //                };

    //            values = new List<object>
    //                {
    //                    recordId, (rowData.ContainsKey("mapname") ? rowData["mapname"] : ""), Convert.ToInt64(rowData.ContainsKey("sourceid") ? rowData["sourceid"] : 0), "F", putData.UserName,
    //                    putData.ModifiedOn, putData.UserName, putData.ModifiedOn, 1, 1
    //                };

    //            if (rowData.ContainsKey("mapname"))
    //                rowData.Remove("mapname");

    //            if (rowData.ContainsKey("sourceid"))
    //                rowData.Remove("sourceid");

    //            paramTypes = new List<DbType>
    //                {
    //                    DbType.Int64, DbType.String,  DbType.Int64, DbType.String, DbType.String,
    //                    DbType.String, DbType.String, DbType.String, DbType.Int64, DbType.Int64
    //                };
    //        }
    //        else
    //        {
    //            if (!_recordIdParams.Contains(rowRecIdParam))
    //                _recordIdParams.Add(rowRecIdParam);

    //            columnNames = new List<string>
    //                {
    //                    $"{tableName}id", $"{primaryTableName}id"
    //                };

    //            parameters = new List<string>
    //                {
    //                    rowRecIdParam, recordIdParam
    //                };

    //            parametersForSQL = new List<string>
    //                {
    //                    rowRecIdParam, recordIdParam
    //                };

    //            values = new List<object>
    //                {
    //                    rowRecordId, recordId
    //                };

    //            paramTypes = new List<DbType>
    //                {
    //                    DbType.Int64,  DbType.Int64
    //                };
    //        }
    //    }
    //    else
    //    {
    //        if (!_recordIdParams.Contains(rowRecIdParam))
    //            _recordIdParams.Add(rowRecIdParam);

    //        columnNames = new List<string>
    //            {
    //                $"{tableName}row", $"{tableName}id", $"{primaryTableName}id"
    //            };

    //        parameters = new List<string>
    //            {
    //                $"@rowno", rowRecIdParam, recordIdParam
    //            };

    //        parametersForSQL = new List<string>
    //            {
    //                $"@rowno", rowRecIdParam, recordIdParam
    //            };

    //        values = new List<object>
    //            {
    //                rowNo, rowRecordId, recordId
    //            };

    //        paramTypes = new List<DbType>
    //            {
    //                DbType.Int32, DbType.Int64,  DbType.Int64
    //            };
    //    }

    //    foreach (var prop in rowData)
    //    {
    //        if (!string.IsNullOrEmpty(prop.Value))
    //        {
    //            string fieldName = prop.Key;
    //            var fieldType = _validate._sdef.GetFieldDataType(fieldName);
    //            string value = prop.Value;

    //            if (fieldType.ToLower() == "i")
    //            {
    //                ProcessImageUploads(fieldName, value);
    //                continue;
    //            }
    //            else if (fieldName.ToLower().StartsWith("axpfile_"))
    //            {
    //                value = ProcessAxpFileUploads(fieldName, value, rowNo, "INSERT");
    //            }

    //            columnNames.Add(fieldName);
    //            var dbType = _validate._sdef.GetFieldDBType(fieldName);

    //            if (dbType == DbType.Date)
    //            {
    //                if (_common._milliSecsInTimeStamp)
    //                    parametersForSQL.Add(_common._dbVars["dbdatestringwithms"].Replace("$DATE$", $"@{fieldName}"));
    //                else
    //                    parametersForSQL.Add(_common._dbVars["dbdatestring"].Replace("$DATE$", $"@{fieldName}"));

    //                dbType = DbType.String;
    //                values.Add(value);
    //            }
    //            else if (dbType == DbType.Int64)
    //            {
    //                parametersForSQL.Add($"@{fieldName}");
    //                values.Add(Convert.ToInt64(value));
    //            }
    //            else if (dbType == DbType.Decimal)
    //            {
    //                parametersForSQL.Add($"@{fieldName}");
    //                values.Add(Convert.ToDecimal(value));
    //            }
    //            else if (dbType == DbType.Object)
    //            {
    //                parametersForSQL.Add($"@{fieldName}");
    //                values.Add(value);
    //            }
    //            else if (dbType == DbType.Guid)
    //            {
    //                Guid guidValue;

    //                if (Guid.TryParse(value, out guidValue))
    //                {
    //                    parametersForSQL.Add($"@{fieldName}");
    //                    values.Add(guidValue);
    //                }
    //                else
    //                {
    //                    throw new Exception($" {fieldName} is not having a valid GUID. Current value {value}");
    //                }
    //            }
    //            else
    //            {
    //                parametersForSQL.Add($"@{fieldName}");
    //                values.Add(value);
    //            }

    //            parameters.Add($"@{fieldName}");
    //            paramTypes.Add(dbType);

    //        }
    //    }

    //    if (string.IsNullOrEmpty(tableName) || columnNames.Count == 0 || parametersForSQL.Count == 0)
    //        return null;

    //    string sql = $"insert into {tableName} ({string.Join(",", columnNames)}) values ({string.Join(",", parametersForSQL)})";

    //    if (_traceService.IsTracingEnabled())
    //    {
    //        _traceService.LogMessage($"Insert SQL for {tableName}", $"{sql}");
    //        _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
    //        _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
    //        _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
    //    }

    //    return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.INSERT);
    //}

    //private SQLCommandBuilder GenerateUpdateStatement(AxPut putData, string primaryTableName, string tableName, long recordId, long rowRecordId, int rowNo, bool isGrid, Dictionary<string, string> rowData)
    //{
    //    if (_common._milliSecsInTimeStamp)
    //        putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
    //    else
    //        putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    //    var setClauses = new List<string>();
    //    var parameters = new List<string>();
    //    var values = new List<object>();
    //    var paramTypes = new List<DbType>();

    //    // Add common columns for main table (non-grid)
    //    if (!isGrid && primaryTableName == tableName)
    //    {
    //        setClauses.Add($"username = @username");

    //        if (_common._milliSecsInTimeStamp)
    //            setClauses.Add($"modifiedon = {_common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@modifiedon")}");
    //        else
    //            setClauses.Add($"modifiedon = {_common._dbVars["dbdatestring"].Replace("$DATE$", "@modifiedon")}");

    //        setClauses.Add($"app_desc = @app_desc");
    //        setClauses.Add($"app_level = @app_level");

    //        parameters.AddRange(new[] { "@username", "@modifiedon", "@app_desc", "@app_level" });
    //        values.AddRange(new object[] { putData.UserName, putData.ModifiedOn, 1, 1 });
    //        paramTypes.AddRange(new[] { DbType.String, DbType.String, DbType.Int64, DbType.Int64 });
    //    }

    //    // Add user-defined fields dynamically
    //    foreach (var prop in rowData)
    //    {
    //        string fieldName = prop.Key;
    //        var fieldType = _validate._sdef.GetFieldDataType(fieldName);
    //        string value = prop.Value;

    //        if (fieldType.ToLower() == "i")
    //        {
    //            ProcessImageUploads(fieldName, value);
    //            continue;
    //        }
    //        else if (fieldName.ToLower().StartsWith("axpfile_"))
    //        {
    //            value = ProcessAxpFileUploads(fieldName, value, rowNo, "UPDATE");
    //        }

    //        string paramName = $"@{fieldName}";
    //        var dbType = _validate._sdef.GetFieldDBType(fieldName);

    //        if (dbType == DbType.Date)
    //        {
    //            if (string.IsNullOrEmpty(value))
    //            {
    //                setClauses.Add($"{fieldName} = {paramName}");
    //                values.Add(DBNull.Value);
    //            }
    //            else
    //            {
    //                if (_common._milliSecsInTimeStamp)
    //                    setClauses.Add($"{fieldName} = {_common._dbVars["dbdatestringwithms"].Replace("$DATE$", paramName)}");
    //                else
    //                    setClauses.Add($"{fieldName} = {_common._dbVars["dbdatestring"].Replace("$DATE$", paramName)}");

    //                values.Add(value);
    //                dbType = DbType.String; // handled as string since date format string is passed
    //            }

    //        }
    //        else if (dbType == DbType.Int64)
    //        {
    //            setClauses.Add($"{fieldName} = {paramName}");
    //            if (string.IsNullOrEmpty(value))
    //                values.Add(0);
    //            else
    //                values.Add(Convert.ToInt64(value));
    //        }
    //        else if (dbType == DbType.Decimal)
    //        {
    //            setClauses.Add($"{fieldName} = {paramName}");
    //            if (string.IsNullOrEmpty(value))
    //                values.Add(0);
    //            else
    //                values.Add(Convert.ToDecimal(value));
    //        }
    //        else if (dbType == DbType.Object)
    //        {
    //            setClauses.Add($"{fieldName} = {paramName}");
    //            if (string.IsNullOrEmpty(value))
    //                values.Add(DBNull.Value);
    //            else
    //                values.Add(value);
    //        }
    //        else if (dbType == DbType.Guid)
    //        {
    //            Guid guidValue;
    //            setClauses.Add($"{fieldName} = {paramName}");
    //            if (string.IsNullOrEmpty(value))
    //                values.Add(DBNull.Value);
    //            else
    //            {
    //                if (Guid.TryParse(value, out guidValue))
    //                    values.Add(guidValue);
    //                else
    //                    throw new Exception($" {fieldName} is not having a valid GUID. Current value {value}");
    //            }
    //        }
    //        else
    //        {
    //            setClauses.Add($"{fieldName} = {paramName}");
    //            values.Add(value);
    //        }

    //        parameters.Add(paramName);
    //        paramTypes.Add(dbType);
    //    }

    //    // Add row number if applicable (for grid rows)
    //    if (isGrid)
    //    {
    //        setClauses.Add($"{tableName.ToLower()}row = @rowno");
    //        parameters.Add("@rowno");
    //        values.Add(rowNo);
    //        paramTypes.Add(DbType.Int32);
    //    }

    //    // WHERE condition for record ID
    //    string whereClause = $"{tableName}id = @{tableName}id";
    //    parameters.Add($"@{tableName}id");
    //    values.Add(rowRecordId);
    //    paramTypes.Add(DbType.Int64);

    //    if (string.IsNullOrEmpty(tableName) || setClauses.Count == 0)
    //        return null;

    //    // Build SQL
    //    string sql = $"update {tableName} set {string.Join(", ", setClauses)} where {whereClause}";

    //    if (_traceService.IsTracingEnabled())
    //    {
    //        _traceService.LogMessage($"Update SQL for {tableName} : {sql}");
    //        _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
    //        _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
    //        _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
    //    }

    //    return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.UPDATE);
    //}

    //private SQLCommandBuilder GenerateDeleteStatement(AxPut putData, string tableName, long rowRecordId)
    //{
    //    var parameters = new List<string>();
    //    var values = new List<object>();
    //    var paramTypes = new List<DbType>();

    //    // WHERE condition for the delete
    //    string whereClause = $"{tableName}id = @{tableName}id";

    //    parameters.Add($"@{tableName}id");
    //    values.Add(rowRecordId);
    //    paramTypes.Add(DbType.Int64);

    //    if (string.IsNullOrEmpty(tableName))
    //        return null;

    //    string sql = $"delete from {tableName} where {whereClause}";

    //    if (_traceService.IsTracingEnabled())
    //    {
    //        _traceService.LogMessage($"Delete SQL : {sql}");
    //        _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
    //        _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
    //        _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
    //    }


    //    return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.DELETE);
    //}

    public class RecordIdGenerator
    {
        private long _current = 999900000000000;
        private readonly object _lock = new object();

        public long GetTemporaryRecordId()
        {
            lock (_lock)
            {
                _current++;
                return _current;
            }
        }
    }
}
