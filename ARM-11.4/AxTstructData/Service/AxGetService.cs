using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;
using static ARMCommon.Helpers.Constants;
using AxStructDef;
using AxValidate;
using AxParserApp;
using AxStructDef.Models;
using System.Globalization;


namespace AxTstructData
{
    public class AxGetService : IAxGetService
    {
        private readonly IAxGetRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private AxpertLog _axpertLog = new AxpertLog();
        private bool isTracing;
        private readonly IAxAttachmentsService _attachmentsService;
        private Validate _validate;
        private readonly IAxParserProvider _parserProvider;

        public AxGetService(IAxGetRepository repo, Utils common, IARMLogTraceService traceService, IAxAttachmentsService attachmentsService, IAxParserProvider parserProvider) 
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;          
            _validate = new Validate(_common, _traceService);
            _attachmentsService = attachmentsService;
            _parserProvider = parserProvider;
        }

        public async Task<APIResult> AxGet(AxGet getData)
        {
            var result = new APIResult();
            var isValidSession = await _common.ValidateSession(getData.ARMSessionId);
            if (!isValidSession)
            {
                result.success = false;
                result.data["error"] = $"SessionId is not valid";
                return result;
            }
            try
            {
                _axpertLog.CalledOn = DateTime.Now;

                isTracing = getData.Trace == true;                
                _traceService.EnableTracing(isTracing);
                
                if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getData)}");

                var validRoles = await _common.IsValidRole(getData.ARMSessionId, getData.Roles, _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

                if (!validRoles)
                {
                    result.success = false;
                    result.error = $"User does not access to some of the roles.";
                }
                else
                {
                    getData.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                    getData.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                    getData.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();

                    if (getData.Roles.Count == 0)
                    {
                        getData.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString().Split(",").ToList();
                    }

                    if (getData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
                    {
                        result = await GetTstructLoadData(getData);
                    }
                    else
                    {
                        result.success = false;
                        result.error = $"Invalid action - {getData.Action.ToUpper()}.";
                    }
                }                
            }
            catch (Exception ex)
            {
                result = new APIResult();
                result.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                result.error = ex.Message;

            }
            finally
            {
                await LogAxGetDetails(getData, result);
                await _common.CloseDBConnectionAsync();
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                result.data["trace"] = await _traceService.GetTraceFile($"AxGet-{getData.TransId}");
            }

            return result;

        }

        private async Task<APIResult> GetTstructLoadData(AxGet getData)
        {

            try
            {
                getData.Dimensions = _common.GetAxgApplicableDimensionsString();
                var accessResult = await CheckTstructViewAccess(getData);

                if (!string.IsNullOrEmpty(accessResult.error))
                {
                    accessResult.data["transid"] = getData.TransId;
                    return accessResult;
                }
                else
                {
                    if (isTracing)
                    {
                        _traceService.LogMessage($"CheckTstructViewAccess Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                    }

                    getData.RecordId = Convert.ToInt64(accessResult.data["RecordId"]?.ToString() ?? "0");

                    return await LoadData(getData, accessResult);
                }
            }
            catch (Exception ex)
            {
                var result = new APIResult(false, ex.Message);

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(result.error);
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                return result;
            }
        }

        public async Task<APIResult> CheckTstructViewAccess(AxGet getData)
        {
            var result = new APIResult();

            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            List<string> encryptedFields = new List<string>();

            var userPermissions = await _repo.GetTstructViewPermissionFromDB(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            if (userPermissions != null && userPermissions.data != null && userPermissions.data.Rows.Count == 1)
            {
                if (userPermissions.data.Rows[0]["recordid"]?.ToString() == string.Empty || userPermissions.data.Rows[0]["recordid"]?.ToString() == "0")
                {
                    result.success = false;
                    result.error = "No record found for the specified keyvalue. Please verify the keyvalue and try again.";
                    return result;
                }
            }
            else //If more than one record comes, return error message
            {
                result.success = false;
                result.error = "Multiple records found for the given keyvalue. Please enter a unique value and try again.";
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

            bool canView = false;
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            var recordid = "";
            foreach (DataRow dr in userPermissions.data.Rows)
            {
                var view = dr["view_access"]?.ToString() ?? "";
                var fullControl = dr["fullcontrol"]?.ToString() ?? "";

                if ((!string.IsNullOrEmpty(fullControl) && fullControl.ToUpper() == "T") || (string.IsNullOrEmpty(fullControl) && (string.IsNullOrEmpty(view) || (!string.IsNullOrEmpty(view) && view.ToUpper() != "NONE"))))
                {
                    canView = true;
                    var excludeDCs = dr["view_excludedc"].ToString() ?? none;
                    var includeDCs = dr["view_includedc"].ToString() ?? all;
                    var excludeElems = dr["view_excludeflds"].ToString() ?? none;
                    var includeElems = dr["view_includeflds"].ToString() ?? all;

                    if (string.IsNullOrEmpty(recordid))
                        recordid = dr["recordid"].ToString();


                    var maskedElems = dr["maskedflds"].ToString();
                    if (!string.IsNullOrEmpty(maskedElems))
                    {
                        var maskedFieldsList = maskedElems
                                .Split(',')
                                .Select(field => field.Trim()) // Trim any leading/trailing whitespace
                                .ToList();

                        foreach (var item in maskedFieldsList)
                        {
                            maskedFields.Add(item.ToString());
                        }
                    }

                    var encryptedFlds = dr["encryptedflds"].ToString() ?? "";
                    if (!string.IsNullOrEmpty(encryptedFlds))
                    {
                        var encryptedFldsList = encryptedFlds
                                .Split(',')
                                .Select(field => field.Trim()) // Trim any leading/trailing whitespace
                                .ToList();

                        foreach (var item in encryptedFldsList)
                        {
                            encryptedFields.Add(item.ToString());
                        }
                    }


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

            if (!canView)
            {
                result.success = false;
                result.error = "User does not have permissions to view this record.";
                if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                return result;
            }

            if (string.IsNullOrEmpty(recordid))
            {
                result.success = false;
                result.error = "Not able to load record. Matching recordid is not available.";
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
                _traceService.LogMessage($"Masked Fields - {string.Join(",", maskedFields)}");
                _traceService.LogMessage($"Encrypted Fields - {string.Join(",", encryptedFields)}");
            }

            result.data["ExcludedDCs"] = string.Join(",", excludedDCElements.Distinct().ToList<string>());
            result.data["IncludedDCs"] = string.Join(",", includeDCElements.Distinct().ToList<string>());
            result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
            result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
            result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
            result.data["RecordId"] = recordid;
            result.data["EncryptedFields"] = string.Join(",", encryptedFields.Distinct().ToList<string>());

            return result;
        }

        private async Task<APIResult> LoadStructure(AxGet getData)
        {
            _validate.TraceService = _traceService;
            _validate._sdef = await StructDef.LoadStructure(_common, getData.TransId, getData.Roles, _traceService);

            if (_validate._sdef is null)
            {
                return new APIResult(false, "Error in Tstruct Load Structure. StructDef Not Initialized");
            }

            //AxParser parser;
            //string parserJson = await _common.GetAxParserObjFromCache(getData.ARMSessionId);
            //if (string.IsNullOrEmpty(parserJson))
            //    parser = new AxParser(_common, _validate._sdef); //TODO: Non signin case.
            //else
            //{
            //    parser = JsonConvert.DeserializeObject<AxParser>(parserJson);
            //    parser._common = _common;
            //    parser._structDef = _validate._sdef;
            //}

            var parser = await _parserProvider.GetParserAsync(getData.ARMSessionId, _validate._sdef);

            _validate._common = _common;
            _validate._parser = parser;

            _validate._parser.RegisterVar("recordid", "n", getData.RecordId.ToString());
            _validate._sdef.LastSavedRecordid = Convert.ToInt64(getData.RecordId);
            //_validate._sdef.TraceService = _traceService;

            if (!string.IsNullOrEmpty(getData.AxClient_DateFormat))
                _validate._sdef.clientDateFormat = getData.AxClient_DateFormat;
            else if (!string.IsNullOrEmpty(_validate._parser.GetVarValue("axclient_dateformat")))
                _validate._sdef.clientDateFormat = _validate._parser.GetVarValue("axclient_dateformat");

            if (isTracing)
            {
                _traceService.LogMessage($"Client date format from AxGet Input Json: {getData.AxClient_DateFormat}");
                _traceService.LogMessage($"Client date format from Global Vars: {_validate._parser.GetVarValue("axclient_dateformat")}");
                _traceService.LogMessage($"Final client date format: {_validate._sdef.clientDateFormat}");
            }

            bool isValid = await _validate.InitValidateAsync(getData.TransId);

            if (!isValid)
            {
                return new APIResult(false, "Error in Tstruct validate init. StructDef Not Initialized");
            }

            return new APIResult(true, "Structure loaded successfully.");
        }

        public async Task<APIResult> LoadData(AxGet getData, APIResult accessResult = null, Validate validate = null)
        {
            var result = new APIResult();

            try
            {
                if (validate != null)
                {
                    _validate = validate;
                }
                else
                {
                    var loadStructureResult = await LoadStructure(getData);

                    if (!loadStructureResult.success)
                    {
                        result = new APIResult(false, loadStructureResult.error);
                        return result;
                    }
                }

                _common._milliSecsInTimeStamp = getData.MilliSecsInTimeStamp;
                var dcwiseData = new Dictionary<string, DataTable>();
                var dcwiseError = new Dictionary<string, string>();
                var recordId = (Int64)getData.RecordId;
                foreach (var dcFrame in _validate._sdef.Frames)
                {
                    var dcName = "dc" + dcFrame.FrameNo.ToString();

                    bool hasMatch = dcFrame.DcSQLGroup.Any(dc => dcwiseData.ContainsKey(dc));
                    if (!hasMatch)
                    {

                        var dcData = await _repo.ExecuteDCWiseSQLs(recordId, dcFrame.DcSQL);
                        if (dcData.data != null && dcData.data.Rows.Count > 0)
                        {
                            foreach (string dcNo in dcFrame.DcSQLGroup)
                            {
                                dcwiseData.Add(dcNo, dcData.data);
                            }
                        }
                        else if (!string.IsNullOrEmpty(dcData.error))
                        {
                            foreach (string dcNo in dcFrame.DcSQLGroup)
                            {
                                dcwiseError.Add(dcNo, dcData.error);
                                break;
                            }
                        }
                        else
                        {
                            foreach (string dcNo in dcFrame.DcSQLGroup)
                            {
                                dcwiseData.Add(dcNo, null);
                            }
                        }
                    }

                    if (dcwiseData.ContainsKey(dcName) && dcwiseData[dcName] != null)
                    {
                        var dcFields = _validate._sdef.GetDCFieldObjsAsList(dcFrame.FrameNo); //ToDo
                        int rowNo = 1;
                        foreach (DataRow dr in dcwiseData[dcName].Rows)
                        {
                            var recIdColumn = dcFrame.TableName + "id";
                            var rowNoColumn = dcFrame.TableName + "row";
                            var rowRecId = Convert.ToInt64(dr[recIdColumn]?.ToString() ?? "0");

                            _validate._sdef.SubmitRecordId(dcFrame.FrameNo, rowNo, rowRecId);

                            foreach (var dcField in dcFields)
                            {
                                string fldColName = dcField.FieldName.ToLower();
                                string fldIDColName = $"{dcField.FieldName}_sourceid".ToLower();

                                string fldVal = "";
                                string fldIdVal = "";

                                if (dr.Table.Columns.Contains(fldColName) && dr[fldColName] != DBNull.Value)
                                {
                                    Type colType = dr.Table.Columns[fldColName].DataType;


                                    if (dcField.DataType.ToUpper() == "D")
                                    {
                                        if (dcField.CustType.ToUpper() == "DATE")
                                        {
                                            DateTime dateValue = (DateTime)dr[fldColName];
                                            fldVal = dateValue.ToString(_validate._sdef.clientDateFormat, CultureInfo.InvariantCulture);
                                        }
                                        else if (dcField.CustType.ToUpper() == "TIMESTAMP")
                                        {
                                            DateTime dtoValue = (DateTime)dr[fldColName];
                                            if(_common._milliSecsInTimeStamp)
                                                fldVal = dtoValue.ToString($"{_validate._sdef.clientDateFormat} HH:mm:ss.fff", CultureInfo.InvariantCulture);
                                            else
                                                fldVal = dtoValue.ToString($"{_validate._sdef.clientDateFormat} HH:mm:ss", CultureInfo.InvariantCulture);
                                        }
                                        else
                                        {
                                            fldVal = dr[fldColName]?.ToString() ?? "";
                                        }
                                    }
                                    else
                                    {
                                        fldVal = dr[fldColName]?.ToString() ?? "";
                                    }

                                    if (dr.Table.Columns.Contains(fldIDColName) && dr[fldIDColName] != DBNull.Value)
                                    {
                                        fldIdVal = dr[fldIDColName]?.ToString() ?? "";
                                    }
                                }

                                Int64 idVal = 0;
                                Int64.TryParse(fldIdVal, out idVal);

                                _validate._sdef.SubmitValueOnLoad(dcField.FieldName, rowNo, fldVal, idVal);

                                //_validate._parser.RegisterVar(dcField.FieldName, "c", fldVal);
                                _validate._parser.RegisterVar(dcField.FieldName, dcField.DataType.ToString(), fldVal);

                            }

                            if (dcFrame.AsGrid)
                                rowNo++;
                            else
                                break;
                        }
                    }
                }

                if (dcwiseError.Count > 0)
                {
                    result = new APIResult(false, "Error in Data Load.");
                    result.data["errors"] = dcwiseError;
                    return result;
                }

                var dataJson = await _validate.RefreshFieldsOnLoad();

                result.success = true;
                result.data["message"] = "Data loaded successfully.";
                result.data["recordid"] = recordId;
                result.data["data"] = null;
                
                if(dcwiseData.ContainsKey("dc1") && dcwiseData["dc1"] != null && dcwiseData["dc1"]?.Rows.Count > 0)
                {
                    result.data["createdby"] = dcwiseData["dc1"]?.Rows[0]["createdby"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(dcwiseData["dc1"]?.Rows[0]["createdon"]?.ToString()))
                        result.data["createdon"] = "";
                    else
                    {
                        DateTime dateValue = (DateTime)dcwiseData["dc1"]?.Rows[0]["createdon"];
                        result.data["createdon"] = dateValue.ToString($"{_validate._sdef.clientDateFormat} HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    }

                    result.data["modifiedby"] = dcwiseData["dc1"]?.Rows[0]["username"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(dcwiseData["dc1"]?.Rows[0]["modifiedon"]?.ToString()))
                        result.data["modifiedon"] = "";
                    else
                    {
                        DateTime dateValue = (DateTime)dcwiseData["dc1"]?.Rows[0]["modifiedon"];
                        result.data["modifiedon"] = dateValue.ToString($"{_validate._sdef.clientDateFormat} HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    }

                    _validate._sdef.CancelledTrans = dcwiseData["dc1"]?.Rows[0]["cancel"]?.ToString().ToLower() == "t" ? true : false;
                    if (_validate._sdef.CancelledTrans)
                    {
                        _validate._sdef.CancelRemarks = "This transaction has been cancelled. "
                            + dcwiseData["dc1"]?.Rows[0]["cancelremarks"]?.ToString()
                            + " by "
                            + result.data["modifiedby"] + " on " + result.data["modifiedon"];
                        result.data["cancel"] = "T";
                        result.data["cancelremarks"] = _validate._sdef.CancelRemarks;
                    }


                }

                if (getData.CalledFrom != "SAVEDATA")
                {
                    if (_validate._sdef.Struct != null && _validate._sdef.Struct.Attach)
                    {
                        var imageObj = new AxAttachments();
                        imageObj.TransId = getData.TransId;
                        imageObj.RecordId = recordId;
                        imageObj.AttachmentType = "HEADERATTACHMENTS";

                        var headerAttachmentsList = _attachmentsService.GetAttachments(imageObj);
                        result.data["attachments"] = headerAttachmentsList;
                    }
                }

                if (getData.AxpertWSFormat)
                {
                    result.data["data"] = GetTstructDataWSFormatForAxGet(getData);
                    //ToDo HandleIncludeExcludeCols Logic.
                }
                else
                {
                    var tstData = GetTstructDataForAxGet(getData);
                    if (getData.CalledFrom != "SAVEDATA")
                        tstData = await _common.HandleIncludeExcludeCols(tstData, accessResult, _validate._sdef.FieldWiseDCs);
                    result.data["data"] = tstData;
                }
            }
            catch (Exception ex)
            {
                result = new APIResult(false, ex.Message);
                result.data["errormsg"] = ex.Message;
            }

            return result;

        }

        private (string, List<string>) GetDCSQLProperties(string dcName, DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                var dcNames = row["dcno"].ToString().Split(',').ToList();
                foreach (var dc in dcNames)
                {
                    if (dc.Trim().Equals(dcName, StringComparison.OrdinalIgnoreCase))
                    {
                        return (row["sqltext"].ToString(), dcNames);
                    }
                }
            }

            return ("", new List<string>());
        }


        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetTstructDataForAxGet(AxGet getData)
        {
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> tstData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
            {
                var rowNo = 1;
                int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);
                var currentFrame = _validate._sdef.Frames[fidx];

                Dictionary<string, Dictionary<string, string>> dcRowsData = new Dictionary<string, Dictionary<string, string>>();
                for (int idx = 1; idx <= totalRowCount; idx++)
                {
                    rowNo = idx;

                    int startFldIdx = currentFrame.StartIndex;
                    int endFldIdx = currentFrame.StartIndex + currentFrame.FieldCount;
                    Dictionary<string, string> fieldData = new Dictionary<string, string>();
                    fieldData[$"axp_recid"] = currentFrame.RecordIdDict[rowNo].ToString() ?? "0";
                    for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                    {
                        TField field = _validate._sdef.Flds[fldIdx];                        
                        var value = _validate._sdef.GetFieldValue(field, rowNo);

                        if (field.DataType == "d")
                            fieldData[field.FieldName] = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);
                        else
                            fieldData[field.FieldName] = value;

                        if (field.ModeofEntry.ToUpper() == "SELECT")
                        {
                            if (field.SourceKey)
                            {
                                fieldData[field.FieldName + "_sourceid"] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                            }
                            else
                            {
                                fieldData[field.FieldName + "_sourceid"] = "0";
                            }
                        }

                        if (getData.CalledFrom != "SAVEDATA")
                        {
                            if (field.DataType == "i")
                            {
                                var fldname = field.FieldName;
                                var imageObj = new AxAttachments();
                                imageObj.TransId = getData.TransId;
                                imageObj.FieldName = fldname;
                                imageObj.RecordId = (Int64)getData.RecordId;
                                imageObj.AttachmentType = "IMAGE";

                                AxAttachmentResponse imageData = _attachmentsService.GetImage(imageObj);

                                if (imageData != null && !string.IsNullOrEmpty(imageData.FileData))
                                {
                                    var imageJson = JsonConvert.SerializeObject(imageData);

                                    if (!string.IsNullOrEmpty(imageJson))
                                    {
                                        fieldData[field.FieldName] = imageJson;
                                    }
                                }
                                
                            }
                            else if (field.FieldName.ToLower().StartsWith("axpfile_"))
                            {
                                var filePath = _validate._sdef.GetFieldValue(field.FieldName.Replace("axpfile_", "axpfilepath_"), rowNo).ToString();
                                if (filePath.EndsWith('*'))
                                    filePath = filePath.TrimEnd('*');

                                var fldname = field.FieldName;
                                var axpFileObj = new AxAttachments();
                                axpFileObj.TransId = getData.TransId;
                                axpFileObj.FieldName = fldname;
                                axpFileObj.RecordId = (Int64)getData.RecordId;
                                axpFileObj.AttachmentType = "AXPFILE";
                                axpFileObj.FileName = value;
                                axpFileObj.FilePath = filePath;                                
                                List<AxAttachmentResponse> axpFilesData =  _attachmentsService.GetAxpFilesFromFolder(axpFileObj);
                                if (axpFilesData != null && axpFilesData.Count > 0)
                                {
                                    var filesJson = JsonConvert.SerializeObject(axpFilesData);

                                    if (!string.IsNullOrEmpty(filesJson))
                                    {
                                        fieldData[field.FieldName] = filesJson;
                                    }
                                }
                            }
                        }

                    }
                    dcRowsData[$"row{rowNo}"] = fieldData;
                }

                tstData[$"dc{currentFrame.FrameNo}"] = dcRowsData;
            }

            return tstData;
        }


        public List<Dictionary<string, string>> GetTstructDataWSFormatForAxGet(AxGet getData)
        {
            List<Dictionary<string, string>> tstData = new List<Dictionary<string, string>>();
            for (int fidx = 0; fidx < _validate._sdef.Frames.Count; fidx++)
            {
                var rowNo = 1;
                int totalRowCount = _validate._sdef.GetRowCount(fidx + 1);

                var currentFrame = _validate._sdef.Frames[fidx];

                var fieldData = new Dictionary<string, string>();
                fieldData["n"] = $"DC{currentFrame.FrameNo}";
                fieldData["v"] = totalRowCount.ToString();
                fieldData["t"] = "dc";

                if (currentFrame.AsGrid)
                {
                    if (totalRowCount > 0)
                    {
                        fieldData["cr"] = string.Join(",", Enumerable.Range(1, totalRowCount).Select(i => $"i{i}"));
                        fieldData["hasdatarows"] = "yes";
                    }
                    else
                        fieldData["hasdatarows"] = "no";
                }

                tstData.Add(fieldData);

                Dictionary<string, Dictionary<string, string>> dcRowsData = new Dictionary<string, Dictionary<string, string>>();
                for (int idx = 1; idx <= totalRowCount; idx++)
                {
                    rowNo = idx;

                    int startFldIdx = currentFrame.StartIndex;
                    int endFldIdx = _validate._sdef.Frames[fidx].StartIndex + currentFrame.FieldCount;

                    fieldData = new Dictionary<string, string>();
                    fieldData["n"] = $"axp_recid{currentFrame.FrameNo}";
                    fieldData["v"] = currentFrame.RecordIdDict[rowNo].ToString() ?? "0";
                    if (currentFrame.AsGrid)
                        fieldData["r"] = rowNo.ToString();
                    else
                        fieldData["r"] = "0";

                    fieldData["t"] = "s";
                    tstData.Add(fieldData);

                    for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                    {
                        TField field = _validate._sdef.Flds[fldIdx];
                        var value = _validate._sdef.GetFieldValue(field, rowNo);
                        if (string.IsNullOrEmpty(value))
                            continue;

                        if (field.DataType == "d")
                            value = _common.ConvertDBDateToClientDateFormat(field.CustType, value, _validate._sdef.clientDateFormat);

                        fieldData = new Dictionary<string, string>();
                        fieldData["n"] = field.FieldName;
                        fieldData["v"] = value;

                        if (currentFrame.AsGrid)
                            fieldData["r"] = rowNo.ToString();
                        else
                            fieldData["r"] = "0";

                        if (field.ModeofEntry.ToUpper() == "SELECT")
                        {
                            if (field.SourceKey)
                            {
                                fieldData["idcol"] = "yes";
                                fieldData["id"] = _validate._sdef.GetIDValue(field, rowNo).ToString();
                                fieldData["oldid"] = _validate._sdef.GetOldIDValue(field, rowNo).ToString();
                            }
                            else
                            {
                                fieldData["idcol"] = "no";
                                fieldData["id"] = "0";
                                fieldData["oldid"] = "0";
                            }
                        }

                        fieldData["t"] = field.DataType;

                        tstData.Add(fieldData);

                    }
                }
            }

            return tstData;
        }

        private async Task LogAxGetDetails(AxGet input, APIResult response)
        {
            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId; ;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = (response.success ? "success" : response.error);
            _axpertLog.RecordId = input.RecordId ?? 0;
            _axpertLog.StructName = input.TransId;
            _axpertLog.ServiceName = "ARM - AxGet API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }
    }
}
