using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AutoMapper;
using AxParserApp;
using AxStructDef;
using AxStructDef.Models;
using AxValidate;
using AxValidate.Dto;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using static ARMCommon.Helpers.Constants;

namespace AxTstructData
{
    public class AxPutService : IAxPutService
    {
        private readonly IAxGetService _getData;
        private readonly IAxPutRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        //private SessionData _session;
        private bool isTracing;
        //private StructDef _structDef;
        private Validate _validate;
        //, StructDef structDef, Validate validate
        private AxpertLog _axpertLog = new AxpertLog();
        private TrackHistory _trackHistory = null;
        private bool _track = false;

        private List<StructDef> _structDefList = new List<StructDef>();
        private List<string> _transidList = new List<string>();
        private List<int> _parentList = new List<int>();
        private List<Dictionary<string, object>> _saveDataResultList = new List<Dictionary<string, object>>();
        //private string _parserJson = string.Empty;
        private List<Int64> _recordIds = new List<Int64>();
        private Dictionary<string, List<Int64>> _saveDataRecordIds = new Dictionary<string, List<Int64>>();
        private List<string> _recordIdParams = new List<string>();
        private RecordIdGenerator _recordIdGenerator = new RecordIdGenerator();
        private bool _validateOnly = false;
        private readonly IAxAttachmentsService _attachmentsService;
        private List<AxAttachments> _imagesList = new List<AxAttachments>();
        private List<AxAttachments> _axpFilesList = new List<AxAttachments>();
        private readonly IAxParserProvider _parserProvider;
        public AxPutService(IAxPutRepository repo, Utils common, IARMLogTraceService traceService, IAxGetService getData, IAxAttachmentsService attachmentsService, IAxParserProvider parserProvider)
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;
            _validate = new Validate(_common, _traceService);
            _getData = getData;
            _attachmentsService = attachmentsService;
            _parserProvider = parserProvider;
        }

        public async Task<APIResult> AxPut(AxPutInput putData)
        {
            //string _exeDir = Path.Combine(
            //                    AppDomain.CurrentDomain.BaseDirectory,
            //                    "log");
            //try
            //{

            //    Directory.CreateDirectory(_exeDir);

            //    string _logFileName =
            //        "AxPut-" +
            //        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") +
            //        ".log";

            //    _exeDir = Path.Combine(_exeDir, _logFileName);

            //    LogEnvironmentDetails(_exeDir);

            //}
            //catch (Exception ex)
            //{
            //    File.AppendAllText(_exeDir, $"{ex}\n\n");

            //}


            _axpertLog.CalledOn = DateTime.Now;
            isTracing = putData.Trace == true;
            _validateOnly = putData.ValidateOnly;
            _traceService.EnableTracing(isTracing);
            var result = new APIResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(putData)}");

            var isValidSession = await _common.ValidateSession(putData.ARMSessionId);
            if (!isValidSession)
            {
                result.success = false;
                result.data["error"] = $"SessionId is not valid";
                return result;
            }

            try
            {
                var validRoles = await _common.IsValidRole(putData.ARMSessionId, putData.Roles, _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

                if (!validRoles)
                {
                    result.success = false;
                    result.data["error"] = $"User does not access to some of the roles.";
                    return result;
                }

                putData.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                putData.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                putData.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();

                if (putData.Roles.Count == 0)
                {
                    putData.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString().Split(",").ToList();
                }

                result = await SaveTstructData(putData);

            }
            catch (Exception ex)
            {
                result = new APIResult();
                result.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                result.error = "Error occurred in AxPut.";

            }
            finally
            {
                if (!result.success)
                    _common.RollbackActiveTransactions();

                await LogAxPutDetails(putData, result);
                await _common.CloseDBConnectionAsync();
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                result.data["trace"] = await _traceService.GetTraceFile($"AxPut");
            }


            return result;

        }

//        private void LogEnvironmentDetails(string logFilePath)
//        {
//            try
//            {
//                CultureInfo culture = CultureInfo.CurrentCulture;
//                CultureInfo uiCulture = CultureInfo.CurrentUICulture;
//                WindowsIdentity identity = WindowsIdentity.GetCurrent();

//                DateTime localNow = DateTime.Now;
//                DateTime utcNow = DateTime.UtcNow;
//                TimeZoneInfo timeZone = TimeZoneInfo.Local;

//                string environmentDetails = $@"
//==================== ENVIRONMENT DETAILS ====================

//[Machine]
//Machine Name              : {Environment.MachineName}
//User Name                 : {Environment.UserName}
//Windows Identity          : {identity?.Name}
//Is Authenticated          : {identity?.IsAuthenticated}

//[Operating System]
//OS Version                : {Environment.OSVersion}
//OS Description            : {RuntimeInformation.OSDescription}
//OS Architecture           : {RuntimeInformation.OSArchitecture}

//[.NET Runtime]
//Framework                 : {RuntimeInformation.FrameworkDescription}
//Process Architecture      : {RuntimeInformation.ProcessArchitecture}
//64 Bit Process            : {Environment.Is64BitProcess}
//Process ID                : {Environment.ProcessId}

//[Application]
//Process Path              : {Environment.ProcessPath}
//Current Directory         : {Environment.CurrentDirectory}
//Base Directory            : {AppDomain.CurrentDomain.BaseDirectory}
//Application Domain        : {AppDomain.CurrentDomain.FriendlyName}

//[Culture]
//Current Culture           : {culture.Name}
//Current UI Culture        : {uiCulture.Name}
//Culture Display Name      : {culture.DisplayName}

//[Date Format]
//Short Date Pattern        : {culture.DateTimeFormat.ShortDatePattern}
//Long Date Pattern         : {culture.DateTimeFormat.LongDatePattern}
//Short Time Pattern        : {culture.DateTimeFormat.ShortTimePattern}
//Long Time Pattern         : {culture.DateTimeFormat.LongTimePattern}
//Full DateTime Pattern     : {culture.DateTimeFormat.FullDateTimePattern}
//Date Separator             : {culture.DateTimeFormat.DateSeparator}
//Time Separator             : {culture.DateTimeFormat.TimeSeparator}

//[Date / Time]
//Local DateTime             : {localNow:yyyy-MM-dd HH:mm:ss.fff}
//Local DateTime Kind        : {localNow.Kind}
//UTC DateTime               : {utcNow:yyyy-MM-dd HH:mm:ss.fff}
//UTC DateTime Kind          : {utcNow.Kind}

//[Time Zone]
//Time Zone ID               : {timeZone.Id}
//Time Zone Display Name     : {timeZone.DisplayName}
//Base UTC Offset             : {timeZone.BaseUtcOffset}
//Supports Daylight Saving   : {timeZone.SupportsDaylightSavingTime}

//==============================================================

//";

//                File.AppendAllText(logFilePath, environmentDetails);
//            }
//            catch
//            {
//                // Environment logging should never affect application execution.
//            }
//        }

        private async Task<APIResult> SaveTstructData(AxPutInput putDataInput)
        {
            try
            {
                var result = new APIResult();
                if (putDataInput.Data == null || putDataInput.Data.Count == 0)
                {
                    result.success = false;
                    result.data["error"] = "Data array should not be empty in input json.";
                    return result;
                }

                return await SaveData(putDataInput);
            }
            catch (Exception ex)
            {
                var result = new APIResult(false, "Error occured in 'SaveTstructLoadData'.");

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(result.error);
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                return result;
            }
        }

        public async Task<APIResult> CheckTstructCreateAccess(AxPut putData)
        {
            var result = new APIResult();
            try
            {
                bool canCreate = false;
                var userPermissions = await _repo.GetCreatePermissionFromDB(putData);

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

                foreach (DataRow dr in userPermissions.data.Rows)
                {
                    var create = dr["allowcreate"]?.ToString() ?? "";
                    var fullControl = dr["fullcontrol"]?.ToString() ?? "";

                    if ((!string.IsNullOrEmpty(create) && create.ToUpper() == "YES") || (!string.IsNullOrEmpty(fullControl) && fullControl.ToUpper() == "T"))
                    {
                        canCreate = true;
                        break;
                    }
                }

                if (!canCreate)
                {
                    result.success = false;
                    result.error = "User does not have permissions to create this record.";
                    if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                    return result;
                }
                else
                {
                    result.success = true;
                    result.data["AllowCreate"] = "T";
                }

            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'CheckTstructCreateAccess'.";

                return apiResult;
            }

            return result;
        }

        public async Task<APIResult> CheckTstructEditAccess(AxPut putData)
        {
            var result = new APIResult();

            try
            {

                List<string> excludedDCElements = new List<string>();
                List<string> includeDCElements = new List<string>();
                List<string> excludedElements = new List<string>();
                List<string> includeElements = new List<string>();

                var userPermissions = await _repo.GetEditPermissionFromDB(putData);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = userPermissions?.error ?? "An error occurred while verifying user permissions."; //"User does not have access to this structure.";
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
                        var excludeDCs = dr["edit_excludedc"].ToString() ?? none;
                        var includeDCs = dr["edit_includedc"].ToString() ?? all;
                        var excludeElems = dr["edit_excludeflds"].ToString() ?? none;
                        var includeElems = dr["edit_includeflds"].ToString() ?? all;


                        if (string.IsNullOrEmpty(recordid))
                            recordid = dr["recordid"].ToString();


                        if (!string.IsNullOrEmpty(excludeDCs))
                        {
                            if (!excludedDCElements.Contains(none, StringComparer.OrdinalIgnoreCase))
                            {
                                excludedDCElements.AddRange(excludeDCs.Split(',').Where(x => !excludedDCElements.Contains(x) && !string.IsNullOrEmpty(x)));
                                excludedDCElements = excludedDCElements.Distinct().ToList();
                            }
                            else
                            {
                                excludedDCElements.Clear();
                                excludedDCElements.Add(none);
                            }
                        }

                        if (!string.IsNullOrEmpty(includeDCs))
                        {

                            if (!includeDCElements.Contains(all, StringComparer.OrdinalIgnoreCase))
                            {
                                includeDCElements.AddRange(includeDCs.Split(',').Where(x => !includeDCElements.Contains(x) && !string.IsNullOrEmpty(x)));
                                includeDCElements = includeDCElements.Distinct().ToList();
                            }
                            else
                            {
                                includeDCElements.Clear();
                                includeDCElements.Add(all);
                            }
                        }

                        if (!string.IsNullOrEmpty(excludeElems))
                        {
                            if (!excludedElements.Contains(none, StringComparer.OrdinalIgnoreCase))
                            {
                                excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x) && !string.IsNullOrEmpty(x)));
                                excludedElements = excludedElements.Distinct().ToList();
                            }
                            else
                            {
                                excludedElements.Clear();
                                excludedElements.Add(none);
                            }
                        }

                        if (!string.IsNullOrEmpty(includeElems))
                        {

                            if (!includeElements.Contains(all, StringComparer.OrdinalIgnoreCase))
                            {
                                includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x) && !string.IsNullOrEmpty(x)));
                                includeElements = includeElements.Distinct().ToList();
                            }
                            else
                            {
                                includeElements.Clear();
                                includeElements.Add(all);
                            }
                        }
                    }
                }

                if (!canEdit)
                {
                    result.success = false;
                    result.error = "User does not have permissions to edit this record.";
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

                if (excludedElements.Count == 0) excludedElements.Add(none);
                if (includeElements.Count == 0) includeElements.Add(all);
                if (excludedDCElements.Count == 0) excludedDCElements.Add(none);
                if (includeDCElements.Count == 0) includeDCElements.Add(all);

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"Excluded DC Elements - {string.Join(",", excludedDCElements)}");
                    _traceService.LogMessage($"Included DC Elements - {string.Join(",", includeDCElements)}");
                    _traceService.LogMessage($"Excluded Elements - {string.Join(",", excludedElements)}");
                    _traceService.LogMessage($"Included Elements - {string.Join(",", includeElements)}");
                    _traceService.LogMessage($"RecordId - {recordid}");
                }

                result.data["ExcludedDCs"] = excludedDCElements.Distinct().ToList();
                result.data["IncludedDCs"] = includeDCElements.Distinct().ToList();
                result.data["ExcludedFields"] = excludedElements.Distinct().ToList();
                result.data["IncludedFields"] = includeElements.Distinct().ToList();
                result.data["RecordId"] = recordid;
                putData.RecordId = Convert.ToInt64(recordid ?? "0");

            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'CheckTstructEditAccess'.";

                return apiResult;
            }
            return result;
        }

        private async Task<APIResult> LoadStructure(AxPutInput putDataInput, AxPut putData)
        {
            _validate = new Validate(_common, _traceService);
            _validate._sdef = await StructDef.LoadStructure(_common, putData.TransId, putDataInput.Roles, _traceService);

            if (_validate._sdef is null)
            {
                return new APIResult(false, $"Error in Tstruct Load Structure. StructDef Not Initialized for transid {putData.TransId}");
            }
            else
            {
                _structDefList.Add(_validate._sdef);
            }

            //AxParser parser;
            //if (string.IsNullOrEmpty(_parserJson))
            //{
            //    _parserJson = await _common.GetAxParserObjFromCache(putDataInput.ARMSessionId);
            //}
            //if (string.IsNullOrEmpty(_parserJson))
            //{
            //    parser = new AxParser(_common, _validate._sdef);

            //    // ValidateSession already populated _common._globalVars
            //    // from the current session.
            //    await RestoreSessionGlobals(parser);

            //    // Save a clean parser snapshot for this request and Redis.
            //    await PreserveReconstructedParser(
            //        parser,
            //        putDataInput.ARMSessionId);

            //    _validate._sdef.AxParser = parser;
            //}
            //else
            //{
            //    parser = JsonConvert.DeserializeObject<AxParser>(_parserJson);
            //    parser._common = _common;
            //    parser._structDef = _validate._sdef;
            //    _validate._sdef.AxParser = parser;
            //}

            var parser = await _parserProvider.GetParserAsync(putDataInput.ARMSessionId, _validate._sdef);

            _validate._common = _common;
            _validate._parser = parser;
            _validate._parser.RegisterVar("recordid", "n", putData.RecordId.ToString());
            _validate._sdef.LastSavedRecordid = putData.RecordId;
            //_validate._sdef.TraceService = _traceService;
            if (!string.IsNullOrEmpty(putDataInput.AxClient_DateFormat))
                _validate._sdef.clientDateFormat = putDataInput.AxClient_DateFormat;
            else if (!string.IsNullOrEmpty(_validate._parser.GetVarValue("axclient_dateformat")))
                _validate._sdef.clientDateFormat = _validate._parser.GetVarValue("axclient_dateformat");
            if (isTracing)
            {
                _traceService.LogMessage($"Client date format from AxPut Input Json: {putDataInput.AxClient_DateFormat}");
                _traceService.LogMessage($"Client date format from Global Vars: {_validate._parser.GetVarValue("axclient_dateformat")}");
                _traceService.LogMessage($"Final client date format: {_validate._sdef.clientDateFormat}");
            }

            bool isValid = await _validate.InitValidateAsync(putData.TransId);

            if (!isValid)
            {
                return new APIResult(false, $"Error in Tstruct validate init. StructDef Not Initialized for transid {putData.TransId}");
            }

            return new APIResult(true, $"Structure loaded successfully for transid {putData.TransId}");
        }

        private async Task<APIResult> SaveData(AxPutInput putDataInput)
        {
            int inputTransIndex = 0;
            int transIndex = 0;
            int defIndex = 0;
            int parentDefIndex = -1;
            string transId = putDataInput.Data[0].TransId.ToString();
            _transidList.Add(transId);
            _parentList.Add(-1);
            var result = new APIResult();
            bool error = false;
            try
            {
                Dictionary<string, APIResult> accessControls = new Dictionary<string, APIResult>();
                //_validate.TraceService = _traceService;
                var newRowsCount = GetNewRowsCount(putDataInput);

                _common._milliSecsInTimeStamp = putDataInput.MilliSecsInTimeStamp;

                var dimensions = _common.GetAxgApplicableDimensionsString();
                while (transId != string.Empty)
                {
                    result = new APIResult();
                    _imagesList = new List<AxAttachments>();
                    _axpFilesList = new List<AxAttachments>();

                    var putData = putDataInput.Data[inputTransIndex];
                    putData.UserName = putDataInput.UserName;
                    putData.Roles = putDataInput.Roles;
                    putData.TransId = transId;
                    putData.Dimensions = dimensions;
                    var accessResult = new APIResult();
                    var accessControlKey = (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString() ? $"{putData.TransId}_{putData.Action.ToUpper()}" : $"{putData.TransId}_{putData.Action.ToUpper()}_{putData.KeyField.ToUpper()}_{putData.KeyValue.ToUpper()}");

                    parentDefIndex = _parentList[transIndex];

                    if (accessControls.ContainsKey(accessControlKey))
                    {
                        accessResult = accessControls[accessControlKey];
                    }
                    else
                    {
                        if (parentDefIndex == -1)
                        {
                            if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
                            {
                                accessResult = await CheckTstructCreateAccess(putData);
                            }
                            else if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
                            {
                                accessResult = await CheckTstructEditAccess(putData);
                            }
                            else
                            {
                                accessResult.success = false;
                                accessResult.error = $"Invalid action - {putData.Action.ToUpper()}.";
                            }

                            if (isTracing)
                            {
                                _traceService.LogMessage($"{accessControlKey} Access Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                            }

                            if (!string.IsNullOrEmpty(accessResult.error))
                            {
                                result.success = false;
                                result.data["error"] = accessResult.error;
                                result.data["transid"] = putData.TransId;
                                result.data["index"] = inputTransIndex;
                                break;
                            }
                            else
                            {
                                accessControls[accessControlKey] = accessResult;
                            }
                        }
                    }

                    var loadStructureResult = await LoadStructure(putDataInput, putData);
                    if (!loadStructureResult.success)
                    {
                        result = new APIResult(false, loadStructureResult.error);
                        result.data["transid"] = putData.TransId;
                        result.data["index"] = inputTransIndex;
                        break;
                    }

                    if (newRowsCount > 0 && _recordIds.Count == 0)
                        _recordIds = await GetNewRecordIds(newRowsCount);

                    if (parentDefIndex == -1 && putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
                    {
                        putData.RecordId = Convert.ToInt64(accessResult.data?["RecordId"]?.ToString() ?? "0");

                        var tstructFieldsValidation = await ValidateUserPermissionsAccessForEdit(putData, accessResult);

                        if (!tstructFieldsValidation.IsValid)
                        {
                            result.success = false;
                            result.data["error"] = string.Join(",", tstructFieldsValidation.Errors);
                            result.data["transid"] = putData.TransId;
                            result.data["index"] = inputTransIndex;
                            break;
                        }
                    }

                    defIndex = _structDefList.Count - 1;//array index of the last added element to the structdeflist
                    //parentDefIndex = _parentList[transIndex];

                    var sqlCommands = new List<SQLCommandBuilder>();

                    if (parentDefIndex == -1)
                    {
                        if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
                        {
                            var config = new MapperConfiguration(cfg => cfg.CreateMap<AxPut, TStructRequestDto>());
                            var mapper = config.CreateMapper();
                            TStructRequestDto tstData = mapper.Map<TStructRequestDto>(putData);
                            var submitValueResult = await _validate.SubmitValue(tstData);
                            if (submitValueResult.StartsWith("Error"))
                            {
                                result = new APIResult(false, submitValueResult);
                                result.data["transid"] = putData.TransId;
                                result.data["index"] = inputTransIndex;
                                break;
                            }

                            var validateResult = await _validate.ValidateData();

                            if (validateResult.Result == "Fail")
                            {
                                result = new APIResult(false, validateResult.Message);
                                result.data["transid"] = putData.TransId;
                                result.data["index"] = inputTransIndex;
                                break;
                            }
                            else
                            {
                                sqlCommands.AddRange(await GetTableWiseSQLCommandsForCreate(putData));
                            }
                        }
                        else if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
                        {
                            var getDataConfig = new MapperConfiguration(cfg => cfg.CreateMap<AxPut, AxGet>());
                            var getDataMapper = getDataConfig.CreateMapper();
                            AxGet getData = getDataMapper.Map<AxGet>(putData);
                            getData.CalledFrom = "SAVEDATA";
                            getData.MilliSecsInTimeStamp = putDataInput.MilliSecsInTimeStamp;
                            var loadDataResult = await _getData.LoadData(getData, null, _validate);

                            if (!loadDataResult.success)
                            {
                                result = new APIResult(false, loadDataResult.error);
                                result.data["transid"] = putData.TransId;
                                result.data["index"] = inputTransIndex;
                                break;
                            }
                            else
                            {
                                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxPut, TStructRequestDto>());
                                var mapper = config.CreateMapper();
                                TStructRequestDto tstData = mapper.Map<TStructRequestDto>(putData);
                                tstData.Submitdata = (Dictionary<string, Dictionary<string, Dictionary<string, string>>>)loadDataResult.data["data"];
                                await _validate.SubmitValueOnLoad(tstData);

                                ReconcileSubmittedRecordIds(putData);
                                tstData.Submitdata = putData.SubmitData;
                                var submitValueResult = await _validate.SubmitValue(tstData);
                                if (submitValueResult.StartsWith("Error"))
                                {
                                    result = new APIResult(false, submitValueResult);
                                    result.data["transid"] = putData.TransId;
                                    result.data["index"] = inputTransIndex;
                                    break;
                                }

                                var validateResult = await _validate.ValidateData();

                                if (validateResult.Result == "Fail")
                                {
                                    result = new APIResult(false, validateResult.Message);
                                    result.data["transid"] = putData.TransId;
                                    result.data["index"] = inputTransIndex;
                                    break;
                                }
                                else
                                {
                                    sqlCommands.AddRange(await GetTableWiseSQLCommandsForEdit(putData));
                                }
                            }
                        }
                    }
                    else
                    {
                        string tableName = _validate._sdef.PrimaryTable;
                        string mapName = _validate._sdef.GetGenMapName(_structDefList[parentDefIndex]);
                        var parentRecId = _structDefList[parentDefIndex].LastSavedRecordid.ToString();
                        if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
                        {
                            putData.RecordId = 0;

                        }
                        else if (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
                        {
                            putData.RecordId = await GetChildTransRecordId(parentRecId, tableName, mapName);
                        }

                        var isCreate = putData.RecordId == 0;
                        var isEdit = !isCreate;

                        if (isEdit)
                        {
                            var getDataConfig = new MapperConfiguration(cfg => cfg.CreateMap<AxPut, AxGet>());
                            var getDataMapper = getDataConfig.CreateMapper();
                            AxGet getData = getDataMapper.Map<AxGet>(putData);
                            getData.CalledFrom = "SAVEDATA";

                            var loadDataResult = await _getData.LoadData(getData, null, _validate);

                            if (!loadDataResult.success)
                            {
                                result = new APIResult(false, loadDataResult.error);
                                result.data["transid"] = putData.TransId;
                                result.data["index"] = inputTransIndex;
                                break;
                            }
                            else
                            {
                                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxPut, TStructRequestDto>());
                                var mapper = config.CreateMapper();
                                TStructRequestDto tstData = mapper.Map<TStructRequestDto>(putData);
                                tstData.Submitdata = (Dictionary<string, Dictionary<string, Dictionary<string, string>>>)loadDataResult.data["data"];

                                AddRowAction(tstData.Submitdata);

                                await _validate.SubmitValueOnLoad(tstData);
                            }
                        }

                        _validate._sdef.SaveGenMap(_structDefList[parentDefIndex]);
                        var validateResult = await _validate.ValidateData();

                        if (validateResult.Result == "Fail")
                        {
                            result = new APIResult(false, validateResult.Message);
                            result.data["transid"] = putData.TransId;
                            result.data["index"] = inputTransIndex;
                            break;
                        }

                        sqlCommands.AddRange(isCreate ?
                            await GetTableWiseSQLCommandsForCreate(putData, true) :
                            await GetTableWiseSQLCommandsForEdit(putData, true));

                        int insertCount = sqlCommands.Count(x => x.Action == DATA_ACTION.INSERT);

                        if (insertCount > 0)
                        {
                            _recordIds.AddRange(await GetNewRecordIds(insertCount));

                            foreach (var sqlCommand in sqlCommands.Where(x => x.Action == DATA_ACTION.INSERT))
                            {
                                foreach (var p in _recordIdParams)
                                {
                                    int idx = Array.IndexOf(sqlCommand.ParameterNames, p);
                                    if (idx >= 0)
                                    {
                                        // [fix]: for genmap grid rows it's creating new recordIds also the grid rows are not maped with primaryTable
                                        if (putData.RecordId != 0 && p == "@recordid")
                                        {
                                            sqlCommand.ParameterValues[idx] = putData.RecordId;
                                        }
                                        else
                                        {
                                            sqlCommand.ParameterValues[idx] = GetRecordIdForCreate();
                                        }

                                        if (p == "@recordid")
                                        {
                                            putData.RecordId = Convert.ToInt64(sqlCommand.ParameterValues[idx]);
                                        }
                                    }
                                }
                            }

                        }
                    }

                    //MDMap
                    if (_validate._sdef.MdMaps != null && _validate._sdef.MdMaps.Count > 0)
                    {
                        sqlCommands.AddRange(await _validate._sdef.DoMDMapAsync());
                    }

                    if (sqlCommands.Count > 0)
                    {

                        if (isTracing) _traceService.LogMessage($"Record ID Params = {JsonConvert.SerializeObject(_recordIdParams)}");
                        var saveTransactionResult = await _repo.ExecuteSaveDataSQLCommands(putData, sqlCommands);

                        if (saveTransactionResult.success)
                        {
                            SaveImagesAndAttachments(putDataInput, putData);

                            result.success = true;
                            result.data["message"] = $"{_validate._sdef.Struct.Caption} saved successfully.";
                            result.data["recordid"] = putData.RecordId;
                            result.data["transid"] = putData.TransId;
                            result.data["caption"] = _validate._sdef.Struct.Caption;
                            result.data["username"] = putDataInput.UserName;
                            if (_track)
                            {
                                _trackHistory.ModifiedOn = _common.ConvertDBDateToClientDateFormat("TIMESTAMP", putData.ModifiedOn, _validate._sdef.clientDateFormat);
                                _trackHistory.Action = putData.Action;

                                result.data["trackchanges"] = _trackHistory;
                            }
                            string returnFldName = putData.ReturnField;
                            if (!string.IsNullOrEmpty(returnFldName))
                            {
                                string returnFldVal = _validate._sdef.GetFieldValue(returnFldName, 1);
                                result.data["returnfield"] = returnFldName;
                                result.data["returnfieldval"] = returnFldVal ?? "";
                            }
                            _saveDataResultList.Add(result.data);

                            if (_saveDataRecordIds.ContainsKey(putData.TransId))
                                _saveDataRecordIds[putData.TransId].Add(putData.RecordId);
                            else
                                _saveDataRecordIds[putData.TransId] = new List<Int64>() { putData.RecordId };
                        }
                        else
                        {
                            result.success = false;
                            result = new APIResult(false, saveTransactionResult.error);
                            result.data["recordid"] = (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString()) ? 0 : putData.RecordId;
                            result.data["transid"] = putData.TransId;
                            result.data["caption"] = _validate._sdef.Struct.Caption;
                            result.data["username"] = putDataInput.UserName;
                            result.data["index"] = inputTransIndex;
                            break;
                        }

                    }
                    else
                    {
                        result.success = false;
                        result = new APIResult(false, $"Not able to construct SQLs for transid {putData.TransId}.");
                        result.data["recordid"] = (putData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString()) ? 0 : putData.RecordId;
                        result.data["transid"] = putData.TransId;
                        result.data["index"] = inputTransIndex;
                        break;
                    }

                    _validate._parser.RegisterVar("recordid", "n", putData.RecordId.ToString());
                    _validate._sdef.LastSavedRecordid = putData.RecordId;

                    var childTransiIds = _validate._sdef.GetChildTransAsList();
                    foreach (var childTransiId in childTransiIds)
                    {
                        _transidList.Add(childTransiId);
                        _parentList.Add(defIndex);
                    }

                    if (transIndex == _transidList.Count - 1)
                    {
                        inputTransIndex++;
                        if (inputTransIndex == putDataInput.Data.Count)
                            break;
                        transId = putDataInput.Data[inputTransIndex].TransId.ToString();
                        _transidList.Add(transId);
                        _parentList.Add(-1);
                        transIndex++;
                    }
                    else
                    {
                        transIndex++;
                        transId = _transidList?[transIndex] ?? "";
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

                result.data["username"] = putDataInput.UserName;
                result.data["index"] = inputTransIndex;

                return result;
            }

            if (result.success && _saveDataResultList.Count > 0)
            {
                NonQueryResult commitResult = new NonQueryResult();
                if (_validateOnly)
                    commitResult = _common._dbHelper.RollBackTransaction();
                else
                    commitResult = _common._dbHelper.CommitTransaction();

                if (string.IsNullOrEmpty(commitResult.error))
                {
                    result = new APIResult();
                    result.success = true;
                    if (_validateOnly)
                        result.data["message"] = "Validation successful.";
                    else
                        result.data["data"] = _saveDataResultList;
                }
                else
                {
                    result = new APIResult();
                    result.success = false;
                    result.error = commitResult.error;
                    if (!_validateOnly)
                        result.data["message"] = "Validation failed.";

                }
            }
            return result;

        }

        private bool SaveImagesAndAttachments(AxPutInput putDataInput, AxPut putData)
        {
            _traceService.LogMessage("Save Images and Attachments", "Start");
            bool result = true;
            if (_imagesList.Count > 0)
            {
                foreach (var image in _imagesList)
                {
                    image.RecordId = putData.RecordId;
                    image.TransId = putData.TransId;

                    result = _attachmentsService.SaveImages(image, _validate._sdef.SaveImageInDB, (putData.Action.ToUpper() != Constants.TSTRUCT_ACTIONS.CREATE.ToString()));

                    if (!result)
                        return result;
                }
            }

            if (_axpFilesList.Count > 0)
            {
                foreach (var axpFile in _axpFilesList)
                {
                    axpFile.RecordId = putData.RecordId;
                    axpFile.TransId = putData.TransId;

                    result = _attachmentsService.WriteAxpFilesToFolder(axpFile);

                    if (!result)
                        return result;
                }
            }

            if (_validate._sdef.Struct != null && _validate._sdef.Struct.Attach && putData.Attachments != null && putData.Attachments.Count > 0)
            {
                foreach (var attachment in putData.Attachments)
                {
                    attachment.RecordId = putData.RecordId;
                    attachment.TransId = putData.TransId;

                    result = _attachmentsService.SaveAttachments(attachment);

                    if (!result)
                        return result;
                }
            }
            _traceService.LogMessage("Save Images and Attachments", "End");
            return result;
        }

        private async Task<Int64> GetChildTransRecordId(string parentRecid, string table, string mapName)
        {
            var result = await _repo.GetChildTransRecordId(parentRecid, table, mapName);

            if (!string.IsNullOrEmpty(result.error))
                throw new Exception($"Error fetching Record Id for Target table ({table}): {result.error}");

            string recordIdStr = "0";

            if (result != null && result.data != null && result.data.Rows.Count > 0)
            {
                recordIdStr = result.data.Rows[0][0].ToString();
            }

            Int64.TryParse(recordIdStr, out Int64 RecId);
            return RecId;
        }

        private void AddRowAction(Dictionary<string, Dictionary<string, Dictionary<string, string>>> submitData)
        {
            foreach (var dc in submitData.Keys.ToList())
            {
                var submitRows = submitData[dc].Values.ToList();

                for (int i = 0; i < submitRows.Count; i++)
                {
                    if (!submitRows[i].ContainsKey("axrow_action"))
                    {
                        submitRows[i].Add("axrow_action", "edit");
                    }
                }

            }
        }

        private SQLCommandBuilder GenerateInsertStatement(AxPut putData, string primaryTableName, string tableName, Int64 recordId, Int64 rowRecordId, int rowNo, bool isGrid, Dictionary<string, string> rowData)
        {
            if (_common._milliSecsInTimeStamp)
                putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            else
                putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var columnNames = new List<string>();
            var parametersForSQL = new List<string>();
            var parameters = new List<string>();
            var values = new List<object>();
            var paramTypes = new List<DbType>();
            var recordIdParam = "@recordid";
            var rowRecIdParam = $"@{tableName}_row{rowNo}_id";

            if (!_recordIdParams.Contains(recordIdParam))
                _recordIdParams.Add(recordIdParam);

            if (!isGrid)
            {
                if (primaryTableName == tableName)
                {
                    columnNames = new List<string>
                    {
                        $"{tableName}id", "mapname", "sourceid", "cancel", "createdby",
                        "createdon", "username", "modifiedon", "app_desc", "app_level"
                    };

                    if (_common._milliSecsInTimeStamp)
                    {
                        parametersForSQL = new List<string>
                        {
                            recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
                            _common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@createdon") , "@username",
                            _common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@modifiedon") , "@app_desc", "@app_level"
                        };
                    }
                    else
                    {
                        parametersForSQL = new List<string>
                        {
                            recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
                            _common._dbVars["dbdatestring"].Replace("$DATE$", "@createdon") , "@username",
                            _common._dbVars["dbdatestring"].Replace("$DATE$", "@modifiedon") , "@app_desc", "@app_level"
                        };
                    }



                    parameters = new List<string>
                    {
                        recordIdParam, "@mapname", "@sourceid", "@cancel", "@createdby",
                        "@createdon" , "@username","@modifiedon" , "@app_desc", "@app_level"
                    };

                    values = new List<object>
                    {
                        recordId, (rowData.ContainsKey("mapname") ? rowData["mapname"] : ""), Convert.ToInt64(rowData.ContainsKey("sourceid") ? rowData["sourceid"] : 0), "F", putData.UserName,
                        putData.ModifiedOn, putData.UserName, putData.ModifiedOn, 1, 1
                    };

                    if (rowData.ContainsKey("mapname"))
                        rowData.Remove("mapname");

                    if (rowData.ContainsKey("sourceid"))
                        rowData.Remove("sourceid");

                    paramTypes = new List<DbType>
                    {
                        DbType.Int64, DbType.String,  DbType.Int64, DbType.String, DbType.String,
                        DbType.String, DbType.String, DbType.String, DbType.Int64, DbType.Int64
                    };
                }
                else
                {
                    if (!_recordIdParams.Contains(rowRecIdParam))
                        _recordIdParams.Add(rowRecIdParam);

                    columnNames = new List<string>
                    {
                        $"{tableName}id", $"{primaryTableName}id"
                    };

                    parameters = new List<string>
                    {
                        rowRecIdParam, recordIdParam
                    };

                    parametersForSQL = new List<string>
                    {
                        rowRecIdParam, recordIdParam
                    };

                    values = new List<object>
                    {
                        rowRecordId, recordId
                    };

                    paramTypes = new List<DbType>
                    {
                        DbType.Int64,  DbType.Int64
                    };
                }
            }
            else
            {
                if (!_recordIdParams.Contains(rowRecIdParam))
                    _recordIdParams.Add(rowRecIdParam);

                columnNames = new List<string>
                {
                    $"{tableName}row", $"{tableName}id", $"{primaryTableName}id"
                };

                parameters = new List<string>
                {
                    $"@rowno", rowRecIdParam, recordIdParam
                };

                parametersForSQL = new List<string>
                {
                    $"@rowno", rowRecIdParam, recordIdParam
                };

                values = new List<object>
                {
                    rowNo, rowRecordId, recordId
                };

                paramTypes = new List<DbType>
                {
                    DbType.Int32, DbType.Int64,  DbType.Int64
                };
            }

            foreach (var prop in rowData)
            {
                if (!string.IsNullOrEmpty(prop.Value))
                {
                    string fieldName = prop.Key;
                    var fieldType = _validate._sdef.GetFieldDataType(fieldName);
                    string value = prop.Value;

                    if (fieldType.ToLower() == "i")
                    {
                        ProcessImageUploads(fieldName, value);
                        continue;
                    }
                    else if (fieldName.ToLower().StartsWith("axpfile_"))
                    {
                        value = ProcessAxpFileUploads(fieldName, value, rowNo, "INSERT");
                    }

                    columnNames.Add(fieldName);
                    var dbType = _validate._sdef.GetFieldDBType(fieldName);

                    if (dbType == DbType.Date || dbType == DbType.DateTime)
                    {
                        if (_common._milliSecsInTimeStamp)
                            parametersForSQL.Add(_common._dbVars["dbdatestringwithms"].Replace("$DATE$", $"@{fieldName}"));
                        else
                            parametersForSQL.Add(_common._dbVars["dbdatestring"].Replace("$DATE$", $"@{fieldName}"));

                        dbType = DbType.String;
                        values.Add(value);
                    }
                    else if (dbType == DbType.Int64)
                    {
                        parametersForSQL.Add($"@{fieldName}");
                        values.Add(Convert.ToInt64(value));
                    }
                    else if (dbType == DbType.Decimal)
                    {
                        parametersForSQL.Add($"@{fieldName}");
                        values.Add(Convert.ToDecimal(value));
                    }
                    else if (dbType == DbType.Object)
                    {
                        parametersForSQL.Add($"@{fieldName}");
                        values.Add(value);
                    }
                    else if (dbType == DbType.Guid)
                    {
                        Guid guidValue;

                        if (Guid.TryParse(value, out guidValue))
                        {
                            parametersForSQL.Add($"@{fieldName}");
                            values.Add(guidValue);
                        }
                        else
                        {
                            throw new Exception($" {fieldName} is not having a valid GUID. Current value {value}");
                        }
                    }
                    else
                    {
                        parametersForSQL.Add($"@{fieldName}");
                        values.Add(value);
                    }

                    parameters.Add($"@{fieldName}");
                    paramTypes.Add(dbType);

                }
            }

            if (string.IsNullOrEmpty(tableName) || columnNames.Count == 0 || parametersForSQL.Count == 0)
                return null;

            string sql = $"insert into {tableName} ({string.Join(",", columnNames)}) values ({string.Join(",", parametersForSQL)})";

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Insert SQL for {tableName}", $"{sql}");
                _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
                _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
                _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
            }

            return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.INSERT);
        }

        private SQLCommandBuilder GenerateUpdateStatement(AxPut putData, string primaryTableName, string tableName, long recordId, long rowRecordId, int rowNo, bool isGrid, Dictionary<string, string> rowData)
        {
            if (_common._milliSecsInTimeStamp)
                putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            else
                putData.ModifiedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var setClauses = new List<string>();
            var parameters = new List<string>();
            var values = new List<object>();
            var paramTypes = new List<DbType>();

            // Add common columns for main table (non-grid)
            if (!isGrid && primaryTableName == tableName)
            {
                setClauses.Add($"username = @username");

                if (_common._milliSecsInTimeStamp)
                    setClauses.Add($"modifiedon = {_common._dbVars["dbdatestringwithms"].Replace("$DATE$", "@modifiedon")}");
                else
                    setClauses.Add($"modifiedon = {_common._dbVars["dbdatestring"].Replace("$DATE$", "@modifiedon")}");

                setClauses.Add($"app_desc = @app_desc");
                setClauses.Add($"app_level = @app_level");

                parameters.AddRange(new[] { "@username", "@modifiedon", "@app_desc", "@app_level" });
                values.AddRange(new object[] { putData.UserName, putData.ModifiedOn, 1, 1 });
                paramTypes.AddRange(new[] { DbType.String, DbType.String, DbType.Int64, DbType.Int64 });
            }

            // Add user-defined fields dynamically
            foreach (var prop in rowData)
            {
                string fieldName = prop.Key;
                var fieldType = _validate._sdef.GetFieldDataType(fieldName);
                string value = prop.Value;

                if (fieldType.ToLower() == "i")
                {
                    ProcessImageUploads(fieldName, value);
                    continue;
                }
                else if (fieldName.ToLower().StartsWith("axpfile_"))
                {
                    value = ProcessAxpFileUploads(fieldName, value, rowNo, "UPDATE");
                }

                string paramName = $"@{fieldName}";
                var dbType = _validate._sdef.GetFieldDBType(fieldName);

                if (dbType == DbType.Date || dbType == DbType.DateTime)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        setClauses.Add($"{fieldName} = {paramName}");
                        values.Add(DBNull.Value);
                    }
                    else
                    {
                        if (_common._milliSecsInTimeStamp)
                            setClauses.Add($"{fieldName} = {_common._dbVars["dbdatestringwithms"].Replace("$DATE$", paramName)}");
                        else
                            setClauses.Add($"{fieldName} = {_common._dbVars["dbdatestring"].Replace("$DATE$", paramName)}");

                        values.Add(value);
                        dbType = DbType.String; // handled as string since date format string is passed
                    }

                }
                else if (dbType == DbType.Int64)
                {
                    setClauses.Add($"{fieldName} = {paramName}");
                    if (string.IsNullOrEmpty(value))
                        values.Add(0);
                    else
                        values.Add(Convert.ToInt64(value));
                }
                else if (dbType == DbType.Decimal)
                {
                    setClauses.Add($"{fieldName} = {paramName}");
                    if (string.IsNullOrEmpty(value))
                        values.Add(0);
                    else
                        values.Add(Convert.ToDecimal(value));
                }
                else if (dbType == DbType.Object)
                {
                    setClauses.Add($"{fieldName} = {paramName}");
                    if (string.IsNullOrEmpty(value))
                        values.Add(DBNull.Value);
                    else
                        values.Add(value);
                }
                else if (dbType == DbType.Guid)
                {
                    Guid guidValue;
                    setClauses.Add($"{fieldName} = {paramName}");
                    if (string.IsNullOrEmpty(value))
                        values.Add(DBNull.Value);
                    else
                    {
                        if (Guid.TryParse(value, out guidValue))
                            values.Add(guidValue);
                        else
                            throw new Exception($" {fieldName} is not having a valid GUID. Current value {value}");
                    }
                }
                else
                {
                    setClauses.Add($"{fieldName} = {paramName}");
                    values.Add(value);
                }

                parameters.Add(paramName);
                paramTypes.Add(dbType);
            }

            // Add row number if applicable (for grid rows)
            if (isGrid)
            {
                setClauses.Add($"{tableName.ToLower()}row = @rowno");
                parameters.Add("@rowno");
                values.Add(rowNo);
                paramTypes.Add(DbType.Int32);
            }

            // WHERE condition for record ID
            string whereClause = $"{tableName}id = @{tableName}id";
            parameters.Add($"@{tableName}id");
            values.Add(rowRecordId);
            paramTypes.Add(DbType.Int64);

            if (string.IsNullOrEmpty(tableName) || setClauses.Count == 0)
                return null;

            // Build SQL
            string sql = $"update {tableName} set {string.Join(", ", setClauses)} where {whereClause}";

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Update SQL for {tableName} : {sql}");
                _traceService.LogMessage($"Parameters", $"{JsonConvert.SerializeObject(parameters)}");
                _traceService.LogMessage($"Param Types", $"{JsonConvert.SerializeObject(paramTypes)}");
                _traceService.LogMessage($"Values", $"{JsonConvert.SerializeObject(values)}");
            }

            return new SQLCommandBuilder(sql, parameters.ToArray(), paramTypes.ToArray(), values.ToArray(), DATA_ACTION.UPDATE);
        }

        private SQLCommandBuilder GenerateDeleteStatement(AxPut putData, string tableName, long rowRecordId)
        {
            var parameters = new List<string>();
            var values = new List<object>();
            var paramTypes = new List<DbType>();

            // WHERE condition for the delete
            string whereClause = $"{tableName}id = @{tableName}id";

            parameters.Add($"@{tableName}id");
            values.Add(rowRecordId);
            paramTypes.Add(DbType.Int64);

            if (string.IsNullOrEmpty(tableName))
                return null;

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
        /*
       public Dictionary<string, TableWiseDCData> GetTableWiseDCDataForCreate()
       {
           if (_validate._sdef.Track_chng)
           {
               _track = true;
               _trackHistory = new TrackHistory();
           }

           Dictionary<string, TableWiseDCData> tableWiseDCData = new Dictionary<string, TableWiseDCData>();
           for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
           {
               var rowNo = 1;
               int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
               var currentFrame = _validate._sdef.Frames[fidx];

               var tableName = currentFrame.TableName;
               if (!tableWiseDCData.ContainsKey(tableName))
                   tableWiseDCData[tableName] = new TableWiseDCData();

               tableWiseDCData[tableName].DCs.Add($"DC{currentFrame.FrameNo}");
               tableWiseDCData[tableName].GridDc = currentFrame.AsGrid;

               Dictionary<string, Dictionary<string, string>> dcRowsData = new Dictionary<string, Dictionary<string, string>>();
               for (int idx = 1; idx <= totalRowCount; idx++)
               {
                   rowNo = idx;
                   int startFldIdx = currentFrame.StartIndex;
                   int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
                   Dictionary<string, string> fieldData = new Dictionary<string, string>();
                   for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                   {
                       TField field = _validate._sdef.Flds[fldIdx];
                       if (field.SaveValue)
                       {
                           if (field.SourceKey)
                               fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                           else
                               fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
                       }

                   }
                   dcRowsData[$"row{rowNo}"] = fieldData;
               }

               tableWiseDCData[tableName].RowsData.Add(dcRowsData);
           }

           if (_traceService.IsTracingEnabled())
           {
               foreach (var tableData in tableWiseDCData)
               {
                   _traceService.LogMessage($"Table wise data mapped:");
                   _traceService.LogMessage($"{tableData.Key} - {JsonConvert.SerializeObject(tableData.Value)}");
               }
           }

           return tableWiseDCData;
       }

       public Dictionary<string, TableWiseDCData> GetTableWiseDCDataForEdit(AxPut putData)
       {
           Dictionary<string, TableWiseDCData> tableWiseDCData = new Dictionary<string, TableWiseDCData>();
           for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
           {
               var rowNo = 1;
               var currentFrame = _validate._sdef.Frames[fidx];
               int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
               var tableName = currentFrame.TableName;
               if (tableWiseDCData.ContainsKey(tableName) || totalRowCount == 0)
                   continue;
               else
                   tableWiseDCData[tableName] = new TableWiseDCData();

               tableWiseDCData[tableName].DCs.Add($"DC{currentFrame.FrameNo}");
               tableWiseDCData[tableName].GridDc = currentFrame.AsGrid;

               List<TrackField> trackFields = new List<TrackField>();
               if (currentFrame.AsGrid)
               {
                   Dictionary<string, Dictionary<string, string>> dcRowsData = new Dictionary<string, Dictionary<string, string>>();

                   for (int idx = 1; idx <= totalRowCount; idx++)
                   {
                       rowNo = idx;
                       var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo);
                       rowAction = rowAction.ToUpper();
                       if (rowAction == "CREATE")
                       {
                           int startFldIdx = currentFrame.StartIndex;
                           int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
                           Dictionary<string, string> fieldData = new Dictionary<string, string>();
                           fieldData["axp_recid"] = "0";
                           fieldData["axrow_action"] = rowAction;
                           for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                           {
                               TField field = _validate._sdef.Flds[fldIdx];
                               if (field.SaveValue)
                               {
                                   if (field.SourceKey)
                                       fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                                   else
                                       fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
                               }

                           }

                           dcRowsData[$"row{rowNo}"] = fieldData;
                       }
                       else if (rowAction == "EDIT")
                       {
                           var changedFields = _validate._sdef.GetGridRowChangedFieldsAsList(currentFrame.FrameNo, rowNo);
                           if (changedFields == null || changedFields.Count == 0)
                           {
                               continue;
                           }

                           Dictionary<string, string> fieldData = new Dictionary<string, string>();
                           fieldData["axp_recid"] = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
                           fieldData["axrow_action"] = rowAction;
                           foreach (var changedField in changedFields)
                           {
                               if (changedField.SaveValue)
                               {
                                   if (changedField.SourceKey)
                                       fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
                                   else
                                       fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
                               }

                           }
                           dcRowsData[$"row{rowNo}"] = fieldData;
                       }
                       else if (rowAction == "DELETE")
                       {
                           Dictionary<string, string> fieldData = new Dictionary<string, string>();
                           fieldData["axp_recid"] = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
                           fieldData["axrow_action"] = rowAction;
                           dcRowsData[$"row{rowNo}"] = fieldData;
                       }

                   }
                   tableWiseDCData[tableName].RowsData.Add(dcRowsData);
               }
               else
               {
                   var changedFields = _validate._sdef.GetNGChangedFieldsAsList(tableName);
                   var primaryTable = _validate._sdef.Frames[0].TableName;

                   if ((changedFields == null || changedFields.Count == 0) && tableName != primaryTable)
                   {
                       tableWiseDCData.Remove(tableName);
                       continue;
                   }

                   Dictionary<string, Dictionary<string, string>> dcRowsData = new Dictionary<string, Dictionary<string, string>>();
                   Dictionary<string, string> fieldData = new Dictionary<string, string>();
                   var rowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, 1).ToString();
                   var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo).ToUpper();
                   foreach (var changedField in changedFields)
                   {
                       if (changedField.SaveValue)
                       {
                           if (changedField.SourceKey)
                               fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
                           else
                               fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
                       }     

                   }

                   dcRowsData[$"row1"] = fieldData;
                   tableWiseDCData[tableName].RowsData.Add(dcRowsData);

               }

           }

           if (_traceService.IsTracingEnabled())
           {
               foreach (var tableData in tableWiseDCData)
               {
                   _traceService.LogMessage($"Table wise data mapped:");
                   _traceService.LogMessage($"{tableData.Key} - {JsonConvert.SerializeObject(tableData.Value)}");
               }
           }

           return tableWiseDCData;
       }

       public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForCreate(AxPut putData)
       {
           if (_validate._sdef.Track_chng)
           {
               _track = true;
               _trackHistory = new TrackHistory();
           }

           var sqlCommands = new List<SQLCommandBuilder>();

           var tableWiseDCData = GetTableWiseDCDataForCreate();

           var primaryTableName = _validate._sdef.Frames[0].TableName;

           foreach (var tableData in tableWiseDCData)
           {
               if (!tableData.Value.GridDc)
               {
                   if (primaryTableName == tableData.Key)
                   {
                       Int64 tempRecId = 0;
                       var recordIdStr = await _repo.GetRecordIdFromDB(Convert.ToInt32(_siteNo), "F", 1);
                       Int64.TryParse(recordIdStr, out tempRecId);

                       putData.RecordId = tempRecId;

                       Dictionary<string, string> rowData = new Dictionary<string, string>();
                       foreach (var rowsData in tableData.Value.RowsData)
                       {
                           foreach (var tempRowData in rowsData)
                           {
                               foreach (var kvp in tempRowData.Value)
                               {
                                   rowData[kvp.Key] = kvp.Value;
                               }
                           }
                       }

                       var sqlCommand = GenerateInsertStatement(putData, primaryTableName, tableData.Key, putData.RecordId, 0, 0, false, rowData);
                       sqlCommands.Add(sqlCommand);
                   }
                   else
                   {
                       Int64 tempRecId = 0;
                       var recordIdStr = await _repo.GetRecordIdFromDB(Convert.ToInt32(_siteNo), "F", 1);
                       Int64.TryParse(recordIdStr, out tempRecId);

                       Dictionary<string, string> rowData = new Dictionary<string, string>();
                       foreach (var rowsData in tableData.Value.RowsData)
                       {
                           foreach (var tempRowData in rowsData)
                           {
                               foreach (var kvp in tempRowData.Value)
                               {
                                   rowData[kvp.Key] = kvp.Value;
                               }
                           }
                       }

                       var sqlCommand = GenerateInsertStatement(putData, primaryTableName, tableData.Key, putData.RecordId, tempRecId, 0, false, rowData);
                       sqlCommands.Add(sqlCommand);
                   }
               }
               else
               {
                   SQLResult recordIdsList = new SQLResult();

                   if (tableData.Value.RowsData.Count > 0)
                   {
                       recordIdsList = await _repo.GetRecordIdsFromDB(Convert.ToInt32(_siteNo), "T", tableData.Value.RowsData[0].Count);
                   }

                   int i = 0;
                   foreach (var rowsData in tableData.Value.RowsData)
                   {
                       foreach (var rowData in rowsData)
                       {
                           int rowNo = Convert.ToInt32(rowData.Key.Replace("row", ""));

                           Int64 gridRecId = 0;
                           var recordIdStr = recordIdsList.data.Rows[i][0].ToString();
                           Int64.TryParse(recordIdStr, out gridRecId);

                           var sqlCommand = GenerateInsertStatement(putData, primaryTableName, tableData.Key, putData.RecordId, Convert.ToInt64(gridRecId), rowNo, true, rowData.Value);
                           sqlCommands.Add(sqlCommand);
                           i++;
                       }

                   }
               }
           }
           return sqlCommands;
       }
       */

        public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForCreate(AxPut putData, bool isGenMapRecord = false)
        {
            if (_validate._sdef.Struct.TrackChange)
            {
                _track = true;
                _trackHistory = new TrackHistory();
            }

            var sqlCommands = new List<SQLCommandBuilder>();

            Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();
            var primaryTable = _validate._sdef.Frames?[0].TableName ?? "";
            for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
            {
                var rowNo = 1;
                var currentFrame = _validate._sdef.Frames[fidx];
                int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
                var tableName = currentFrame.TableName;
                var dcName = "dc" + currentFrame.FrameNo;
                if (tableWiseSQLCommands.ContainsKey(tableName) || (totalRowCount == 0 && tableName != primaryTable))
                    continue;
                else
                    tableWiseSQLCommands[tableName] = totalRowCount;

                List<TrackField> trackFields = new List<TrackField>();
                if (currentFrame.AsGrid)
                {
                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;

                        int startFldIdx = currentFrame.StartIndex;
                        int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
                        Dictionary<string, string> fieldData = new Dictionary<string, string>();

                        Int64 gridRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

                        for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                        {
                            TField field = _validate._sdef.Flds[fldIdx];
                            if (field.SaveValue)
                            {
                                if (field.SourceKey)
                                    fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                                else
                                    fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
                            }

                            if (_track)
                            {
                                var value = _validate._sdef.GetFieldValue(field, rowNo);
                                if (field.DataType == "d")
                                    value = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);

                                TrackField trackFld = new TrackField(field.FieldName, "", value, rowNo, field.FrameNo, gridRecId, (field.SourceKey ? Convert.ToInt64(fieldData[field.FieldName]) : 0), 0, "Create");
                                trackFields.Add(trackFld);
                            }

                        }

                        var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRecId), rowNo, true, fieldData);
                        if (sqlCommand != null)
                            sqlCommands.Add(sqlCommand);
                    }
                }
                else
                {
                    var changedFields = _validate._sdef.GetNGChangedFieldsAsList(tableName, "create");

                    Int64 rowRecId = 0;
                    if (primaryTable == tableName)
                    {
                        if (putData.RecordId == 0)
                        {
                            rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();
                            putData.RecordId = rowRecId;
                        }
                    }
                    else
                        rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

                    //if ((changedFields == null || changedFields.Count == 0) && tableName != primaryTable)
                    //{
                    //    tableWiseSQLCommands.Remove(tableName);
                    //    continue;
                    //}

                    Dictionary<string, string> fieldData = new Dictionary<string, string>();

                    foreach (var changedField in changedFields)
                    {
                        if (changedField.SaveValue)
                        {
                            if (changedField.SourceKey)
                                fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
                            else
                                fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
                        }

                        if (_track)
                        {
                            var value = _validate._sdef.GetFieldValue(changedField, rowNo);
                            if (changedField.DataType == "d")
                                value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);

                            TrackField trackFld = new TrackField(changedField.FieldName, "", value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), 0, "Create");
                            trackFields.Add(trackFld);

                        }
                    }

                    if (!string.IsNullOrEmpty(_validate._sdef.MapName))
                        fieldData.Add("mapname", _validate._sdef.MapName);

                    if (_validate._sdef.SourceId > 0)
                        fieldData.Add("sourceid", _validate._sdef.SourceId.ToString());

                    var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, rowNo, false, fieldData);
                    if (sqlCommand != null)
                        sqlCommands.Add(sqlCommand);
                }

                if (_track && trackFields.Count > 0)
                    _trackHistory.ChangedFields.AddRange(trackFields);
            }
            return sqlCommands;
        }

        public async Task<List<SQLCommandBuilder>> GetTableWiseSQLCommandsForEdit(AxPut putData, bool isGenMapRecord = false)
        {
            if (_validate._sdef.Struct.TrackChange)
            {
                _track = true;
                _trackHistory = new TrackHistory();
            }

            var sqlCommands = new List<SQLCommandBuilder>();

            Dictionary<string, int> tableWiseSQLCommands = new Dictionary<string, int>();
            var primaryTable = _validate._sdef.Frames?[0].TableName ?? "";
            for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
            {
                var rowNo = 1;
                var currentFrame = _validate._sdef.Frames[fidx];
                int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
                var tableName = currentFrame.TableName;
                var dcName = "dc" + currentFrame.FrameNo;
                if (tableWiseSQLCommands.ContainsKey(tableName) || (totalRowCount == 0 && tableName != primaryTable))
                    continue;
                else
                    tableWiseSQLCommands[tableName] = totalRowCount;

                List<TrackField> trackFields = new List<TrackField>();
                var deletedRows = new List<int>();
                if (currentFrame.AsGrid)
                {
                    if (_validate.deletedRowsDict.ContainsKey(currentFrame.FrameNo) && _validate.deletedRowsDict[currentFrame.FrameNo] == true)
                    {
                        for (int idx = 1; idx <= totalRowCount; idx++)
                        {
                            rowNo = idx;
                            var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo);
                            rowAction = rowAction.ToUpper();

                            if (rowAction == "DELETE")
                            {
                                Dictionary<string, string> fieldData = new Dictionary<string, string>();
                                var rowRecordId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
                                deletedRows.Add(rowNo);
                                var sqlCommand = GenerateDeleteStatement(putData, tableName, Convert.ToInt64(rowRecordId));
                                if (sqlCommand != null)
                                    sqlCommands.Add(sqlCommand);

                                if (_track)
                                {
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

                                            TrackField trackFld = new TrackField(deletedField.FieldName, oldValue, "", rowNo, deletedField.FrameNo, Convert.ToInt64(rowRecordId), (deletedField.SourceKey ? Convert.ToInt64(fieldData[deletedField.FieldName]) : 0), (deletedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(deletedField, rowNo)) : 0), rowAction);

                                            trackFields.Add(trackFld);
                                        }

                                    }

                                }
                            }

                        }
                    }

                    int tempRowNo = 1;
                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;

                        if (deletedRows.Contains(rowNo))
                            continue;

                        var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo);
                        rowAction = rowAction.ToUpper();

                        if (rowAction == "CREATE")
                        {
                            int startFldIdx = currentFrame.StartIndex;
                            int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
                            Dictionary<string, string> fieldData = new Dictionary<string, string>();

                            Int64 gridRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();

                            for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                            {
                                TField field = _validate._sdef.Flds[fldIdx];
                                if (field.SaveValue)
                                {
                                    if (field.SourceKey)
                                        fieldData[field.FieldName] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                                    else
                                        fieldData[field.FieldName] = _validate._sdef.GetFieldValue(field, rowNo);
                                }

                                if (_track)
                                {
                                    var value = _validate._sdef.GetFieldValue(field, rowNo);
                                    if (field.DataType == "d")
                                        value = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);

                                    TrackField trackFld = new TrackField(field.FieldName, "", value, tempRowNo, field.FrameNo, gridRecId, (field.SourceKey ? Convert.ToInt64(fieldData[field.FieldName]) : 0), 0, rowAction);
                                    trackFields.Add(trackFld);
                                }

                            }

                            var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRecId), tempRowNo, true, fieldData);
                            if (sqlCommand != null)
                                sqlCommands.Add(sqlCommand);
                        }
                        else if (rowAction == "EDIT")
                        {
                            var changedFields = _validate._sdef.GetGridRowChangedFieldsAsList(currentFrame.FrameNo, rowNo);
                            if (changedFields == null || changedFields.Count == 0)
                            {
                                if (tempRowNo != rowNo)
                                {
                                    var tempGridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
                                    var sqlCommandRowNoUpdate = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(tempGridRowRecId), tempRowNo, true, new Dictionary<string, string>());
                                    if (sqlCommandRowNoUpdate != null)
                                        sqlCommands.Add(sqlCommandRowNoUpdate);
                                }
                                tempRowNo++;
                                continue;
                            }

                            Dictionary<string, string> fieldData = new Dictionary<string, string>();
                            var gridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();

                            foreach (var changedField in changedFields)
                            {
                                if (changedField.SaveValue)
                                {
                                    if (changedField.SourceKey)
                                        fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
                                    else
                                        fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
                                }

                                if (_track)
                                {
                                    var oldValue = _validate._sdef.GetOldFieldValue(changedField, rowNo);
                                    var value = _validate._sdef.GetFieldValue(changedField, rowNo);
                                    if (changedField.DataType == "d")
                                    {
                                        oldValue = _common.ConvertDBDateToClientDateFormat(changedField.CustType, oldValue, _validate._sdef.clientDateFormat);
                                        value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);
                                    }

                                    TrackField trackFld = new TrackField(changedField.FieldName, oldValue, value, tempRowNo, changedField.FrameNo, Convert.ToInt64(gridRowRecId), (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), (changedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(changedField, rowNo)) : 0), rowAction);

                                    trackFields.Add(trackFld);
                                }

                            }
                            var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRowRecId), tempRowNo, true, fieldData);
                            if (sqlCommand != null)
                                sqlCommands.Add(sqlCommand);

                        }
                        else if (rowAction == "" && tempRowNo != rowNo)
                        {
                            var gridRowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, rowNo).ToString();
                            var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, Convert.ToInt64(gridRowRecId), tempRowNo, true, new Dictionary<string, string>());
                            if (sqlCommand != null)
                                sqlCommands.Add(sqlCommand);
                        }

                        tempRowNo++;
                    }
                }
                else
                {
                    var changedFields = _validate._sdef.GetNGChangedFieldsAsList(tableName, "edit");
                    Int64 rowRecId = _validate._sdef.GetRecordId(currentFrame.FrameNo, 1);
                    var rowAction = _validate._sdef.GetRowAction(currentFrame.FrameNo, rowNo).ToUpper();

                    if (tableName == primaryTable && (rowAction == "DELETE" || rowAction == "CREATE"))
                        throw new Exception($"DC rows using same table as Header DC can't be {rowAction.ToLower()}d.");

                    if (fidx == 0 && string.IsNullOrEmpty(rowAction))
                        rowAction = "EDIT";

                    if (rowAction == "DELETE")
                    {
                        var sqlCommand = GenerateDeleteStatement(putData, tableName, rowRecId);
                        if (sqlCommand != null)
                            sqlCommands.Add(sqlCommand);

                        if (_track)
                        {
                            Dictionary<string, string> trkFieldData = new Dictionary<string, string>();
                            var deletedFields = _validate._sdef.GetRowFieldsAsList(currentFrame.FrameNo, 1);

                            foreach (var deletedField in deletedFields)
                            {
                                if (deletedField.SaveValue)
                                {
                                    if (deletedField.SourceKey)
                                        trkFieldData[deletedField.FieldName] = _validate._sdef.GetIDValue(deletedField, 1).ToString();
                                    else
                                        trkFieldData[deletedField.FieldName] = _validate._sdef.GetFieldValue(deletedField, 1);
                                }

                                var oldValue = _validate._sdef.GetOldFieldValue(deletedField, 1);
                                if (deletedField.DataType == "d")
                                {
                                    oldValue = _common.ConvertDBDateToClientDateFormat(deletedField.CustType, oldValue, _validate._sdef.clientDateFormat);
                                }

                                TrackField trackFld = new TrackField(deletedField.FieldName, oldValue, "", 1, deletedField.FrameNo, rowRecId, (deletedField.SourceKey ? Convert.ToInt64(trkFieldData[deletedField.FieldName]) : 0), (deletedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(deletedField, 1)) : 0), rowAction);

                                trackFields.Add(trackFld);
                            }
                        }
                    }
                    else
                    {
                        //if ((changedFields == null || changedFields.Count == 0) && tableName != primaryTable)
                        //{
                        //    tableWiseSQLCommands.Remove(tableName);
                        //    continue;
                        //}

                        Dictionary<string, string> fieldData = new Dictionary<string, string>();
                        if (rowAction == "CREATE")
                        {
                            rowRecId = isGenMapRecord ? 0 : GetRecordIdForCreate();
                        }

                        foreach (var changedField in changedFields)
                        {
                            if (changedField.SaveValue)
                            {
                                if (changedField.SourceKey)
                                    fieldData[changedField.FieldName] = _validate._sdef.GetIDValue(changedField, rowNo).ToString();
                                else
                                    fieldData[changedField.FieldName] = _validate._sdef.GetFieldValue(changedField, rowNo);
                            }

                            if (_track)
                            {
                                var value = _validate._sdef.GetFieldValue(changedField, rowNo);
                                if (changedField.DataType == "d")
                                    value = _common.ConvertDBDateToClientDateFormat(changedField.CustType, value, _validate._sdef.clientDateFormat);

                                if (rowAction == "CREATE")
                                {
                                    TrackField trackFld = new TrackField(changedField.FieldName, "", value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), 0, rowAction);
                                    trackFields.Add(trackFld);
                                }
                                else if (rowAction == "EDIT")
                                {
                                    var oldValue = _validate._sdef.GetOldFieldValue(changedField, rowNo);
                                    if (changedField.DataType == "d")
                                        oldValue = _common.ConvertDBDateToClientDateFormat(changedField.CustType, oldValue, _validate._sdef.clientDateFormat);

                                    TrackField trackFld = new TrackField(changedField.FieldName, oldValue, value, rowNo, changedField.FrameNo, rowRecId, (changedField.SourceKey ? Convert.ToInt64(fieldData[changedField.FieldName]) : 0), (changedField.SourceKey ? Convert.ToInt64(_validate._sdef.GetOldIDValue(changedField, rowNo)) : 0), rowAction);
                                    trackFields.Add(trackFld);
                                }

                            }
                        }

                        if (rowAction == "CREATE")
                        {
                            var sqlCommand = GenerateInsertStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, rowNo, false, fieldData);
                            if (sqlCommand != null)
                                sqlCommands.Add(sqlCommand);
                        }
                        else if (rowAction == "EDIT")
                        {
                            var sqlCommand = GenerateUpdateStatement(putData, primaryTable, tableName, putData.RecordId, rowRecId, 0, false, fieldData);
                            if (sqlCommand != null)
                                sqlCommands.Add(sqlCommand);
                        }
                    }
                }

                if (_track && trackFields.Count > 0)
                    _trackHistory.ChangedFields.AddRange(trackFields);

            }

            return sqlCommands;
        }

        private async Task<TstructDataValidationResult> ValidateUserPermissionsAccessForEdit(AxPut putData, APIResult accessResult)
        {
            var result = new TstructDataValidationResult();

            var excludedDCs = (List<string>)accessResult.data["ExcludedDCs"];
            var includedDCs = (List<string>)accessResult.data["IncludedDCs"];

            var excludedFields = (List<string>)accessResult.data["ExcludedFields"];
            var includedFields = (List<string>)accessResult.data["IncludedFields"];

            var fieldBasedDCs = _validate._sdef.FieldWiseDCs.Where(x => includedFields.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).Select(x => x.Value).ToList();

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            List<string> skipFields = ["axp_recid", "axrow_action"];

            foreach (var dc in putData.SubmitData)
            {
                string dcKey = dc.Key;

                // Validate Excluded DCs
                if (!excludedDCs.Contains(none) && excludedDCs.Contains(dcKey, StringComparer.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"DC '{dcKey}' is not allowed (excluded).");
                }

                // Validate Included DCs
                if (!includedDCs.Contains(all) && !includedDCs.Contains(dcKey, StringComparer.OrdinalIgnoreCase) && !fieldBasedDCs.Contains(dcKey, StringComparer.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"DC '{dcKey}' is not in the list of included DCs.");
                }

                foreach (var rowData in dc.Value)
                {
                    foreach (var fieldData in rowData.Value)
                    {
                        if (skipFields.Contains(fieldData.Key, StringComparer.OrdinalIgnoreCase))
                            continue;

                        List<string> tempIncludedFields = new List<string>();

                        if (includedDCs.Contains(dc.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            tempIncludedFields.Add(all);
                        }
                        else
                        {
                            tempIncludedFields.AddRange(new List<string>(includedFields));
                        }


                        await ValidateField(result, fieldData.Key, excludedFields, tempIncludedFields, dcKey, rowData.Key);
                    }
                }
            }

            return result;
        }

        private async Task<List<Int64>> GetNewRecordIds(int newRowsCount)
        {
            if (!_validateOnly)
            {
                var recIdsResult = await _validate._sdef.GetRecordIdsFromDB(newRowsCount);

                if (!string.IsNullOrEmpty(recIdsResult.error))
                    throw new Exception($"Error fetching new Record Ids: {recIdsResult.error}");

                return recIdsResult.data.AsEnumerable()
                         .Where(row => row["recordid"] != DBNull.Value)
                         .Select(row => Convert.ToInt64(row["recordid"]))
                         .ToList();
            }
            else
            {
                return new List<Int64> { _recordIdGenerator.GetTemporaryRecordId() };
            }
        }

        public Int64 GetRecordIdForCreate()
        {
            if (_recordIds == null || _recordIds.Count == 0)
            {
                _recordIds = GetNewRecordIds(1).Result;
            }

            Int64 id = _recordIds[0];
            _recordIds.RemoveAt(0);
            return id;
        }

        private int GetNewRowsCount(AxPutInput input)
        {
            int count = 0;

            foreach (var axPut in input.Data)
            {
                if (axPut.Action.Equals("create", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dc in axPut.SubmitData)
                    {
                        count += dc.Value.Count;
                    }
                }
                else if (axPut.Action.Equals("edit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dc in axPut.SubmitData)
                    {
                        foreach (var row in dc.Value)
                        {
                            if (row.Value.TryGetValue("axrow_action", out string actionFlag) &&
                                actionFlag.Equals("create", StringComparison.OrdinalIgnoreCase))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private async Task ValidateField(TstructDataValidationResult result, string fieldName, List<string> excludedFields, List<string> includedFields, string dcKey, string rowKey = null)
        {
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            string location = rowKey != null ? $"DC '{dcKey}' -> Row '{rowKey}'" : $"DC '{dcKey}'";

            if (!excludedFields.Contains(none) && excludedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"{location} contains excluded field '{fieldName}'.");
            }

            if (!includedFields.Contains(all) && !includedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"{location} contains field '{fieldName}' which is not in the list of included fields.");
            }
        }

        private async Task LogAxPutDetails(AxPutInput input, APIResult response)
        {
            string callDetails = "";
            try
            {
                var groups = _saveDataRecordIds.Select(kvp =>
                    $"{kvp.Key}={string.Join(",", kvp.Value)}"
                );
                callDetails = string.Join("~", groups);
            }
            catch (Exception ex)
            {
                callDetails = ex.Message;
            }

            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = response.success ? "success" : "error";
            _axpertLog.CallDetails = response.success ? callDetails : response.error; ;
            _axpertLog.ServiceName = "ARM - AxPut API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }

        private string ProcessAxpFileUploads(string fieldName, string value, Int32 rowNo, string action = "")
        {
            var axpFilesObj = JsonConvert.DeserializeObject<List<AxAttachments>>(value);
            var oldFileNames = "";
            if (action == "UPDATE")
                oldFileNames = _validate._sdef.GetOldFieldValue(fieldName, rowNo);

            var oldFileNamesSet = new HashSet<string>(
    oldFileNames
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x)),
    StringComparer.OrdinalIgnoreCase);

            foreach (var axpFileObj in axpFilesObj)
            {
                var tempFilePath = _validate._sdef.GetFieldValue(fieldName.Replace("axpfile_", "axpfilepath_"), rowNo).ToString();

                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    string timestamp = "";
                    if (tempFilePath.EndsWith("*"))
                    {
                        timestamp = DateTime.Now.ToString("ddMMyyyyHHmmssfffffff");
                        tempFilePath = tempFilePath.TrimEnd('*');
                    }

                    if (!axpFileObj.Delete)
                        axpFileObj.FileName = timestamp + axpFileObj.FileName;

                    axpFileObj.FilePath = tempFilePath;
                }

                axpFileObj.FieldName = fieldName;
                _axpFilesList.Add(axpFileObj);

                if (axpFileObj.Delete)
                {
                    if (oldFileNamesSet.Contains(axpFileObj.FileName))
                        oldFileNamesSet.Remove(axpFileObj.FileName);
                }
                else
                {
                    oldFileNamesSet.Add(axpFileObj.FileName);
                }
            }

            if (!string.IsNullOrEmpty(_validate._sdef.FileUploadLimit))
            {
                int filesUploadLimit = 0;
                Int32.TryParse(_validate._sdef.FileUploadLimit, out filesUploadLimit);

                if (filesUploadLimit > 0 && oldFileNamesSet.Count > filesUploadLimit)
                {
                    throw new Exception($"File upload limit of {filesUploadLimit} is exceeded in {fieldName}.");
                }
            }

            return string.Join(",", oldFileNamesSet);
        }

        private void ProcessImageUploads(string fieldName, string value)
        {
            var imageObj = JsonConvert.DeserializeObject<AxAttachments>(value);
            imageObj.FieldName = fieldName;
            _imagesList.Add(imageObj);
        }

        /// <summary>
        /// Reconciles missing record IDs across submit data.
        /// </summary>
        /// <param name="putData">The submitted payload.</param>
        /// <param name="forceReconciliation">
        /// Optional override: 
        /// - null: Automatically detects if reconciliation is needed by checking the first row.
        /// - true: Forces reconciliation across all rows.
        /// - false: Bypasses reconciliation entirely.
        /// </param>
        private void ReconcileSubmittedRecordIds(AxPut putData, bool? forceReconciliation = null)
        {
            if (putData?.SubmitData == null || putData.SubmitData.Count == 0) return;

            // 1. Determine if reconciliation is required
            bool shouldReconcile = forceReconciliation ?? ShouldReconcile(putData.SubmitData);
            if (!shouldReconcile) return;

            // 2. Perform full reconciliation if required
            foreach (var (dcKey, rows) in putData.SubmitData)
            {
                if (!dcKey.StartsWith("dc", StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(dcKey.AsSpan(2), out int dcNo))
                {
                    continue;
                }

                foreach (var (rowKey, row) in rows)
                {
                    if (!rowKey.StartsWith("row", StringComparison.OrdinalIgnoreCase) ||
                        !int.TryParse(rowKey.AsSpan(3), out int rowNo))
                    {
                        continue;
                    }

                    if (!row.TryGetValue("axrow_action", out var action)) continue;

                    bool isCreate = string.Equals(action, "create", StringComparison.OrdinalIgnoreCase);
                    bool isEditOrDelete = string.Equals(action, "edit", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase);

                    if (isCreate)
                    {
                        row["axp_recid"] = "0";
                    }
                    else if (isEditOrDelete)
                    {
                        //Note: Condition may not needed since handled in ShouldReconcile method
                        row.TryGetValue("axp_recid", out var existingRecId);

                        if (string.IsNullOrWhiteSpace(existingRecId) || existingRecId == "0")
                        {
                            var recId = _validate._sdef.GetRecordId(dcNo, rowNo);
                            if (recId != 0)
                            {
                                row["axp_recid"] = recId.ToString();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inspects the first row in the submit data to decide if reconciliation is needed.
        /// </summary>
        private static bool ShouldReconcile(Dictionary<string, Dictionary<string, Dictionary<string, string>>> submitData)
        {
            // Grab the first available DC and its first row
            var firstDc = submitData.Values.FirstOrDefault();
            if (firstDc == null) return false;

            var firstRow = firstDc.Values.FirstOrDefault();
            if (firstRow == null) return false;

            // Check action
            if (!firstRow.TryGetValue("axrow_action", out var action)) return false;

            bool isCreate = string.Equals(action, "create", StringComparison.OrdinalIgnoreCase);
            bool isEditOrDelete = string.Equals(action, "edit", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase);

            // If create, check if axp_recid is already normalized to "0"
            if (isCreate)
            {
                bool isNormalized = firstRow.TryGetValue("axp_recid", out var recId) && recId == "0";
                return !isNormalized; // If not normalized, needs reconciliation
            }

            // If edit/delete, check if a valid non-zero ID is present
            if (isEditOrDelete)
            {
                bool hasValidRecId = firstRow.TryGetValue("axp_recid", out var recId) &&
                                     !string.IsNullOrWhiteSpace(recId) &&
                                     recId != "0";

                return !hasValidRecId; // If missing/invalid ID, needs reconciliation
            }

            // Unrecognized or neutral action -> No reconciliation needed
            return false;
        }

        //private async Task RestoreSessionGlobals(AxParser parser)
        //{
        //    if (_common._globalVars == null || _common._globalVars.Count == 0)
        //        return;

        //    foreach (var globalVar in _common._globalVars)
        //    {
        //        // Metadata used to determine variable types; it is not a parser variable.
        //        if (globalVar.Key.Equals("appvartypes",
        //                StringComparison.OrdinalIgnoreCase))
        //        {
        //            continue;
        //        }

        //        var variableType =
        //            await _common.GetGlobalVariableType(globalVar.Key);

        //        parser.RegisterVar(
        //            globalVar.Key,
        //            string.IsNullOrEmpty(variableType) ? "c" : variableType,
        //            globalVar.Value ?? string.Empty);
        //    }
        //}

        //private async Task PreserveReconstructedParser(
        //    AxParser parser,
        //    string armSessionId)
        //{
        //    var common = parser._common;
        //    var structDef = parser._structDef;

        //    try
        //    {
        //        // These runtime dependencies should not be serialized.
        //        parser._common = null;
        //        parser._structDef = null;

        //        // Keeping this value also prevents another cache read/reconstruction
        //        // during the current AxPut request.
        //        _parserJson = JsonConvert.SerializeObject(parser);

        //        var remainingExpiry =
        //            await _common.GetRemainingKeyExpirySeconds(armSessionId);

        //        // Never create a persistent parser key or extend it beyond the session.
        //        if (remainingExpiry > 0)
        //        {
        //            await _common.StoreAxParserObjInCache(
        //                armSessionId,
        //                _parserJson,
        //                remainingExpiry);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Re-caching is an optimization and should not fail AxPut.
        //        if (_traceService.IsTracingEnabled())
        //        {
        //            _traceService.LogError(
        //                $"Unable to cache reconstructed parser: {ex.Message}");
        //        }
        //    }
        //    finally
        //    {
        //        parser._common = common;
        //        parser._structDef = structDef;
        //    }
        //}
    
    }

    public class RecordIdGenerator
    {
        private long _current = 999900000000000;  // 15-digit start
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


