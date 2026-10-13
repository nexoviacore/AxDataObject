using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
//using AxDataObject.Helpers;
using AxDataObject.Interface;
using AxDataObject.Models;
using AxDataObject.Models.Dto;
using AxDataObject.Repository;
using AxParserApp;
using AxStructDef;
using AxStructDef.Models;
using AxValidate;
using AxValidate.Dto;
using AxValidate.Helpers;
using Newtonsoft.Json;
//using Org.BouncyCastle.Ocsp;
using System.Text.RegularExpressions;

namespace AxDataObject.Service;

public class DataObjectService : IDataObjectService
{
    private readonly IDataObjectRepository _repo;
    private readonly Utils _common;
    private readonly IARMLogTraceService _traceService;
    //private readonly IAxAttachmentsService _attachmentsService;

    public DataObjectService(IDataObjectRepository repo, Utils common,
        IARMLogTraceService traceService)
        //IAxAttachmentsService attachmentsService)
    {
        _repo = repo; _common = common;
        _traceService = traceService;
        //_configuration = configuration;
        //_attachmentsService = attachmentsService;
    }

    // ═══════════════════════════════════════════════════════
    // SetFldValue — single field change from UI / API client
    // ═══════════════════════════════════════════════════════
    public async Task<SetValueResponse> SetFldValueAsync(SetValueRequest req)
    {
        var response = new SetValueResponse();
        var schemaName = _common.GetSchemaName();
        try
        {
            var dataObject = await LoadOrCreateDataObjectAsync(schemaName, req.TransId, GetUserName(), req.RecordId);

            if (dataObject == null)
                return Fail<SetValueResponse>("StructDef not found for transId: " + req.TransId);

            var field = dataObject.GetField(req.FieldName);
            if (field == null) return Fail<SetValueResponse>($"Field '{req.FieldName}' not found.");

            int rowNo = field.AsGrid ? req.RowNo : 1;

            // DataObject.SubmitValue handles date conversion, numeric norm, encrypt
            dataObject.SubmitValue(req.FieldName, rowNo, req.Value, req.IdValue);

            var validate = await CreateValidate(dataObject, req.TransId);
            var changed = await RunRefreshForFieldAsync(validate, dataObject, req.FieldName, rowNo);

            var success = await _repo.SaveDataObjectAsync(dataObject, _common.GetSchemaName(), req.TransId, GetUserName(), req.RecordId);

            if (!success)
                throw new InvalidOperationException($"Failed to save dataObject changes in redis without throwing any error");

            // Build response: return all changed field values
            response.Success = true;
            response.ChangedFields = BuildChangedFieldsMap(dataObject, changed);
        }
        catch (Exception ex)
        {
            LogError(nameof(SetFldValueAsync), ex);
            response.Success = false;
            response.Error = ex.Message;
        }
        return response;
    }

    // ═══════════════════════════════════════════════════════
    // GetFldValue — read a single field value
    // ═══════════════════════════════════════════════════════
    public async Task<GetValueResponse> GetFldValueAsync(GetValueRequest req)
    {
        var response = new GetValueResponse();
        try
        {
            var dataObject = await _repo.GetDataObjectAsync(_common.GetSchemaName(), req.TransId, GetUserName(), req.RecordId);
            
            if (dataObject == null) return Fail<GetValueResponse>("No active DataObject found. Call GetData first.");

            dataObject.ReAttachServices(_common, _traceService);

            response.Value = dataObject.GetFieldValue(req.FieldName, req.RowNo);
            response.IdValue = dataObject.GetIDValue(req.FieldName, req.RowNo);
            response.Success = true;
        }
        catch (Exception ex)
        {
            LogError(nameof(GetFldValueAsync), ex);
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    // ═══════════════════════════════════════════════════════
    // SubmitValue — batch set (e.g. paste / form-level submit)
    // Validates each field after setting.
    // ═══════════════════════════════════════════════════════
    public async Task<SubmitValueResponse> SubmitValueAsync(SubmitValueRequest req)
    {
        var response = new SubmitValueResponse();
        var userName = GetUserName();
        var schemaName = _common.GetSchemaName();

        try
        {
            var dataObject = await LoadOrCreateDataObjectAsync(schemaName, req.TransId, userName, req.RecordId);
            if (dataObject == null) return Fail<SubmitValueResponse>("StructDef not found.");

            //dataObject.Sdef.Action = req.Action;
            long.TryParse(req.RecordId, out var recordId);
            dataObject.Sdef.Recid = recordId;

            // Apply all submitted values — DataObject.SubmitValue handles all type logic
            foreach (var (dcName, rowsDict) in req.SubmitData ?? new())
            {
                int frameNo = int.TryParse(dcName.Replace("dc", ""), out int fn) ? fn : 0;
                foreach (var (rowKey, fieldDict) in rowsDict)
                {
                    if (!int.TryParse(rowKey, out int rowNo)) continue;

                    // axrow_action / axp_recid
                    if (fieldDict.TryGetValue("axrow_action", out var rowAction))
                        dataObject.Sdef.Frames[frameNo - 1].RowActionDict[rowNo.ToString()] = rowAction;
                    if (fieldDict.TryGetValue("axp_recid", out var recIdStr) && long.TryParse(recIdStr, out long recId))
                        dataObject.SubmitRecordId(frameNo, rowNo, recId);

                    foreach (var (fieldName, value) in fieldDict)
                    {
                        if (fieldName is "axrow_action" or "axp_recid") continue;
                        if (fieldName.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase)) continue;

                        var sourceIdKey = fieldName + "_sourceid";
                        long idVal = fieldDict.TryGetValue(sourceIdKey, out var sid) && long.TryParse(sid, out long id) ? id : 0;
                        dataObject.SubmitValue(fieldName, rowNo, value, "", idVal, 0, 0);
                        //dataObject.SubmitValue(fieldName, rowNo, value, 0, 0, idVal, 0);
                    }
                }
            }

            var validate = await CreateValidate(dataObject, req.TransId);
            await validate.RefreshFieldsOnLoad();   // refresh calculated fields first
            var valResult = await validate.ValidateData();

            var success = await _repo.SaveDataObjectAsync(dataObject, _common.GetSchemaName(), req.TransId, userName, req.RecordId);

            if (!success)
                throw new InvalidOperationException($"Failed to save dataObject changes in redis without throwing any error");

            response.Success = valResult.Result == "Success";
            response.ValidationErrors = MapValidationErrors(valResult);
            response.ChangedFields = BuildChangedFieldsMap(dataObject,
               dataObject.Sdef.Flds.Select(f => f.FieldName).ToList());
        }
        catch (Exception ex)
        {
            LogError(nameof(SubmitValueAsync), ex);
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    // ═══════════════════════════════════════════════════════
    // GetData — AxGet: load record from DB into DataObject
    // ═══════════════════════════════════════════════════════
    public async Task<GetDataResponse> GetDataAsync(GetDataRequest req)
    {
        var response = new GetDataResponse();
        try
        {
            var userName = GetUserName();
            var schemaName = _common.GetSchemaName();

            // 1. Permission check
            //var permResult = await _repo.GetViewPermissionAsync(req);
            //if (!IsPermissionGranted(permResult))
            //    return Fail<GetDataResponse>("View permission denied.");

            // 2. Load StructDef (schema)
            var structDef = await _repo.GetStructDefAsync(req.TransId, req.RefreshCache);
            if (structDef == null)
                return Fail<GetDataResponse>("StructDef not found.");

            var dataObject = new DataObject(structDef, _common, _traceService);
            //var dataObject = new DataObject(structDef, req.KeyValue, schemaName, _common, _traceService);
            long.TryParse(req.KeyValue, out var recId);
            dataObject.Sdef.Recid = recId;
            dataObject.Sdef.clientDateFormat = req.AxClient_DateFormat;

            if (recId > 0)
            {
                // Load DC SQLs and populate via DataObject methods
                var dcSqls = await _repo.GetDCWiseSQLsAsync(req.TransId, req.Roles);
                foreach (var frame in structDef.Frames)
                {
                    var sql = GetDCSql("dc" + frame.FrameNo, dcSqls);
                    if (string.IsNullOrEmpty(sql)) continue;

                    var rowData = await _repo.ExecuteDCWiseSQLAsync(recId, sql);
                    if (rowData?.data == null) continue;

                    for (int rowIdx = 0; rowIdx < rowData.data.Rows.Count; rowIdx++)
                    {
                        int rowNo = rowIdx + 1;
                        var dbRow = rowData.data.Rows[rowIdx];

                        // axp_recid — store row-level record ID
                        if (rowData.data.Columns.Contains("axp_recid") &&
                            long.TryParse(dbRow["axp_recid"]?.ToString(), out long rowRecId))
                            dataObject.SubmitRecordId(frame.FrameNo, rowNo, rowRecId);

                        foreach (var fld in structDef.Flds.Where(f => f.FrameNo == frame.FrameNo).ToList())
                        {
                            if (!rowData.data.Columns.Contains(fld.FieldName)) continue;
                            var value = dbRow[fld.FieldName]?.ToString() ?? "";
                            var sourceIdCol = fld.FieldName + "_sourceid";
                            long idVal = rowData.data.Columns.Contains(sourceIdCol) &&
                                         long.TryParse(dbRow[sourceIdCol]?.ToString(), out long sid) ? sid : 0;

                            // SubmitValueOnLoad sets old=new (load state), handles encrypt/decrypt
                            dataObject.SubmitValueOnLoad(fld.FieldName, rowNo, value, idVal);
                        }
                    }
                }
            }

            var validate = await CreateValidate(dataObject, req.TransId);
            await validate.RefreshFieldsOnLoad();

            // 6. Save DataObject to Redis (replaces any previous session state)
            var success = await _repo.SaveDataObjectAsync(dataObject, _common.GetSchemaName(), req.TransId, GetUserName(), req.KeyValue);

            if (!success)
                throw new InvalidOperationException($"Failed to save dataObject changes in redis without throwing any error");

            response.Success = true;
            response.Data = BuildResponseData(dataObject);
            //response.ValidationErrors = validationErrors;
        }
        catch (Exception ex)
        {
            LogError(nameof(GetDataAsync), ex);
            response.Success = false;
            response.Error = ex.Message;
        }
        finally
        {
            await _common.CloseDBConnectionAsync();
        }

        return response;
    }

    // ═══════════════════════════════════════════════════════
    // SetData — AxPut: validate → persist → clear DataObject
    // ═══════════════════════════════════════════════════════
    //public async Task<SetDataResponse> SetDataAsync(SetDataRequest req)
    //{
    //    var response = new SetDataResponse();
    //    try
    //    {
    //        var userName = GetUserName();
    //        var schemaName = _common.GetSchemaName();

    //        // 1. Permission check
    //        SQLResult permResult = req.Action == "create"
    //            ? await _repo.GetCreatePermissionAsync(req)
    //            : await _repo.GetEditPermissionAsync(req);
    //        if (!IsPermissionGranted(permResult))
    //            return Fail<SetDataResponse>($"{req.Action} permission denied.");

    //        // 2. Load DataObject
    //        var dataObject = await _repo.GetDataObjectAsync(schemaName, req.TransId, userName, req.RecordId);
    //        if (dataObject == null)
    //            return Fail<SetDataResponse>("No active DataObject. Call SetValue or GetData first.");

    //        dataObject.ReAttachServices(_common, _traceService);
    //        dataObject.Sdef.clientDateFormat = req.AxClient_DateFormat;

    //        await _common.OpenDBConnectionAsync();
    //        var validate = CreateValidate(dataObject, req.TransId);
    //        var valResult = await validate.ValidateData();
    //        if (valResult.Result == "Fail")
    //        {
    //            response.Success = false;
    //            response.ValidationErrors = MapValidationErrors(valResult);
    //            return response;
    //        }

    //        // 5. Generate SQL commands
    //        var sqlCommands = req.Action == "create"
    //            ? dataObject.GetTableWiseSQLCommandsForCreate()
    //            : dataObject.GetTableWiseSQLCommandsForEdit();

    //        // MDMap (header totals from grid rows)
    //        var mdCommands = await dataObject.DoMDMapAsync();
    //        if (mdCommands?.Any() == true) sqlCommands.AddRange(mdCommands);

    //        // 6. Execute or dry-run
    //        if (!req.ValidateOnly)
    //        {
    //            var txResult = await _repo.ExecuteSaveCommandsAsync(sqlCommands);
    //            if (!txResult.Success)
    //            {
    //                response.Success = false;
    //                response.Error = txResult.Error;
    //                return response;
    //            }

    //            response.SavedRecordId = dataObject.Sdef.NewRecid;

    //            if (req.Attachments?.Any() == true)
    //            {
    //                await _attachmentsService.ProcessAttachments(req.Attachments, response.SavedRecordId, req.TransId);
    //            }

    //            await _repo.DeleteDataObjectAsync(schemaName, req.TransId, userName, req.RecordId);
    //        }

    //        response.Success = true;
    //    }
    //    catch (Exception ex)
    //    {
    //        LogError(nameof(SetDataAsync), ex);
    //        _common.RollbackActiveTransactions();
    //        response.Success = false;
    //        response.Error = ex.Message;
    //    }
    //    finally
    //    {
    //        await _common.CloseDBConnectionAsync();
    //    }
    //    return response;
    //}

    // ═══════════════════════════════════════════════════════
    // Private Helpers
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Loads existing DataObject from Redis, or creates a fresh one from StructDef.
    /// </summary>
    private async Task<DataObject> LoadOrCreateDataObjectAsync(
        string schemaName, string transId, string userName, string recordId)
    {
        var dataObject = await _repo.GetDataObjectAsync(schemaName, transId, userName, recordId);

        if (dataObject != null)
        {
            dataObject.ReAttachServices(_common, _traceService);
            return dataObject;
        }

        // No existing DataObject — load StructDef and wrap it
        var structDef = await _repo.GetStructDefAsync(transId);
        if (structDef == null) return null;

        return new DataObject(structDef, _common, _traceService);
    }

    /// <summary>
    /// Runs StructDef/Validate RefreshField for the changed field and its dependents.
    /// Returns the list of field names that were recalculated.
    /// </summary>
    private async Task<List<string>> RunRefreshForFieldAsync(
        Validate validate, DataObject dataObject, string fieldName, int rowNo)
    {
        var changed = new List<string>();
        var field = dataObject.GetField(fieldName);
        if (field == null) return changed;

        await validate.RefreshFieldAsync(field, rowNo);
        changed.Add(fieldName);

        var dependents = dataObject.Sdef.Flds?
            .Where(f => f.FieldName != fieldName &&
                (f.Cexp?.Contains(fieldName, StringComparison.OrdinalIgnoreCase) == true ))
                //|| f.Dep?.Contains(fieldName, StringComparison.OrdinalIgnoreCase) == true))
            .ToList() ?? new();

        foreach (var dep in dependents)
        {
            await validate.RefreshFieldAsync(dep, dep.AsGrid ? rowNo : 1);
            if (!changed.Contains(dep.FieldName)) changed.Add(dep.FieldName);
        }
        return changed;
    }

    private static string GetDCSql(string dcName, SQLResult dcSqls)
    {
        foreach (System.Data.DataRow row in dcSqls.data.Rows)
        {
            var dcNos = row["dcno"].ToString().Split(',');
            if (dcNos.Any(d => d.Trim().Equals(dcName, StringComparison.OrdinalIgnoreCase)))
                return row["sqltext"].ToString();
        }
        return "";
    }

    /// <summary>
    /// Builds the response dictionary for GetData (field values grouped by DC/frame).
    /// </summary>
    private static Dictionary<string, object> BuildResponseData(DataObject dataObject)
    {
        var result = new Dictionary<string, object>();
        foreach (var frame in dataObject.Sdef.Frames)
        {
            var rows = new List<Dictionary<string, object>>();
            int rowCount = Math.Max(dataObject.GetRowCount(frame.FrameNo), 1);
            var dcFlds = dataObject.Sdef.Flds.Where(f => f.FrameNo == frame.FrameNo).ToList();

            for (int rowNo = 1; rowNo <= rowCount; rowNo++)
            {
                var row = new Dictionary<string, object>();
                row["axp_recid"] = frame.RecordIdDict.TryGetValue(rowNo, out long rid) ? rid : 0L;

                foreach (var fld in dcFlds)
                {
                    row[fld.FieldName] = dataObject.GetFieldValue(fld, rowNo);
                    if (fld.SourceKey)
                        row[fld.FieldName + "_sourceid"] = dataObject.GetIDValue(fld, rowNo);
                }
                rows.Add(row);
            }
            result["dc" + frame.FrameNo] = rows;
        }
        return result;
    }

    private static Dictionary<string, Dictionary<int, string>> BuildChangedFieldsMap(
    DataObject dataObject, List<string> fieldNames)
    {
        var map = new Dictionary<string, Dictionary<int, string>>();
        foreach (var fieldName in fieldNames)
        {
            var fld = dataObject.GetField(fieldName);
            if (fld == null) continue;
            var rowMap = new Dictionary<int, string>();
            int rowCount = fld.AsGrid ? dataObject.GetRowCount(fld.FrameNo) : 1;
            for (int rowNo = 1; rowNo <= rowCount; rowNo++)
                rowMap[rowNo] = dataObject.GetFieldValue(fld, rowNo);
            map[fieldName] = rowMap;
        }
        return map;
    }

    private static bool IsPermissionGranted(SQLResult result) =>
        result?.data != null &&
        result.data.Rows.Count > 0 &&
        result.data.Rows[0]["access"]?.ToString() == "Y";

    private static List<ValidationError> MapValidationErrors(OperationResult result)
    {
        if (result == null || result.Result == "Success") return new();

        // Each entry format: "Validation Fail: Field 'fieldName' (Row No: N) with value '...'; errorMsg"
        // Joined by ";" — but entries also contain ";", so split on the prefix pattern
        var errors = new List<ValidationError>();
        var entries = Regex.Split(result.Message ?? "", @"(?=Validation Fail:)")
            .Where(s => !string.IsNullOrWhiteSpace(s));

        foreach (var entry in entries)
        {
            var err = new ValidationError { Message = entry.Trim().TrimEnd(';').Trim() };
            var fm = Regex.Match(entry, @"Field '([^']+)'");
            var rm = Regex.Match(entry, @"Row No: (\d+)");
            if (fm.Success) err.FieldName = fm.Groups[1].Value;
            if (rm.Success && int.TryParse(rm.Groups[1].Value, out int rn)) err.RowNo = rn;
            errors.Add(err);
        }
        return errors;
    }
    private static T Fail<T>(string error) where T : class, new()
    {
        var result = new T();
        result.GetType().GetProperty("Success")?.SetValue(result, false);
        result.GetType().GetProperty("Error")?.SetValue(result, error);
        return result;
    }

    private string GetUserName() =>
        _common._sessionVars[Constants.SESSION_DATA.USERNAME.ToString()]?.ToString() ?? "";

    private void LogError(string method, Exception ex)
    {
        if (_traceService.IsTracingEnabled())
            _traceService.LogError($"[DataObjectService.{method}] {ex.Message}");
    }

    private async Task<Validate> CreateValidate(DataObject dObj, string transId)
    {
        var validate = new Validate(_common, _traceService);
        //validate._dataObj = dObj;
        //validate._sdef = dObj.Sdef;

        bool isValid = await validate.InitValidateAsync(transId);

        if (!isValid)
        {
            throw new EntryPointNotFoundException($"Error in Validate initialization: {transId}");
        }

        return validate;
    }
}
