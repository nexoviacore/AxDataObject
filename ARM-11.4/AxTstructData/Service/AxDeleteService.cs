using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;
using static ARMCommon.Helpers.Constants;
using AxStructDef;
using AxValidate;
using AxParserApp;
using AutoMapper;
using AxValidate.Dto;
using AxStructDef.Models;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System;
using AxStructDef.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data.SqlClient;

namespace AxTstructData
{
    public class AxDeleteService : IAxDeleteService
    {
        private readonly IAxGetService _getData;
        private readonly IAxDeleteRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private bool isTracing;
        private Validate _validate;
        private AxpertLog _axpertLog = new AxpertLog();
        private TrackHistory _trackHistory = null;
        private bool _track = false;
        //private string _parserJson = string.Empty;
        private readonly IAxParserProvider _parserProvider;

        public AxDeleteService(IAxDeleteRepository repo, Utils common, IARMLogTraceService traceService, IAxGetService getData, IAxParserProvider parserProvider) //, StructDef structDef,
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;
            _validate = new Validate(_common, _traceService);
            _getData = getData;
            _parserProvider = parserProvider;
        }

        public async Task<APIResult> AxDelete(AxDelete deleteData)
        {
            _axpertLog.CalledOn = DateTime.Now;
            isTracing = deleteData.Trace == true;
            _traceService.EnableTracing(isTracing);
            var result = new APIResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(deleteData)}");

            var isValidSession = await _common.ValidateSession(deleteData.ARMSessionId);
            if (!isValidSession)
            {
                result.success = false;
                result.data["error"] = $"SessionId is not valid";
                return result;
            }

            try
            {
                var validRoles = await _common.IsValidRole(deleteData.ARMSessionId, deleteData.Roles, _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

                if (!validRoles)
                {
                    result.success = false;
                    result.data["error"] = $"User does not access to some of the roles.";
                    return result;
                }

                deleteData.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                deleteData.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                deleteData.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();

                if (deleteData.Roles.Count == 0)
                {
                    deleteData.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString().Split(",").ToList();
                }

                _common._milliSecsInTimeStamp = deleteData.MilliSecsInTimeStamp;

                result = await DeleteData(deleteData);

            }
            catch (Exception ex)
            {
                result = new APIResult();
                result.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                result.error = "Error occurred in AxDelete.";

            }
            finally
            {
                if (!result.success)
                    _common.RollbackActiveTransactions();

                await LogAxDeleteDetails(deleteData, result);
                await _common.CloseDBConnectionAsync();
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                result.data["trace"] = await _traceService.GetTraceFile($"AxDelete");
            }


            return result;

        }        

        private async Task<APIResult> CheckTstructDeleteAccess(AxDelete deleteData)
        {
            var result = new APIResult();

            try
            {
                deleteData.Dimensions = _common.GetAxgApplicableDimensionsString();
                var userPermissions = await _repo.GetDeletePermissionFromDB(deleteData);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = "User does not have access to this structure.";
                    return result;
                }

                if (userPermissions.data.Columns.Contains("permissiontype"))
                {
                    var userRows = userPermissions.data.AsEnumerable()
                            .Where(row => row.Field<string>("permissiontype") == "User");

                    if (userRows.Any())
                    {
                        userPermissions.data = userRows.CopyToDataTable();
                    }
                }

                var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
                var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
                var recordid = "";
                var canEdit = false;
                foreach (DataRow dr in userPermissions.data.Rows)
                {
                    var edit = dr["edit_access"]?.ToString() ?? "";
                    var fullControl = dr["fullcontrol"]?.ToString() ?? "";

                    if ((!string.IsNullOrEmpty(fullControl) && fullControl.ToUpper() == "T") || (string.IsNullOrEmpty(fullControl) && (string.IsNullOrEmpty(edit) || (!string.IsNullOrEmpty(edit) && edit.ToUpper() != "NONE"))))
                    {
                        canEdit = true;

                        if (string.IsNullOrEmpty(recordid))
                            recordid = dr["recordid"].ToString();
                    }
                }

                if (!canEdit)
                {
                    result.success = false;
                    result.error = $"User does not have permissions to {deleteData.Action.ToLower()} this record.";
                    if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                    return result;
                }

                if (string.IsNullOrEmpty(recordid))
                {
                    result.success = false;
                    result.error = "Not able to load record. Matching recordid is not available.";
                    if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                    return result;
                }

                result.data["RecordId"] = recordid;
                deleteData.RecordId = Convert.ToInt64(recordid ?? "0");


                //CheckExistanceInTransControl TODO
            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'CheckTstructDeleteAccess'.";

                return apiResult;
            }
            return result;
        }

        private async Task<APIResult> LoadStructure(AxDelete deleteData)
        {
            _validate = new Validate(_common, _traceService);
            _validate._sdef = await StructDef.LoadStructure(_common, deleteData.TransId, deleteData.Roles, _traceService);

            if (_validate._sdef is null)
            {
                return new APIResult(false, $"Error in Tstruct Load Structure. StructDef Not Initialized for transid {deleteData.TransId}");
            }

            //AxParser parser;
            //if (string.IsNullOrEmpty(_parserJson))
            //{
            //    _parserJson = await _common.GetAxParserObjFromCache(deleteData.ARMSessionId);
            //}
            //if (string.IsNullOrEmpty(_parserJson))
            //{
            //    parser = new AxParser(_common, _validate._sdef); //TODO: Non signin case.
            //    _validate._sdef.AxParser = parser;
            //}
            //else
            //{
            //    parser = JsonConvert.DeserializeObject<AxParser>(_parserJson);
            //    parser._common = _common;
            //    parser._structDef = _validate._sdef;
            //    _validate._sdef.AxParser = parser;
            //}

            var parser = await _parserProvider.GetParserAsync(deleteData.ARMSessionId, _validate._sdef);

            _validate._common = _common;
            _validate._parser = parser;
            _validate._parser.RegisterVar("recordid", "n", deleteData.RecordId.ToString());
            _validate._sdef.LastSavedRecordid = deleteData.RecordId;

            bool isValid = await _validate.InitValidateAsync(deleteData.TransId);

            if (!isValid)
            {
                return new APIResult(false, $"Error in Tstruct validate init. StructDef Not Initialized for transid {deleteData.TransId}");
            }

            return new APIResult(true, $"Structure loaded successfully for transid {deleteData.TransId}");
        }

        private async Task<APIResult> DeleteData(AxDelete deleteData)
        {
            var result = new APIResult();
            try
            {
                var accessResult = new APIResult();
                if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.DELETE.ToString())
                {
                    accessResult = await CheckTstructDeleteAccess(deleteData);//TODO
                }
                else if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CANCEL.ToString())
                {
                    accessResult = await CheckTstructDeleteAccess(deleteData);//TODO
                }
                else
                {
                    accessResult.success = false;
                    accessResult.error = $"Invalid action - {deleteData.Action.ToUpper()}.";
                }

                if (!string.IsNullOrEmpty(accessResult.error))
                {
                    result.success = false;
                    result.data["error"] = accessResult.error;
                    result.data["transid"] = deleteData.TransId;
                }
                else 
                { 
                    var loadStructureResult = await LoadStructure(deleteData);
                    if (!loadStructureResult.success)
                    {
                        result = new APIResult(false, loadStructureResult.error);
                        result.data["transid"] = deleteData.TransId;
                    }
                    else
                    {
                        var sqlCommands = new List<SQLCommandBuilder>();

                        var getDataConfig = new MapperConfiguration(cfg => cfg.CreateMap<AxDelete, AxGet>());
                        var getDataMapper = getDataConfig.CreateMapper();
                        AxGet getData = getDataMapper.Map<AxGet>(deleteData);
                        getData.CalledFrom = "SAVEDATA";
                        var loadDataResult = await _getData.LoadData(getData, null, _validate);
                        if (!loadDataResult.success)
                        {
                            result = new APIResult(false, loadDataResult.error);
                            result.data["transid"] = deleteData.TransId;
                            result.data["recordid"] = deleteData.RecordId;
                        }
                        else
                        {
                            if (_validate._sdef.CancelledTrans)
                            {
                                result = new APIResult(false, $"The {deleteData.Action.ToLowerInvariant()} action is not allowed because this record has already been cancelled.");
                                result.data["transid"] = deleteData.TransId;
                                result.data["recordid"] = deleteData.RecordId;
                                return result;
                            }
                            if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.DELETE.ToString())
                            {
                                sqlCommands.AddRange(await GetTableWiseSQLCommandsForDelete(deleteData));
                            }
                            else if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CANCEL.ToString())
                            {
                                sqlCommands.AddRange(await GetSQLCommandsForCancel(deleteData));
                            }
                        }

                        if (sqlCommands.Count > 0)
                        {
                            var deleteTransactionResult = await _repo.ExecuteDeleteSQLCommands(deleteData, sqlCommands);

                            if (deleteTransactionResult.success)
                            {
                                result.success = true;
                                result.data["recordid"] = deleteData.RecordId;
                                result.data["transid"] = deleteData.TransId;
                                result.data["caption"] = _validate._sdef.Struct.Caption;
                                result.data["username"] = deleteData.UserName;
                                if (_track)
                                {
                                    _trackHistory.ModifiedOn = _common.ConvertDBDateToClientDateFormat("TIMESTAMP", deleteData.ModifiedOn, _validate._sdef.clientDateFormat);
                                    _trackHistory.Action = deleteData.Action;

                                    result.data["trackchanges"] = _trackHistory;
                                }
                            }
                            else
                            {
                                result.success = false;
                                result = new APIResult(false, deleteTransactionResult.error);
                                result.data["recordid"] = deleteData.RecordId;
                                result.data["transid"] = deleteData.TransId;
                                result.data["caption"] = _validate._sdef.Struct.Caption;
                                result.data["username"] = deleteData.UserName;
                            }

                        }
                        else
                        {
                            result.success = false;
                            result = new APIResult(false, $"Not able to construct SQLs for transid {deleteData.TransId}.");
                            result.data["recordid"] = deleteData.RecordId;
                            result.data["transid"] = deleteData.TransId;
                        }
                    }
                }
             }
            catch (Exception ex)
            {
                result = new APIResult(false, ex.Message);
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(result.error);
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
                return result;
            }

            if (result.success)
            {
                NonQueryResult commitResult = new NonQueryResult();
                commitResult = _common._dbHelper.CommitTransaction();

                if (string.IsNullOrEmpty(commitResult.error))
                {                    
                    result.success = true;
                    if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.DELETE.ToString())
                    {
                        result.data["message"] = "Deleted successfully.";
                    }
                    else if (deleteData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CANCEL.ToString())
                    {
                        result.data["message"] = "Cancelled successfully.";
                    }
                }
                else
                {
                    result = new APIResult();
                    result.success = false;
                    result.error = commitResult.error;                   
                }
            }
            return result;

        }

        private SQLCommandBuilder GenerateUpdateStatement(AxDelete deleteData, string primaryTableName, long recordId)
        {
            if (_common._milliSecsInTimeStamp)
                deleteData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            else
                deleteData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var setClauses = new List<string>();
            var parameters = new List<string>();
            var values = new List<object>();
            var paramTypes = new List<DbType>();

            setClauses.Add($"cancel = @cancel");
            setClauses.Add($"cancelremarks = @cancelremarks");
            setClauses.Add($"username = @username");
            if (_common._milliSecsInTimeStamp)
                setClauses.Add($"modifiedon = {_common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@modifiedon")}");
            else
                setClauses.Add($"modifiedon = {_common._dbVars["dbdatestring"].Replace("$DATE$", "@modifiedon")}");
            setClauses.Add($"app_desc = @app_desc");
            setClauses.Add($"app_level = @app_level");

            parameters.AddRange(new[] {"@cancel", "@cancelremarks", "@username", "@modifiedon", "@app_desc", "@app_level" });
            values.AddRange(new object[] { "T", deleteData.Remarks ?? "",  deleteData.UserName, deleteData.ModifiedOn, 1, 1 });
            paramTypes.AddRange(new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.Int64, DbType.Int64 });

            string whereClause = $"{primaryTableName}id = @{primaryTableName}id";
            parameters.Add($"@{primaryTableName}id");
            values.Add(recordId);
            paramTypes.Add(DbType.Int64);

            string sql = $"update {primaryTableName} set {string.Join(", ", setClauses)} where {whereClause}";

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Update SQL for {primaryTableName} : {sql}");
                _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
                _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
                _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
            }

            return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.UPDATE);
        }

        private SQLCommandBuilder GenerateDeleteStatement(AxDelete deleteData, string tableName, string primaryTable, long recordId)
        {
            var parameters = new List<string>();
            var values = new List<object>();
            var paramTypes = new List<DbType>();

            // WHERE condition for the delete
            string whereClause = $"{primaryTable}id = @{primaryTable}id";

            parameters.Add($"@{primaryTable}id");
            values.Add(recordId);
            paramTypes.Add(DbType.Int64);

            string sql = $"delete from {tableName} where {whereClause}";

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Delete SQL : {sql}");
                _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
                _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
                _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
            }

            return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.DELETE);
        }

        public async Task<List<SQLCommandBuilder>> GetSQLCommandsForCancel(AxDelete deleteData)
        {            
            var sqlCommands = new List<SQLCommandBuilder>();

            Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();            
            var currentFrame = _validate._sdef.Frames[0];
            var primaryTable = currentFrame.TableName ?? "";
            var sqlCommand = GenerateUpdateStatement(deleteData, primaryTable, Convert.ToInt64(deleteData.RecordId));
            sqlCommands.Add(sqlCommand);           

            return sqlCommands;
        }

        public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForDelete(AxDelete deleteData)
        {
            if (_validate._sdef.Struct.TrackChange)
            {
                _track = true;
                _trackHistory = new TrackHistory();
            }

            if (_common._milliSecsInTimeStamp)
                deleteData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            else
                deleteData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var sqlCommands = new List<SQLCommandBuilder>();

            Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();
            var primaryTable = _validate._sdef.Frames?[0].TableName ?? "";
            for (int fidx = _validate._sdef.Frames.Count - 1; fidx >= 0; fidx--)
            {
                var currentFrame = _validate._sdef.Frames[fidx];
                int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
                var tableName = currentFrame.TableName;
                var dcName = "dc" + currentFrame.FrameNo;
                if (tableWiseSQLCommands.ContainsKey(tableName) || (totalRowCount == 0 && tableName != primaryTable) || (fidx != 0 && tableName == primaryTable))
                    continue;
                else
                    tableWiseSQLCommands[tableName] = totalRowCount;

                List<TrackField> trackFields = new List<TrackField>();
                var deletedRows = new List<int>();

                var sqlCommand = GenerateDeleteStatement(deleteData, tableName, primaryTable, Convert.ToInt64(deleteData.RecordId));
                sqlCommands.Add(sqlCommand);

                if (_track)
                {
                    var rowNo = 1;

                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;
                        Dictionary<string, string> fieldData = new Dictionary<string, string>();
                        var deletedFields = _validate._sdef.GetRowFieldsAsList(currentFrame.FrameNo, rowNo);

                        foreach (var deletedField in deletedFields)
                        {
                            if (deletedField.SaveValue)
                            {
                                if (deletedField.SourceKey)
                                    fieldData[deletedField.FieldName] = _validate._sdef.GetIDValue(deletedField, rowNo).ToString();
                                else
                                    fieldData[deletedField.FieldName] = _validate._sdef.GetFieldValue(deletedField, rowNo);
                            }

                            if (_track)
                            {
                                var oldValue = _validate._sdef.GetOldFieldValue(deletedField, rowNo);
                                if (deletedField.DataType == "d")
                                    oldValue = _common.ConvertDBDateToClientDateFormat(deletedField.CustType, oldValue, _validate._sdef.clientDateFormat);

                                TrackField trackFld = new TrackField(deletedField.FieldName, oldValue, "", rowNo, deletedField.FrameNo, Convert.ToInt64(deleteData.RecordId), (deletedField.SourceKey ? Convert.ToInt64(fieldData[deletedField.FieldName]) : 0), (deletedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(deletedField, rowNo)) : 0), "DELETE");

                                trackFields.Add(trackFld);
                            }

                        }
                    }
                }

                if (_track && trackFields.Count > 0)
                    _trackHistory.ChangedFields.AddRange(trackFields);

            }

            return sqlCommands;
        }

        private async Task LogAxDeleteDetails(AxDelete input, APIResult response)
        {

            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = response.success ? "success" : "error";
            _axpertLog.CallDetails = response.success ? "success" : response.error; ;
            _axpertLog.RecordId = input.RecordId;
            _axpertLog.ServiceName = $"ARM - Ax{input.Action} API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }



    }
}


