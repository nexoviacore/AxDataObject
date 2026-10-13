using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Data;
using System.Xml;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Service
{
    public class AxGetDataService : IAxGetDataService
    {
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private Dictionary<string, object> sessionVars;
        private bool isTracing;
        public AxGetDataService(IRedisHelper redis, IConfiguration config, Utils common, IARMLogTraceService traceService)
        {
            _redis = redis;
            _config = config;
            _common = common;
            _traceService = traceService;
        }

        public async Task<APIResult> AxGet(AxGetData getData)
        {
            isTracing = getData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getData)}");

            sessionVars = await _common.GetSessionData(getData.ARMSessionId);

            var apiResult = new APIResult();
            if (sessionVars == null || sessionVars.Count == 0)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Session data is not available.";
                return apiResult;
            }

            var validRoles = await _common.IsValidRole(getData.ARMSessionId, getData.Roles, sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

            if (!validRoles)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"User does not access to some of the roles.";
                return apiResult;
            }

            getData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            getData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
            getData.GlobalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());

            if (getData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                if (!string.IsNullOrEmpty(getData.TransId) && getData.ADSNames?.Count > 0)
                {
                    List<Task<APIResult>> adsData = new List<Task<APIResult>>();

                    foreach (var adsName in getData.ADSNames)
                    {
                        adsData.Add(GetADSData(getData, adsName));
                    }

                    var adsDataResults = await Task.WhenAll(adsData);

                    apiResult.success = true;
                    List<object> resultList = new List<object>();
                    foreach (var adsDataResult in adsDataResults)
                    {
                        var tempDictionary = new Dictionary<string, object>();

                        if (!string.IsNullOrEmpty(adsDataResult.error))
                        {
                            tempDictionary.Add("error", adsDataResult.error);

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
                                tempDictionary.Add("error", adsDataResult.data?["error"]?.ToString());

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

                    apiResult.data["ADSNames"] = string.Join(",", getData.ADSNames);
                    apiResult.data["data"] = resultList;

                }
                else
                    apiResult = await GetTstructLoadData(getData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {getData.Action.ToUpper()}.";
                return apiResult;
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile($"AxGet-{getData.TransId}");
            }


            return apiResult;

        }

        private async Task<APIResult> GetTstructLoadData(AxGetData getData)
        {
            try
            {
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

                    getData.RecordId = accessResult.data["RecordId"].ToString();
                    var apiResult = new APIResult();
                    var loadDataResult = await ExecuteLoadDataAPI(getData, accessResult);

                    if (string.IsNullOrEmpty(loadDataResult.error))
                    {
                        apiResult.success = true;
                        apiResult.data["transid"] = getData.TransId;
                        apiResult.data["data"] = loadDataResult.data;
                    }
                    else
                    {
                        apiResult.success = false;
                        apiResult.data["transid"] = getData.TransId;
                        apiResult.data["error"] = loadDataResult.error;
                    }

                    return apiResult;

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

                apiResult.error = "Error occured in 'GetTstructLoadData'.";

                return apiResult;
            }
        }

        private async Task<APIResult> CheckTstructViewAccess(AxGetData getData)
        {
            var result = new APIResult();

            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            var userPermissions = await GetViewPermission(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
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
            }

            result.data["ExcludedDCs"] = string.Join(",", excludedDCElements.Distinct().ToList<string>());
            result.data["IncludedDCs"] = string.Join(",", includeDCElements.Distinct().ToList<string>());
            result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
            result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
            result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
            result.data["RecordId"] = recordid;

            return result;
        }

        private async Task<SQLResult> GetViewPermission(AxGetData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXGET_VIEWPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.KeyField, $"'{getData.KeyValue}'", getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.GET_AXGET_VIEWPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXGET_VIEWPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.KeyField, $"'{getData.KeyValue}'", getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
        }

        private async Task<APIResult> ExecuteLoadDataAPI(AxGetData getData, APIResult accessResult)
        {
            try
            {
                APIResult result = new APIResult();
                var sessionVars = await _common.GetSessionData(getData.ARMSessionId);

                string armWebScriptsURL = _common.GetARMScriptsURL(getData.Project);

                string apiUrl = armWebScriptsURL + "/ASBTStructRest.dll/datasnap/rest/TASBTStruct/LoadData";
                var api = new API();
                DateTime startTime = DateTime.Now;

                JObject dataJson = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(getData));
                var axPropsXML = await _common.GetAxProps(getData.Project);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(axPropsXML);
                string axPropsJson = JsonConvert.SerializeXmlNode(doc.DocumentElement, Newtonsoft.Json.Formatting.Indented, true);

                var loadDataJson = new JObject
                {
                    ["loaddata"] = new JObject
                    {
                        ["axpapp"] = dataJson["Project"].ToString(),
                        ["seed"] = "",
                        ["sessionid"] = dataJson["ARMSessionId"].ToString(),
                        ["username"] = dataJson["UserName"].ToString(),
                        ["trace"] = dataJson["Trace"].ToString() ?? "",
                        ["transid"] = dataJson["TransId"].ToString(),
                        ["recordid"] = dataJson["RecordId"].ToString(),
                        ["dcname"] = "" //TODO
                    },
                    ["axprops"] = JObject.Parse(axPropsJson),
                    ["globalvars"] = JObject.Parse(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString()),
                    ["uservars"] = JObject.Parse(sessionVars[SESSION_DATA.USERVARS.ToString()].ToString()),
                    [dataJson["Project"].ToString()] = JObject.Parse(JsonConvert.SerializeObject(_common._appSettingsHelper.GetAppConnection(getData.Project))), //JObject.Parse(sessionVars[SESSION_DATA.AXAPPS.ToString()].ToString()),
                };

                var apiInput = JsonConvert.SerializeObject(loadDataJson);

                var armResult = await api.POSTData(apiUrl, apiInput, "application/json");
               
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"API URL - {apiUrl}");
                    _traceService.LogMessage($"API JSON - {apiInput}");
                    _traceService.LogMessage($"API Result - {JsonConvert.SerializeObject(armResult)}");
                    
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;
                    _traceService.LogMessage($"API Call - Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
                }

                var resultNode = ParseAxpertLoadDataRestAPIResult(armResult, accessResult);

                return resultNode;
            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'LoadData' API call.";

                return apiResult;
            }
        }

        private APIResult ParseAxpertLoadDataRestAPIResult(ARMResult armResult, APIResult accessResult)
        {
            APIResult result = new APIResult();
            result.data.Add("success", armResult.result["success"]);
            JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
            if (resultJsonObj?["status"] == null)
            {
                resultJsonObj = (JObject)resultJsonObj?["result"]?[0];
            }

            if (resultJsonObj["error"] != null)
            {
                JObject errorJsonObj = JObject.Parse(resultJsonObj["error"].ToString());

                if (errorJsonObj["msg"] != null)
                    result.error = errorJsonObj?["msg"]?.ToString();
                else
                {
                    result.error = resultJsonObj["error"].ToString();
                }

                return result;
            }

            if (resultJsonObj["message"] != null && resultJsonObj["message"]?[0] != null)
            {
                JObject messageJsonObj = JObject.Parse(resultJsonObj["message"][0].ToString());

                if (messageJsonObj["recordid"] != null)
                    result.data["recordid"] = messageJsonObj?["recordid"]?.ToString();

                if (messageJsonObj["msg"] != null)
                    result.data["message"] = messageJsonObj?["msg"]?.ToString();
                else
                {
                    result.data["message"] = resultJsonObj["message"].ToString();
                }
                return result;
            }

            if (!(resultJsonObj?["result"]?.ToString().ToLower() == "true" || resultJsonObj?["result"]?.ToString().ToLower() == "success"))
            {
                if (!string.IsNullOrEmpty(resultJsonObj?["result"]?.ToString()))
                {
                    result.data["result"] = ParseLoadDataResultJson(resultJsonObj, accessResult);
                }
                return result;
            }

            if (!(resultJsonObj?["status"]?.ToString().ToLower() == "true" || resultJsonObj?["status"]?.ToString().ToLower() == "success"))
            {
                if (!string.IsNullOrEmpty(resultJsonObj?["result"]?.ToString()))
                    result.error = resultJsonObj?["result"]?.ToString();
                return result;
            }

            return result;

        }

        private Dictionary<string, Dictionary<string, Dictionary<string, object>>> ParseLoadDataResultJson(JObject jObject, APIResult accessResult)
        {
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            string currentDc = null;
            int currentRow = 0;
            var rowData = new Dictionary<string, object>();

            var excludedDCs = accessResult.data["ExcludedDCs"].ToString().Split(',').ToList<string>();
            var includeDCs = accessResult.data["IncludedDCs"].ToString().Split(',').ToList<string>();
            var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
            var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
            var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

            var result = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
            var fieldDataRow = jObject["result"].Where(item => item["t"].ToString() != "dv").ToList();




            // Process DC inclusion/exclusion logic
            if (includeDCs.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeDCs = fieldDataRow.Where(i => i["t"].ToString() == "dc")
                                 .Select(i => i["n"].ToString())
                                 .ToList();
            }

            if (excludedDCs.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludedDCs.Clear();
            }

            // Process field inclusion/exclusion logic
            if (includeFields.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeFields = fieldDataRow.Where(i => i["t"].ToString() != "dc")
                                     .Select(i => i["n"].ToString())
                                     .Distinct()
                                     .ToList();
            }

            if (excludedFields.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludedFields.Clear();
            }

            // Parse masking rules
            var maskRules = maskedFields
                .Where(r => r.Contains("="))
                .Select(r => r.Split('='))
                .Where(r => r.Length == 2)
                .ToDictionary(r => r[0], r => r[1]);

            foreach (var item in fieldDataRow)
            {
                var type = item["t"].ToString();
                var name = item["n"]?.ToString().ToLower();

                if (type == "dc")
                {
                    // Skip if DC is excluded
                    if (excludedDCs.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        currentDc = null;
                        continue;
                    }

                    if (!includeDCs.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        currentDc = null;
                        continue;
                    }

                    // Save previous DC data if exists
                    if (currentDc != null)
                    {
                        SaveCurrentRowData(result, currentDc, currentRow, rowData);
                    }

                    // Start new DC
                    currentDc = name;
                    var currDc = currentDc.Substring(2);
                    excludedFields.Add($"axp_recid{currDc}");

                    currentRow = 1;
                    rowData = new Dictionary<string, object>();
                }
                else
                {
                    // Skip if not in a DC or if field should be excluded
                    if (currentDc == null) continue;

                    // Skip if field should be excluded
                    if (excludedFields.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    if (!includeFields.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;


                    var row = item["r"]?.ToString();
                    if (row != null && int.TryParse(row, out int rowNum))
                    {
                        if (rowNum == 0) rowNum = 1;

                        if (rowNum != currentRow)
                        {
                            // Save current row data
                            SaveCurrentRowData(result, currentDc, currentRow, rowData);

                            // Start new row
                            currentRow = rowNum;
                            rowData = new Dictionary<string, object>();
                        }
                    }

                    if (name != null)
                    {
                        var value = item["v"]?.ToString();
                        if (value != null)
                        {
                            // Apply masking if needed
                            if (maskRules.ContainsKey(name))
                            {
                                rowData[name] = _common.ApplyFieldMask(value, maskRules[name]);
                            }
                            else
                            {
                                rowData[name] = value;
                            }
                        }
                    }
                }
            }

            // Save the last DC data
            if (currentDc != null)
            {
                SaveCurrentRowData(result, currentDc, currentRow, rowData);
            }

            return result;
        }

        private void SaveCurrentRowData(
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> result,
            string currentDc,
            int currentRow,
            Dictionary<string, object> rowData)
        {
            if (!result.ContainsKey(currentDc))
            {
                result[currentDc] = new Dictionary<string, Dictionary<string, object>>();
            }

            var rowKey = $"row{currentRow}";
            if (!result[currentDc].ContainsKey(rowKey))
            {
                result[currentDc][rowKey] = new Dictionary<string, object>();
            }

            result[currentDc][rowKey] = rowData;
        }

        
        private async Task<SQLResult> GetData(string dbType, string sql, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        private async Task<SQLResult> GetStoredProcedureData(string dbType, string procName, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteStoredProcedureAsync(procName, connectionString, paramName, paramType, paramValue);
        }


        public async Task<APIResult> AxList(AxListData getData)
        {
            isTracing = getData.Trace == true;
            _traceService.EnableTracing(isTracing, true);

            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getData)}");

            var apiResult = new APIResult();

            if (string.IsNullOrEmpty(getData.Project))
            {
                sessionVars = await _common.GetSessionData(getData.ARMSessionId);

                if (sessionVars == null || sessionVars.Count == 0)
                {
                    apiResult.success = false;
                    apiResult.data["error"] = $"Session data is not available.";
                    return apiResult;
                }

                var validRoles = await _common.IsValidRole(getData.ARMSessionId, getData.Roles, sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

                if (!validRoles)
                {
                    apiResult.success = false;
                    apiResult.data["error"] = $"User does not access to some of the roles.";
                    return apiResult;
                }

                getData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                getData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                getData.GlobalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());
            }

            if (getData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                if (!string.IsNullOrEmpty(getData.TransId) && (getData.ADSNames == null || getData.ADSNames?.Count == 0))
                    apiResult = await GetListViewData(getData);
                else if (!string.IsNullOrEmpty(getData.TransId) && (getData.ADSNames != null && getData.ADSNames?.Count > 0))
                {
                    var accessResult = await CheckListViewAccess(getData);

                    var config = new MapperConfiguration(cfg => cfg.CreateMap<AxListData, AxGetData>());
                    var mapper = config.CreateMapper();
                    AxGetData data = mapper.Map<AxGetData>(getData);

                    List<Task<APIResult>> adsData = new List<Task<APIResult>>();

                    foreach (var adsName in getData.ADSNames)
                    {
                        adsData.Add(GetListViewBasedADSData(data, accessResult, adsName));
                    }

                    var adsDataResults = await Task.WhenAll(adsData);

                    apiResult.success = true;
                    List<object> resultList = new List<object>();
                    foreach (var adsDataResult in adsDataResults)
                    {
                        var tempDictionary = new Dictionary<string, object>();

                        if (!string.IsNullOrEmpty(adsDataResult.error))
                        {
                            tempDictionary.Add("error", adsDataResult.error);
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
                                tempDictionary.Add("error", adsDataResult.data?["error"]?.ToString());

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

                    apiResult.data["ADSNames"] = string.Join(",", getData.ADSNames);
                    apiResult.data["data"] = resultList;
                }
                else if (string.IsNullOrEmpty(getData.TransId) && (getData.ADSNames != null && getData.ADSNames?.Count > 0))
                {
                    var config = new MapperConfiguration(cfg => cfg.CreateMap<AxListData, AxGetData>());
                    var mapper = config.CreateMapper();
                    AxGetData data = mapper.Map<AxGetData>(getData);

                    List<Task<APIResult>> adsData = new List<Task<APIResult>>();

                    foreach (var adsName in getData.ADSNames)
                    {
                        adsData.Add(GetADSData(data, adsName));
                    }

                    var adsDataResults = await Task.WhenAll(adsData);

                    apiResult.success = true;
                    List<object> resultList = new List<object>();
                    foreach (var adsDataResult in adsDataResults)
                    {
                        var tempDictionary = new Dictionary<string, object>();

                        if (!string.IsNullOrEmpty(adsDataResult.error))
                        {
                            tempDictionary.Add("error", adsDataResult.error);
                        }

                        if (adsDataResult.data != null)
                        {
                            if (adsDataResult.data.ContainsKey("adsname"))
                                tempDictionary.Add("adsname", adsDataResult.data?["adsname"]?.ToString());

                            if (adsDataResult.data.ContainsKey("data"))
                                tempDictionary.Add("data", adsDataResult.data?["data"]);

                            if (adsDataResult.data.ContainsKey("totalrecords"))
                                tempDictionary.Add("totalrecords", adsDataResult.data?["totalrecords"]?.ToString());

                            if (!tempDictionary.ContainsKey("error") && adsDataResult.data.ContainsKey("error") && !string.IsNullOrEmpty(adsDataResult.data?["error"]?.ToString() ?? ""))
                                tempDictionary.Add("error", adsDataResult.data?["error"]?.ToString());

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

                    apiResult.data["ADSNames"] = string.Join(",", getData.ADSNames);
                    apiResult.data["data"] = resultList;
                }
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {getData.Action.ToUpper()}.";
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile($"AxList-{getData.TransId ?? ""}{getData.ADSName ?? ""}");
            }

            return apiResult;
        }

        private async Task<APIResult> GetListViewData(AxListData getData)
        {
            var accessResult = await CheckListViewAccess(getData);

            if (!string.IsNullOrEmpty(accessResult.error))
            {              
                accessResult.data["transid"] = getData.TransId;
                return accessResult;
            }
            else
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"CheckListViewAccess Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                }
                var filterCond = accessResult.data["FilterCond"].ToString();
                getData.Constraint = filterCond;

                var apiResult = new APIResult();
                var listDataResult = await GetListDataFromDB(getData);
                if (string.IsNullOrEmpty(listDataResult.error) && listDataResult.DataSet != null && listDataResult.DataSet.Tables.Count > 0)
                {
                    var dt = listDataResult.DataSet.Tables[0];

                    var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
                    var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
                    var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

                    var newDt = await _common.HandleIncludeExcludeCols(dt, excludedFields, includeFields, maskedFields);

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

        public async Task<APIResult> CheckListViewAccess(AxListData getData)
        {
            var cacheKey = _common.GenerateRedisKeyString(new string[] { getData.SchemaName ?? getData.Project, getData.TransId, "ARM-AX-VIEW-PERMISSION", getData.UserName, string.Join(",", getData.Roles) ?? "All" });
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult)) 
            {
                if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Cache - Access Loaded from cache", cachedResult);
                return JsonConvert.DeserializeObject<APIResult>(cachedResult);
            }

            var result = new APIResult();

            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            var userPermissions = await GetListViewPermission(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
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
            }

            result.data["ExcludedDCs"] = string.Join(",", excludedDCElements.Distinct().ToList<string>());
            result.data["IncludedDCs"] = string.Join(",", includeDCElements.Distinct().ToList<string>());
            result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
            result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
            result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
            result.data["FilterCond"] = filterCond;

            if (!string.IsNullOrEmpty(filterCond))
                getData.Constraint = filterCond;

            await _redis.StringSetAsync(cacheKey, JsonConvert.SerializeObject(result));
            return result;
        }

        private async Task<SQLResult> GetListViewPermission(AxListData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pusername", "@proles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {

                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@username", "@roles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
        }

        private async Task<SQLResult> GetConnectedDataListViewPermission(AxListData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pusername", "@proles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.ConnectedData_TransIds, getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {

                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@username", "@roles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.ConnectedData_TransIds, getData.UserName, string.Join(",", getData.Roles) };

                var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }

        }

        public async Task<SQLDataSetResult> GetListDataFromDB(AxListData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string applyDac = "F";
            string constraints = "";

            if (!string.IsNullOrEmpty(getData.Constraint)){
                applyDac = "T";
                constraints = getData.Constraint;
            }

            string sql = "";
            getData.ViewFilters = await _common.GetViewFilterForTstruct(getData.ARMSessionId, getData.TransId);
            getData.GlobalParams = await _common.GetGlobalParams(getData.ARMSessionId);

            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXLIST_LISTDATA_MSSQL;
                string[] paramName = { "@ptransid", "@pflds", "@ppagesize", "@ppageno", "@pfilter", "@pusername", "@papplydac", "@puserrole", "@pconstraints" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.TransId, getData.Fields ?? "All", getData.PageSize, getData.PageNo, getData.Filter ?? "NA", getData.UserName, applyDac, string.Join(",", getData.Roles), constraints };

                var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, getData.ViewFilters, getData.GlobalParams);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.GET_AXLIST_LISTDATA.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXLIST_LISTDATA_ORACLE.ToString();

                string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno", "@filter", "@username", "@applydac", "@roles", "@constraints" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.TransId, getData.Fields ?? "All", getData.PageSize, getData.PageNo, getData.Filter ?? "NA", getData.UserName, applyDac, string.Join(",", getData.Roles), constraints };            

                var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, getData.ViewFilters, getData.GlobalParams);
                return dsResult;
            }
        }

        private async Task<SQLDataSetResult> GetDataSet(string dbType, string sql, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null, bool getCount = false)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLsFromSQLAsync(sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams, sqlCommentsReplace, getCount);
        }


        private async Task<APIResult> GetADSData(AxGetData getData, string adsName)
        {
            var accessResult = await CheckADSDataViewAccess(getData, adsName);

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                accessResult.data["adsname"] = adsName;
                return accessResult;
            }
            else
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"CheckADSDataViewAccess Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                }

                var apiResult = new APIResult();
                var adsResult = await GetADSDataFromDB(getData, adsName);
              
                if (string.IsNullOrEmpty(adsResult.error) && adsResult.DataSet != null && adsResult.DataSet.Tables.Count > 0)
                {
                    var dt = adsResult.DataSet.Tables[0];

                    var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
                    var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
                    var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

                    var newDt = await _common.HandleIncludeExcludeCols(dt, excludedFields, includeFields, maskedFields);

                    apiResult.success = true;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["data"] = newDt;
                    apiResult.data["totalrecords"] = adsResult.RecordsCount[0].ToString();
                }
                else
                {
                    apiResult.success = false;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["error"] = adsResult.error;
                }

                if (isTracing)
                {
                    _traceService.LogMessage($"GetADSDataFromDB Result JSON - {JsonConvert.SerializeObject(adsResult)}");
                }

                return apiResult;

            }
        }
        private async Task<APIResult> GetListViewBasedADSData(AxGetData getData, APIResult accessResult = null, string adsName = "")
        {            
            if (!string.IsNullOrEmpty(accessResult.error))
            {
                accessResult.data["adsname"] = adsName;
                return accessResult;
            }
            else
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"CheckListViewAccess Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                }

                var apiResult = new APIResult();
                var adsResult = await GetADSDataFromDB(getData, adsName);                
                if (string.IsNullOrEmpty(adsResult.error) && adsResult.DataSet != null && adsResult.DataSet.Tables.Count > 0)
                {                    
                    var dt = adsResult.DataSet.Tables[0];


                    var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
                    var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
                    var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

                    var newDt = await _common.HandleIncludeExcludeCols(dt, excludedFields, includeFields, maskedFields);

                    apiResult.success = true;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["data"] = newDt;
                    apiResult.data["totalrecords"] = adsResult.RecordsCount[0].ToString();
                }
                else
                {
                    apiResult.success = false;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["error"] = adsResult.error;
                }

                if (isTracing)
                {
                    _traceService.LogMessage($"GetADSDataFromDB Result JSON - {JsonConvert.SerializeObject(adsResult)}");
                }

                return apiResult;

            }
        }
        private async Task<APIResult> CheckADSDataViewAccess(AxGetData getData, string adsName)
        {
            var result = new APIResult();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            var userPermissions = await GetADSDataViewPermission(getData, adsName);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
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

            if (excludedElements.Count == 0) excludedElements.Add(none);
            if (includeElements.Count == 0) includeElements.Add(all);

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Excluded Elements - {string.Join(",", excludedElements)}");
                _traceService.LogMessage($"Included Elements - {string.Join(",", includeElements)}");
                _traceService.LogMessage($"Masked Fields - {string.Join(",", maskedFields)}");
            }

            result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
            result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
            result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
            result.data["FilterCond"] = filterCond;

            if (!string.IsNullOrEmpty(filterCond))
                getData.Constraint = filterCond;

            return result;
        }

        private async Task<SQLResult> GetADSDataViewPermission(AxGetData getData, string adsName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = Constants_SQL.GET_AXLIST_ADSDATAVIEWPERMISSION.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_AXLIST_ADSDATAVIEWPERMISSION_ORACLE.ToString();

            string[] paramName = { "@transid", "@datasource", "@username", "@roles",  "@keyfield", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { getData.TransId ?? "", adsName, getData.UserName, string.Join(",", getData.Roles), getData.KeyField ?? "", getData.KeyValue ?? "" };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }        

        private async Task<SQLDataSetResult> GetADSDataFromDB(AxGetData getData, string adsName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (string.IsNullOrEmpty(getData.Constraint) || string.IsNullOrEmpty(getData.TransId))
            {
                getData.TransId = "";
                getData.Constraint = "NA";
            }

            string sql = Constants_SQL.GET_AXGET_GETADSSQL.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_AXGET_GETADSSQL_ORACLE.ToString();

            string[] paramName = { "@transid", "@datasource", "@constraints" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { getData.TransId ?? "", adsName, getData.Constraint ?? "NA"};

            getData.ViewFilters = await _common.GetViewFilterForTstruct(getData.ARMSessionId, getData.TransId);
            getData.GlobalParams = await _common.GetGlobalParams(getData.ARMSessionId);
            var sqlCommentsReplace = await GetDynamicFiltersSortingPagination(getData);

            if (getData.GlobalParams.ContainsKey("keyvalue"))
                getData.GlobalParams["keyvalue"] = getData.KeyValue;
            else
                getData.GlobalParams.Add("keyvalue", getData.KeyValue);

            if (getData.SqlParams != null && getData.SqlParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> param in getData.SqlParams)
                {
                    if (getData.GlobalParams.ContainsKey(param.Key))
                        getData.GlobalParams[param.Key] = param.Value;
                    else
                        getData.GlobalParams.Add(param.Key, param.Value);
                }
            }            
            
            if (getData.SqlParams != null && getData.SqlParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> param in getData.SqlParams)
                {
                    if (getData.GlobalParams.ContainsKey(param.Key))
                        getData.GlobalParams[param.Key] = param.Value;
                    else
                        getData.GlobalParams.Add(param.Key, param.Value);
                }
            }

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, getData.ViewFilters, getData.GlobalParams, sqlCommentsReplace, Convert.ToBoolean(getData.GetAllRecordsCount));
            return dsResult;
        }

        public async Task<Dictionary<string, string>> GetDynamicFiltersSortingPagination(AxGetData getData)
        {
            Dictionary<string, string> sqlReplaceStr = new Dictionary<string, string>();
            List<string> conditions = new();

            // Filtering Logic
            foreach (var filter in getData.Filters)
            {
                string fldname = filter["fldname"].ToString();
                string datatype = filter["datatype"].ToString().ToUpper();

                switch (datatype)
                {
                    case "TEXT":
                        string value = filter["value"].ToString().Replace("'", "''");
                        string condition = filter.ContainsKey("condition") ? filter["condition"].ToString().ToUpper() : "EQUALS";

                        if (condition == "STARTSWITH")
                            conditions.Add($" lower({fldname}) LIKE '{value.ToLower()}%' ");
                        else if (condition == "ENDSWITH")
                            conditions.Add($" lower({fldname}) LIKE '%{value.ToLower()}' ");
                        else if (condition == "CONTAINS")
                            conditions.Add($" lower({fldname}) LIKE '%{value.ToLower()}%' ");
                        else
                            conditions.Add($" lower({fldname}) = '{value.ToLower()}' ");
                        break;

                    case "DATE":
                        List<string> dateConditions = new();
                        if (filter.ContainsKey("from"))
                        {
                            string fromDate = DateTime.ParseExact(filter["from"].ToString(), "dd-MMM-yyyy", null).ToString("yyyy-MM-dd");
                            dateConditions.Add($" {fldname} >= DATE '{fromDate}' ");
                        }
                        if (filter.ContainsKey("to"))
                        {
                            string toDate = DateTime.ParseExact(filter["to"].ToString(), "dd-MMM-yyyy", null).ToString("yyyy-MM-dd");
                            dateConditions.Add($" {fldname} <= DATE '{toDate}' ");
                        }
                        if (dateConditions.Any())
                            conditions.Add(string.Join(" AND ", dateConditions));
                        break;

                    case "DROPDOWN":

                        var dropdownValues = System.Text.Json.JsonSerializer.Deserialize<List<string>>(System.Text.Json.JsonSerializer.Serialize(filter["value"]));
                        if (dropdownValues.Count > 0)
                        {
                            var formatted = dropdownValues.Select(v => $"'{v.ToString().ToLower().Replace("'", "''")}'");
                            conditions.Add($" lower({fldname}) IN ({string.Join(",", formatted)}) ");
                        }
                        break;

                    case "NUMERIC":
                        List<string> numConditions = new();
                        if (filter.ContainsKey("from"))
                            numConditions.Add($" {fldname} >= {filter["from"]} ");
                        if (filter.ContainsKey("to"))
                            numConditions.Add($" {fldname} <= {filter["to"]} ");
                        if (numConditions.Any())
                            conditions.Add(string.Join(" AND ", numConditions));
                        break;
                }
            }

            // Handle --ui_filter_withwhere or --ui_filter
            if (conditions.Any())
            {
                string whereClause = " WHERE " + string.Join(" AND ", conditions);
                string andClause = " AND " + string.Join(" AND ", conditions);

                sqlReplaceStr.Add("--ui_filter_withwhere", whereClause);
                sqlReplaceStr.Add("--ui_filter", andClause);
            }

            // Handle --orderby and --orderby_append
            if (getData.Sorting != null && getData.Sorting.Any())
            {
                string orderByClause = string.Join(" , ", getData.Sorting.Select(s =>
                    $"{s["fldname"]} {s["sort_order"].ToUpper()}"));

                sqlReplaceStr.Add("--orderby_append", $" , {orderByClause} ");
                sqlReplaceStr.Add("--orderby", $" ORDER BY {orderByClause} ");
            }

            // Handle --pagination (uses LIMIT/OFFSET)
            if (Convert.ToInt32(getData.PageNo) > 0 && Convert.ToInt32(getData.PageSize) > 0)
            {
                int offset = (Convert.ToInt32(getData.PageNo) - 1) * Convert.ToInt32(getData.PageSize);
                sqlReplaceStr.Add("--pagination", $" LIMIT {Convert.ToInt32(getData.PageSize)} OFFSET {offset} ");
            }

            return sqlReplaceStr;
        }


        public async Task<APIResult> AxGetConnectedDataList(AxListData tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

            var apiResult = new APIResult();
            sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);
            if (sessionVars == null || sessionVars.Count == 0)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Session data is not available.";
                return apiResult;
            }

            tstructData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            tstructData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                apiResult = await CheckConnectedDataListViewAccess(tstructData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
            }
            return apiResult;
        }


        public async Task<APIResult> CheckConnectedDataListViewAccess(AxListData getData)
        {
            string connectedTransIds = string.Join("-", getData.ConnectedData_TransIds.Split(',').OrderBy(x => x));
            var cacheKey = _common.GenerateRedisKeyString(new string[] { getData.SchemaName ?? getData.Project, getData.TransId, connectedTransIds,  "ARM-AX-CONNECTEDDATA-PERMISSION", getData.UserName, string.Join(",", getData.Roles) ?? "All" });
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Cache - Access Loaded from cache", cachedResult);
                return JsonConvert.DeserializeObject<APIResult>(cachedResult);
            }

                      
            var userPermissions = await GetConnectedDataListViewPermission(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                var result = new APIResult();
                result.success = false;
                result.error = "User does not have access to these structures.";
                return result;
            }

            var groupedRows = userPermissions.data.AsEnumerable().GroupBy(row => row.Field<string>("transid"));

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            var connectedDataResult = new APIResult(); 
            foreach (var group in groupedRows)
            {
                var result = new APIResult();
                bool canView = false;
                string cTransId = "";
                List<string> excludedDCElements = new List<string>();
                List<string> includeDCElements = new List<string>();

                List<string> excludedElements = new List<string>();
                List<string> includeElements = new List<string>();
                List<string> maskedFields = new List<string>();

                var filterCond = "";
                foreach (DataRow dr in group)
                {
                    var view = dr["view_access"]?.ToString() ?? "";
                    var fullControl = dr["fullcontrol"]?.ToString() ?? "";

                    if ((!string.IsNullOrEmpty(fullControl) && fullControl.ToUpper() == "T") || (string.IsNullOrEmpty(fullControl) && (string.IsNullOrEmpty(view) || (!string.IsNullOrEmpty(view) && view.ToUpper() != "NONE"))))
                    {
                        canView = true;
                        cTransId = dr["transid"]?.ToString() ?? "";
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
                    continue;
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
                }

                result.data["ExcludedDCs"] = string.Join(",", excludedDCElements.Distinct().ToList<string>());
                result.data["IncludedDCs"] = string.Join(",", includeDCElements.Distinct().ToList<string>());
                result.data["ExcludedFields"] = string.Join(",", excludedElements.Distinct().ToList<string>());
                result.data["IncludedFields"] = string.Join(",", includeElements.Distinct().ToList<string>());
                result.data["MaskedFields"] = string.Join(",", maskedFields.Distinct().ToList<string>());
                result.data["FilterCond"] = filterCond;

                if (!string.IsNullOrEmpty(cTransId)) {
                    connectedDataResult.data[cTransId] = result;
                    var transidKey = _common.GenerateRedisKeyString(new string[] { getData.SchemaName ?? getData.Project, cTransId, "ARM-AX-VIEW-PERMISSION", getData.UserName, string.Join(",", getData.Roles) ?? "All" });
                    if(!_redis.KeyExists(transidKey))
                        await _redis.StringSetAsync(transidKey, JsonConvert.SerializeObject(result));
                }


                if (!string.IsNullOrEmpty(filterCond))
                    getData.Constraint = filterCond;

            }

            await _redis.StringSetAsync(cacheKey, JsonConvert.SerializeObject(connectedDataResult));
            return connectedDataResult;
        }
    }
}
