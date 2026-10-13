using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;
using System.Data;
using AutoMapper;
using StackExchange.Redis;

namespace AxList.Service
{
    public class AxListService : IAxListService
    {
        private readonly IAxListRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private readonly IADSDataService _adsDataService;
        bool isTracing = false;
        private AxpertLog _axpertLog = new AxpertLog();

        public AxListService(IAxListRepository repo, IARMLogTraceService traceService, Utils common, IADSDataService adsDataService)
        {
            _repo = repo;
            _traceService = traceService;
            _common = common;
            _adsDataService = adsDataService;
        }
        
        public async Task<APIResult> AxListAsync(AxList listData)
        {
            _axpertLog.CalledOn = DateTime.Now;
            if (listData.Trace)
            {
                isTracing = true;
                _traceService.EnableTracing(true);
                _traceService.LogMessage("AxList API call started.");
                _traceService.LogMessage($"Start Time: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(listData)}");
            }
            
            var apiResult = new APIResult();
            var isValidSession = await _common.ValidateSession(listData.ARMSessionId);
            if (!isValidSession)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"SessionId is not valid";
                return apiResult;
            }

            try
            {                
                // Extract session information
                listData.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                listData.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                listData.SchemaName = _common._sessionVars[SESSION_DATA.SCHEMANAME.ToString()].ToString();
                listData.RolesStr = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString();
                listData.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();
            
                if (listData.Roles != null && listData.Roles.Count > 0)
                {
                    var validRoles = await _common.IsValidRole(listData.ARMSessionId, listData.Roles, listData.RolesStr);

                    if (!validRoles)
                    {
                        apiResult.success = false;
                        apiResult.data["error"] = $"User does not access to some of the roles.";
                        return apiResult;
                    }
                }
                else if(listData.Roles == null || listData.Roles.Count == 0)
                {
                    listData.Roles = listData.RolesStr.Split(',').ToList();
                }                

                if (isTracing) _traceService.LogMessage($"Client date format from AxList Input Json: {listData.AxClient_DateFormat}");

                if (string.IsNullOrEmpty(listData.AxClient_DateFormat))
                {
                    listData.GlobalParams = await _common.GetGlobalParams(listData.ARMSessionId);
                    if (listData.GlobalParams.ContainsKey("axclient_dateformat"))
                    {
                        listData.AxClient_DateFormat = listData.GlobalParams["axclient_dateformat"];
                        if (isTracing) _traceService.LogMessage($"Client date format from Global Vars: {listData.AxClient_DateFormat}");
                    }
                    else
                        listData.AxClient_DateFormat = "dd/MM/yyyy";
                }
                if (isTracing) _traceService.LogMessage($"Final client date format: {listData.AxClient_DateFormat}");

                apiResult = await AxList(listData);
            }
            catch (Exception ex)
            {
                apiResult.success = false;
                apiResult.data["error"] = ex.Message;
                if (listData.Trace) _traceService.LogError("Error in AxList : " + ex.Message + ex.StackTrace + ex.InnerException);
            }
            finally
            {
                await LogAxListDetails(listData, apiResult);
                await _common.CloseDBConnectionAsync();
            }

            if (listData.Trace) apiResult.data["Trace"] = await _traceService.GetTraceFile($"AxList-{listData.TransId ?? ""}-{listData.ADSName ?? ""}-{string.Join(",", listData.ADSNames ?? new List<string>()) ?? ""}");

            return apiResult;
        }

        public async Task<APIResult> AxList(AxList listData)
        {            
            var apiResult = new APIResult();
            
            if (listData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                if (!string.IsNullOrEmpty(listData.TransId) && (listData.ADSNames == null || listData.ADSNames?.Count == 0))
                    apiResult = await GetListViewData(listData);
                else if (listData.ADSNames != null && listData.ADSNames?.Count > 0)
                {
                    if (!string.IsNullOrEmpty(listData.TransId))
                    {
                        var accessResult = await GetListViewPermission(listData);

                        if (!string.IsNullOrEmpty(accessResult.error))
                        {
                            accessResult.data["transid"] = listData.TransId;
                            return accessResult;
                        }
                    }

                    var config = new MapperConfiguration(cfg => cfg.CreateMap<AxList, ADSDataRequest>());
                    var mapper = config.CreateMapper();
                    ADSDataRequest adsDataRequest = mapper.Map<ADSDataRequest>(listData);                    

                    if (!listData.SqlParams.ContainsKey("keyvalue"))
                        listData.SqlParams.Add("keyvalue", listData.KeyValue);

                    if (!adsDataRequest.SQLParams.ContainsKey("keyvalue"))
                        adsDataRequest.SQLParams.Add("keyvalue", listData.KeyValue);

                    var adsDataResults = new List<APIResult>();
                    foreach (var adsName in listData.ADSNames)
                    {
                        adsDataRequest.DataSource = adsName;
                        var result = await _adsDataService.GetADSDataForAxList(listData, adsName, adsDataRequest);
                        adsDataResults.Add(result);
                    }

                    apiResult.success = true;
                    List<object> resultList = new List<object>();
                    int erroredADSCount = 0;
                    foreach (var adsDataResult in adsDataResults)
                    {
                        var tempDictionary = new Dictionary<string, object>();

                        if (!string.IsNullOrEmpty(adsDataResult.error))
                        {
                            tempDictionary.Add("error", adsDataResult.error);
                            erroredADSCount++;
                        }

                        if (adsDataResult.data != null)
                        {
                            if (adsDataResult.data.ContainsKey("adsname"))
                                tempDictionary.Add("adsname", adsDataResult.data?["adsname"]?.ToString());

                            if (adsDataResult.data.ContainsKey("data"))
                                tempDictionary.Add("data", adsDataResult.data?["data"]);

                            if (adsDataResult.data.ContainsKey("totalrecords"))
                                tempDictionary.Add("totalrecords", adsDataResult.data?["totalrecords"]);

                            if (!tempDictionary.ContainsKey("error") && adsDataResult.data.ContainsKey("error") && !string.IsNullOrEmpty(adsDataResult.data?["error"]?.ToString() ?? ""))
                            {
                                tempDictionary.Add("error", adsDataResult.data?["error"]?.ToString());
                                erroredADSCount++;
                            }
                            if (adsDataResult.data.ContainsKey("permissions"))
                                tempDictionary.Add("permissions", adsDataResult.data?["permissions"]?.ToString());

                            if (adsDataResult.data.ContainsKey("trace"))
                                tempDictionary.Add("trace", adsDataResult.data?["trace"]?.ToString());
                        }

                        if (tempDictionary != null && tempDictionary.Count > 0)
                        {
                            resultList.Add(tempDictionary.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value));
                        }
                    }

                    if(erroredADSCount > 0)
                    {
                        apiResult.data["partialsuccess"] = true;
                    }                     

                    apiResult.data["ADSNames"] = string.Join(",", listData.ADSNames);
                    apiResult.data["data"] = resultList;
                }                
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {listData.Action.ToUpper()}.";
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile($"AxList-{listData.TransId ?? ""}{listData.ADSName ?? ""}");
            }

            return apiResult;
        }

        private async Task<APIResult> GetListViewData(AxList listData)
        {
            var accessResult = await GetListViewPermission(listData);

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                accessResult.data["transid"] = listData.TransId;
                return accessResult;
            }
            else
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"GetListViewPermission Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                }
                var filterCond = accessResult.data["FilterCond"].ToString();
                listData.Constraint = filterCond;

                var apiResult = new APIResult();
                var listDataResult = await _repo.GetListDataFromDB(listData);
                if (string.IsNullOrEmpty(listDataResult.error) && listDataResult.data != null)
                {                    
                    var newDt = await _common.HandleIncludeExcludeCols(listDataResult.data, accessResult);
                    if (accessResult != null && accessResult.data != null && accessResult.data.ContainsKey("EncryptedFields") && !string.IsNullOrEmpty(accessResult.data["EncryptedFields"]?.ToString() ?? "")) 
                    {
                        var encryptedFields = accessResult.data["EncryptedFields"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();    
                        if(encryptedFields.Count > 0)
                            newDt = AXPEncryptData.CallDecryptDataTable(newDt, encryptedFields);
                    }

                    apiResult.success = true;
                    apiResult.data["data"] = newDt;
                }

                if (isTracing)
                {
                    _traceService.LogMessage($"GetListDataFromDB Result JSON - {JsonConvert.SerializeObject(listDataResult)}");
                }


                return apiResult;

            }
        }

        public async Task<APIResult> GetListViewPermission(AxList listData)
        {
            listData.Dimensions = _common.GetAxgApplicableDimensionsString();
            if (listData.CachePermissions)
            {
                var resultFromCache = await _repo.GetListViewPermissionFromCache(listData);
                if (resultFromCache != null)
                    return resultFromCache;
            }

            var result = new APIResult();

            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            List<string> encryptedFields = new List<string>();
            var userPermissions = await _repo.GetListViewPermissionFromDB(listData);

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

            bool canView = false;
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            var filterCond = "";
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
                    var filterStr = dr["filtercnd"].ToString() ?? "";

                    if (!string.IsNullOrEmpty(filterStr) && filterStr?.ToUpper() != "NA")
                    {
                        if (string.IsNullOrEmpty(filterCond))
                            filterCond = $"({filterStr})";
                        else
                            filterCond += $" or ({filterStr})";
                    }


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
                result.error = "User does not have permissions to view this screen.";
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
                _traceService.LogMessage($"Masked Fields - {string.Join(",", maskedFields)}");
                _traceService.LogMessage($"Encrypted Fields - {string.Join(",", encryptedFields)}");
            }

            result.data["ExcludedDCs"] = string.Join(",", excludedDCElements.Distinct().ToList<string>());
            result.data["IncludedDCs"] = string.Join(",", includeDCElements.Distinct().ToList<string>());
            result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
            result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
            result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
            result.data["FilterCond"] = filterCond;
            result.data["EncryptedFields"] = string.Join(",", encryptedFields.Distinct().ToList<string>()); ;

            if (!string.IsNullOrEmpty(filterCond))
                listData.Constraint = filterCond;

            if (listData.CachePermissions)
                await _repo.StoreListViewPermissionToCache(listData, result);

            return result;
        }

        private async Task LogAxListDetails(AxList input, APIResult response)
        {
            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId; ;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = (response.success ? "success" : response.error);
            _axpertLog.RecordId = Convert.ToInt64(input.RecordId ?? "0");
            _axpertLog.StructName = input.TransId;
            _axpertLog.CallDetails = string.Join(",", input.ADSNames ?? new List<string>());
            _axpertLog.ServiceName = "ARM - AxList API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }
    }
}
