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
using System.Text;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Service
{
    public class AxPutDataService : IAxPutDataService
    {
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private Dictionary<string, object> sessionVars;
        private bool isTracing;
        public AxPutDataService(IRedisHelper redis, IConfiguration config, Utils common, IARMLogTraceService traceService)
        {
            _redis = redis;
            _config = config;
            _common = common;
            _traceService = traceService;
        }


        public async Task<APIResult> AxPut(AxPutData tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");
            
            var apiResult = new APIResult();
            sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);
            if (sessionVars == null || sessionVars.Count == 0)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Session data is not available.";
                return apiResult;
            }

            var validRoles = await _common.IsValidRole(tstructData.ARMSessionId, tstructData.Roles, sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

            if(!validRoles)
            {
                apiResult.success = false;
                apiResult.data["error"] = $"User does not access to some of the roles.";
                return apiResult;
            }

            tstructData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            tstructData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                apiResult = await CheckTstructCreateAccess(tstructData);
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                apiResult = await CheckTstructEditAccess(tstructData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
                return apiResult;
            }

            if (string.IsNullOrEmpty(apiResult.error))
            {
                apiResult = await ExecuteSaveDataAPI(tstructData);
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile($"AxPut-{tstructData.TransId}");
            }

            return apiResult;

        }

        private async Task<APIResult> CheckTstructCreateAccess(AxPutData tstructData)
        {
            var result = new APIResult();
            try
            {
                bool canCreate = false;
                List<string> excludedElements = new List<string>();
                List<string> includeElements = new List<string>();
                var userPermissions = await GetCreatePermission(tstructData);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = "User does not have access to this structure.";
                    return result;
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

                var tstructValidation = await ValidateCreateJson(tstructData);

                if (!tstructValidation.IsValid)
                {
                    result.success = false;
                    if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                    result.error = string.Join(",", tstructValidation.Errors);
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

        private async Task<SQLResult> GetCreatePermission(AxPutData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            if (tstructData.Roles?.Count == 0)
                tstructData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pusername", "@proles"};
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { tstructData.Action, tstructData.TransId, tstructData.UserName.ToString(), string.Join(",", tstructData.Roles) };


                var dsResult = await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
            else {
                sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION_ORACLE.ToString();
                string[] paramName = { "@action", "@transid", "@username", "@roles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };            
                object[] paramValue = { tstructData.Action, tstructData.TransId, tstructData.UserName.ToString(), string.Join(",", tstructData.Roles) };

                var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }

                
        }

        private async Task<SQLResult> GetTstructRecordIDs(AxPutData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_TSTRUCT_GRIDRECORDIDS.ToString();
            string[] paramName = { "@transid", "@recordid", "@dcrows" };
            DbType[] paramType = { DbType.String, DbType.Int64, DbType.String };

            object[] paramValue = { tstructData.TransId, Convert.ToInt64(tstructData.RecordId), tstructData.DCRows };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<SQLResult> GetTstructDCRecordIDs(AxPutData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_TSTRUCT_DCRECORDIDS.ToString();
            string[] paramName = { "@transid", "@recordid", "@dcs" };
            DbType[] paramType = { DbType.String, DbType.Int64, DbType.String };

            object[] paramValue = { tstructData.TransId, Convert.ToInt64(tstructData.RecordId), tstructData.DCs };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<TstructDataValidationResult> ValidateCreateJson(AxPutData tstructData)
        {
            var result = new TstructDataValidationResult();

            try
            {

                if (!tstructData.SubmitData.ContainsKey("dataarray"))
                {
                    var tempSubmitData = tstructData.SubmitData;
                    tempSubmitData = AddRowNodeIfMissing(tempSubmitData);
                    tstructData.SubmitData = new Dictionary<string, object>();
                    tstructData.SubmitData.Add("dataarray", new Dictionary<string, object> { { "data", tempSubmitData } });
                }

                if (string.IsNullOrEmpty(tstructData.DataArrayJson))
                {
                    tstructData.DataArrayJson = System.Text.Json.JsonSerializer.Serialize(tstructData.SubmitData["dataarray"]);
                }

                JObject rootObject = JObject.Parse(tstructData.DataArrayJson);
                JObject dataObject = (JObject)rootObject["data"];

                if (dataObject == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid data structure: 'data' node not found");
                    return result;
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }



            return result;
        }

        private async Task<APIResult> CheckTstructEditAccess(AxPutData tstructData)
        {

            var result = new APIResult();

            try
            {

                List<string> excludedDCElements = new List<string>();
                List<string> includeDCElements = new List<string>();
                List<string> excludedElements = new List<string>();
                List<string> includeElements = new List<string>();

                var userPermissions = await GetEditPermission(tstructData);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = "User does not have access to this structure.";
                    return result;
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
                tstructData.RecordId = recordid;

                var tstructFieldsValidation = await ValidateIncludeExcludeFields(tstructData.SubmitData, excludedDCElements.Distinct().ToList(), includeDCElements.Distinct().ToList(), excludedElements.Distinct().ToList(), includeElements.Distinct().ToList());

                if (!tstructFieldsValidation.IsValid)
                {
                    result.success = false;
                    if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                    result.error = string.Join(",", tstructFieldsValidation.Errors);
                }
                else
                {
                    var tstructValidation = await TransformEditJson(tstructData);

                    if (!tstructValidation.IsValid)
                    {
                        result.success = false;
                        if (_traceService.IsTracingEnabled()) result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                        result.error = string.Join(",", tstructValidation.Errors);
                    }
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

                apiResult.error = "Error occured in 'CheckTstructEditAccess'.";

                return apiResult;
            }
            return result;
        }

        async Task<TstructDataValidationResult> ValidateIncludeExcludeFields(
        Dictionary<string, object> submitData,
        List<string> excludedDCs,
        List<string> includedDCs,
        List<string> excludedFields,
        List<string> includedFields)
        {
            var result = new TstructDataValidationResult();

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            List<string> skipFields = ["axrow_action"];

            foreach (var dc in submitData)
            {
                string dcKey = dc.Key;

                // Validate Excluded DCs
                if (!excludedDCs.Contains(none) && excludedDCs.Contains(dcKey, StringComparer.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"DC '{dcKey}' is not allowed (excluded).");
                }

                // Validate Included DCs
                if (!includedDCs.Contains(all) && !includedDCs.Contains(dcKey, StringComparer.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add($"DC '{dcKey}' is not in the list of included DCs.");
                }

                string json = System.Text.Json.JsonSerializer.Serialize(dc.Value);
                var dcValue = JToken.Parse(json);
                if (dcValue is JObject dcObject)
                {
                    bool isGrid = dcObject.Properties().Any(p => p.Name.StartsWith("row", StringComparison.OrdinalIgnoreCase));

                    if (isGrid)
                    {
                        foreach (var row in dcObject.Properties())
                        {
                            /*                     string rowJson = System.Text.Json.JsonSerializer.Serialize(row.Value);
                                                 var rowValue = JToken.Parse(rowJson);*/

                            if (row.Value is JObject rowFields)
                            {
                                foreach (var field in rowFields.Properties())
                                {
                                    if (skipFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                                        continue;

                                    await ValidateField(result, field.Name, excludedFields, includedFields, dcKey, row.Name);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var field in dcObject.Properties())
                        {
                            if (skipFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                                continue;

                            await ValidateField(result, field.Name, excludedFields, includedFields, dcKey);
                        }
                    }
                }
                else
                {
                    result.IsValid = false;
                    result.Errors.Add($"Invalid data format for DC '{dcKey}'. Expected JSON object.");
                }
            }

            return result;
        }

        async Task ValidateField(TstructDataValidationResult result, string fieldName, List<string> excludedFields, List<string> includedFields, string dcKey, string rowKey = null)
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

        private string GetDCWiseRowNumbers(Dictionary<string, object> submitData)
        {
            var results = new List<string>();

            foreach (var dc in submitData)
            {
                var dcName = dc.Key;
                var dcValue = dc.Value;

                // Serialize the object to JSON and then parse it as JToken
                string json = System.Text.Json.JsonSerializer.Serialize(dcValue);
                var token = JToken.Parse(json);

                if (token is JObject jObject)
                {
                    // Check if this is a row-based DC (has keys starting with "row")
                    var rowNumbers = jObject.Properties()
                        .Where(p => p.Name.StartsWith("row"))
                        .Select(p => p.Name.Substring(3)) // Get the number part after "row"
                        .OrderBy(n => int.Parse(n))  // Order numerically
                        .ToList();

                    if (rowNumbers.Any())
                    {
                        results.Add($"{dcName}~{string.Join(",", rowNumbers)}");
                    }
                    else
                    {
                        // For non-row DCs, just add with 0
                        results.Add($"{dcName}~0");
                    }
                }
                else
                {
                    // For simple properties, add with 0
                    results.Add($"{dcName}~0");
                }
            }

            return string.Join("|", results);
        }

        private string GetDCNames(Dictionary<string, object> submitData)
        {
            var results = new List<string>();

            foreach (var dc in submitData)
            {
                results.Add(dc.Key);
            }

            return string.Join(",", results);
        }

        //private async Task<TstructDataValidationResult> TransformEditJson(AxPutData tstructData)
        //{
        //    var result = new TstructDataValidationResult();

        //    try
        //    {
        //        tstructData.DCRows = GetDCWiseRowNumbers(tstructData.SubmitData);
        //        var dcRowData = new SQLResult();
        //        if (!string.IsNullOrEmpty(tstructData.DCRows))
        //            dcRowData = await GetTstructRecordIDs(tstructData);

        //        if (!tstructData.SubmitData.ContainsKey("dataarray"))
        //        {
        //            var tempSubmitData = tstructData.SubmitData;
        //            tempSubmitData = AddRowNodesAndRecordIdsWithDataTable(tempSubmitData, dcRowData.data); //AddRowNodeIfMissing(tempSubmitData);
        //            tstructData.SubmitData = new Dictionary<string, object>();
        //            tstructData.SubmitData.Add("dataarray", new Dictionary<string, object> { { "data", tempSubmitData } });
        //        }

        //        if (string.IsNullOrEmpty(tstructData.DataArrayJson))
        //        {
        //            tstructData.DataArrayJson = System.Text.Json.JsonSerializer.Serialize(tstructData.SubmitData["dataarray"]);
        //        }

        //        JObject rootObject = JObject.Parse(tstructData.DataArrayJson);
        //        JObject dataObject = (JObject)rootObject["data"];

        //        if (dataObject == null)
        //        {
        //            result.IsValid = false;
        //            result.Errors.Add("Invalid data structure: 'data' node not found");
        //            return result;
        //        }
        //    }
        //    catch (JsonException ex)
        //    {
        //        result.IsValid = false;
        //        result.Errors.Add($"Invalid JSON: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsValid = false;
        //        result.Errors.Add($"Validation error: {ex.Message}");
        //    }



        //    return result;
        //}

        private async Task<TstructDataValidationResult> TransformEditJson(AxPutData tstructData)
        {
            var result = new TstructDataValidationResult();

            try
            {
                tstructData.DCs = GetDCNames(tstructData.SubmitData);
                var dcRowData = new SQLResult();
                if (!string.IsNullOrEmpty(tstructData.DCs))
                    dcRowData = await GetTstructDCRecordIDs(tstructData);

                if (!tstructData.SubmitData.ContainsKey("dataarray"))
                {
                    var tempSubmitData = tstructData.SubmitData;
                    tempSubmitData = AddRowNodesAndRecordIdsWithDataTable(tempSubmitData, dcRowData.data); //AddRowNodeIfMissing(tempSubmitData);

                    if (tempSubmitData.ContainsKey("Error"))
                    {
                        result.IsValid = false;
                        result.Errors.Add(tempSubmitData["Error"].ToString());
                        return result;
                    }
                    else
                    {
                        tstructData.SubmitData = new Dictionary<string, object>();
                        tstructData.SubmitData.Add("dataarray", new Dictionary<string, object> { { "data", tempSubmitData } });
                    }
                }

                if (string.IsNullOrEmpty(tstructData.DataArrayJson))
                {
                    tstructData.DataArrayJson = System.Text.Json.JsonSerializer.Serialize(tstructData.SubmitData["dataarray"]);
                }

                JObject rootObject = JObject.Parse(tstructData.DataArrayJson);
                JObject dataObject = (JObject)rootObject["data"];

                if (dataObject == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid data structure: 'data' node not found");
                    return result;
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }



            return result;
        }

        private Dictionary<string, object> AddRowNodeIfMissing(Dictionary<string, object> jsonObj)
        {
            var updatedJson = new Dictionary<string, object>();
            var gridDcs = new List<string>();
            foreach (var kvp in jsonObj)
            {
                var tempNodeStr = System.Text.Json.JsonSerializer.Serialize(kvp.Value);
                var tempObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempNodeStr);

                // If the dictionary does not already contain "row1" as a key, wrap it inside "row1"
                if (!tempObj.ContainsKey("row1"))
                {
                    updatedJson[kvp.Key] = new Dictionary<string, object> { { "row1", tempObj } };
                }
                else
                {
                    gridDcs.Add(kvp.Key);
                    updatedJson[kvp.Key] = tempObj; // Keep as is if "row1" exists
                }

            }

            var changedRows = new Dictionary<string, string>();
            foreach (var gridDc in gridDcs)
            {
                changedRows.Add(gridDc, "*");
            }

            updatedJson["changedrows"] = changedRows;

            return updatedJson;
        }

        //private Dictionary<string, object> AddRowNodesAndRecordIdsWithDataTable(Dictionary<string, object> submitData, DataTable recordIdTable)
        //{
        //    var updatedJson = new Dictionary<string, object>();

        //    // Create a lookup dictionary from DataTable
        //    var recordIdMap = new Dictionary<string, string>();
        //    foreach (DataRow row in recordIdTable.Rows)
        //    {
        //        var dcname = row["dcname"].ToString().ToLower();
        //        var rowno = Convert.ToInt32(row["rowno"]);
        //        var recordid = row["recordid"].ToString();

        //        var key = $"{dcname}_row{rowno}";
        //        recordIdMap[key] = recordid;
        //    }

        //    foreach (var dc in submitData)
        //    {
        //        var tempNodeStr = System.Text.Json.JsonSerializer.Serialize(dc.Value);
        //        var dcContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempNodeStr);

        //        bool isGrid = dcContent.Keys.Count > 0 && dcContent.Keys.First().StartsWith("row");

        //        if (!isGrid)
        //        {
        //            var rowKey = "row0";
        //            var dcNameRowKey = $"{dc.Key.ToLower()}_row0";
        //            var recid = recordIdMap.ContainsKey(dcNameRowKey) ? recordIdMap[dcNameRowKey] : "0";

        //            var newRow = new Dictionary<string, object>(dcContent)
        //            {
        //                ["axp_recid"] = recid
        //            };

        //            updatedJson[dc.Key] = new Dictionary<string, object>
        //            {
        //                { rowKey, newRow }
        //            };
        //        }
        //        else
        //        {
        //            var updatedRows = new Dictionary<string, object>();

        //            foreach (var row in dcContent)
        //            {
        //                var rowKey = row.Key;
        //                var rowNodeStr = System.Text.Json.JsonSerializer.Serialize(row.Value);
        //                var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(rowNodeStr);
        //                var rowIndex = int.Parse(rowKey.Replace("row", ""));
        //                var dcNameRowKey = $"{dc.Key.ToLower()}_row{rowIndex}";
        //                var recid = recordIdMap.ContainsKey(dcNameRowKey) ? recordIdMap[dcNameRowKey] : "0";

        //                if (!rowData.ContainsKey("axp_recid"))
        //                {
        //                    rowData["axp_recid"] = recid;
        //                }

        //                updatedRows[rowKey] = rowData;
        //            }

        //            updatedJson[dc.Key] = updatedRows;
        //        }
        //    }

        //    // Apply deletion/edit logic on updatedJson
        //    return ProcessFinalData(updatedJson);
        //}


        private Dictionary<string, object> AddRowNodesAndRecordIdsWithDataTable(Dictionary<string, object> submitData, DataTable recordIdTable)
        {
            var updatedJson = new Dictionary<string, object>();

            //// Create a lookup dictionary from DataTable
            //var recordIdMap = new Dictionary<string, string>();
            //foreach (DataRow row in recordIdTable.Rows)
            //{
            //    var dcname = row["dcname"].ToString().ToLower();
            //    var rowno = Convert.ToInt32(row["rowno"]);
            //    var recordid = row["recordid"].ToString();

            //    var key = $"{dcname}_row{rowno}";
            //    recordIdMap[key] = recordid;
            //}

            Dictionary<string, int> dcwiseRowCount = new Dictionary<string, int>();
            var getMaxRowNo = recordIdTable.AsEnumerable().GroupBy(row => row["dcname"].ToString()).ToDictionary(g => g.Key, g => g.Max(row => Convert.ToInt32(row["rowno"])));

            var errorStrs = new StringBuilder();
            foreach (var dc in submitData)
            {
                var tempNodeStr = System.Text.Json.JsonSerializer.Serialize(dc.Value);
                var dcContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempNodeStr);

                bool isGrid = dcContent.Keys.Count > 0 && dcContent.Keys.First().StartsWith("row");


                if (!isGrid)
                {
                    var rowKey = "row0";
                    var dcNameRowKey = $"{dc.Key.ToLower()}_row0";
                     var matchingRow = recordIdTable.AsEnumerable().FirstOrDefault(row => row["dcname"]?.ToString() == dc.Key);
                    string recid = "0";
                    if (matchingRow != null)
                        recid = matchingRow["recordid"].ToString();
                    
                    var newRow = new Dictionary<string, object>(dcContent)
                    {
                        ["axp_recid"] = recid
                    };

                    updatedJson[dc.Key] = new Dictionary<string, object>
                    {
                        { rowKey, newRow }
                    };
                }
                else
                {
                    var updatedRows = new Dictionary<string, object>();

                    foreach (var row in dcContent)
                    {
                        var rowKey = row.Key;
                        var rowNodeStr = System.Text.Json.JsonSerializer.Serialize(row.Value);
                        var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(rowNodeStr);
                        
                        if (!rowData.ContainsKey("axp_recid"))
                        {
                            rowData["axp_recid"] = "0";
                        }

                        if (rowData["axp_recid"].ToString() == "0")
                        {
                            if (getMaxRowNo.ContainsKey(dc.Key))
                            {
                                getMaxRowNo[dc.Key] = getMaxRowNo[dc.Key] + 1;                                
                            }
                            else
                            {
                                getMaxRowNo.Add(dc.Key, 1);                                
                            }
                            rowKey = $"row{getMaxRowNo[dc.Key].ToString()}";
                        }
                        else
                        {
                            var matchingRow = recordIdTable.AsEnumerable().FirstOrDefault(row => row["dcname"]?.ToString() == dc.Key && row["recordid"]?.ToString() == rowData["axp_recid"].ToString());
                            if (matchingRow == null)
                            {
                                errorStrs.Append($"Invlaid recordid '{rowData["axp_recid"]}' passed in '{dc.Key}'. ");
                            }
                            else
                            {
                                rowKey = $"row{matchingRow["rowno"].ToString()}";
                            }

                        }
                        updatedRows[rowKey] = rowData;
                    }

                    updatedJson[dc.Key] = updatedRows;
                }
            }

            if (errorStrs != null && !string.IsNullOrEmpty(errorStrs.ToString()))
            {
                submitData.Add("Error", errorStrs.ToString());
                return submitData;
            }
            else
            {
                // Apply deletion/edit logic on updatedJson
                return ProcessFinalData(updatedJson);
            }
        }

        private Dictionary<string, object> ProcessFinalData(Dictionary<string, object> submitData)
        {
            var result = new Dictionary<string, object>();
            var delRows = new Dictionary<string, object>();
            var changedRows = new Dictionary<string, string>();

            var finalSubmitData = new Dictionary<string, object>();
            var allDeletedEntries = new List<(int rowIndex, int dcNumber, string dcName, string recid)>();

            var deletedIndicesByDc = new Dictionary<string, List<int>>();

            foreach (var dcPair in submitData)
            {
                string dcName = dcPair.Key;
                int dcNumber = int.Parse(dcName.Replace("dc", ""));
                var dcContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(System.Text.Json.JsonSerializer.Serialize(dcPair.Value));

                var keptRows = new List<(int originalIndex, Dictionary<string, object> rowData, bool isEdited)>();

                foreach (var row in dcContent)
                {
                    var rowKey = row.Key;
                    var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(System.Text.Json.JsonSerializer.Serialize(row.Value));
                    int rowIndex = int.Parse(rowKey.Replace("row", ""));

                    if (rowData.TryGetValue("axrow_action", out var action) && action.ToString().ToLower() == "delete")
                    {
                        string recid = rowData.ContainsKey("axp_recid") ? rowData["axp_recid"].ToString() : $"row{rowIndex}id";
                        allDeletedEntries.Add((rowIndex, dcNumber, dcName, recid));

                        if (!deletedIndicesByDc.ContainsKey(dcName))
                            deletedIndicesByDc[dcName] = new List<int>();

                        deletedIndicesByDc[dcName].Add(rowIndex);
                        continue;
                    }

                    bool isEdited = false;
                    if (rowData.TryGetValue("axrow_action", out var editAction) && editAction.ToString().ToLower() == "edit")
                    {
                        rowData.Remove("axrow_action");
                        isEdited = true;
                    }

                    if (rowData.TryGetValue("axrow_action", out var createAction) && createAction.ToString().ToLower() == "create")
                    {
                        rowData.Remove("axrow_action");
                    }

                    keptRows.Add((rowIndex, rowData, isEdited));
                }

                if (keptRows.Any())
                {
                    var newDcObj = new Dictionary<string, object>();
                    keptRows = keptRows.OrderBy(k => k.originalIndex).ToList();
                    var deletedIndices = deletedIndicesByDc.ContainsKey(dcName) ? deletedIndicesByDc[dcName] : new List<int>();

                    foreach (var (originalIndex, rowData, isEdited) in keptRows)
                    {
                        int deletedBefore = deletedIndices.Count(idx => idx < originalIndex);
                        int newIndex = originalIndex - deletedBefore;
                        if (newIndex == 0) newIndex = 1;
                        newDcObj[$"row{newIndex}"] = rowData;

                        if (!changedRows.ContainsKey(dcName))
                        {
                            changedRows[dcName] = newIndex.ToString();
                        }
                        else
                        {
                            changedRows[dcName] += $",{newIndex.ToString()}";
                        }
                    }

                    finalSubmitData[dcName] = newDcObj;
                }
            }

            //var sortedDeleted = allDeletedEntries.OrderByDescending(x => x.rowIndex).ToList();
            //delRows["deletedrno"] = string.Join(",", sortedDeleted.Select(x => x.rowIndex));
            //delRows["deleteddcno"] = string.Join(",", sortedDeleted.Select(x => x.dcNumber));

            //var dcGroups = sortedDeleted.GroupBy(x => x.dcName);
            //foreach (var group in dcGroups)
            //{
            //    string recidKey = $"axp_recid{group.Key.Replace("dc", "")}";
            //    delRows[group.Key] = new Dictionary<string, object>
            //    {
            //        [recidKey] = group.Select(x => x.recid).ToList()
            //    };
            //}

            // Step 1: Group by dcName (keep group ordering as natural), then inside each group, order by descending rowIndex
            var dcGroups = allDeletedEntries
                .GroupBy(x => x.dcName)
                .ToList(); // Keep original group order

            // Step 2: Prepare deletedrno and deleteddcno based on groupwise descending order
            var deletedRowNos = new List<int>();
            var deletedDcNos = new List<int>();

            foreach (var group in dcGroups)
            {
                var rowsDesc = group.OrderByDescending(x => x.rowIndex).ToList();

                deletedRowNos.AddRange(rowsDesc.Select(x => x.rowIndex));
                deletedDcNos.AddRange(rowsDesc.Select(x => x.dcNumber));

                string recidKey = $"axp_recid{group.Key.Replace("dc", "")}";

                delRows[group.Key] = new Dictionary<string, object>
                {
                    [recidKey] = rowsDesc.Select(x => x.recid).ToList()
                };
            }

            delRows["deletedrno"] = string.Join(",", deletedRowNos);
            delRows["deleteddcno"] = string.Join(",", deletedDcNos);


            result["SubmitData"] = finalSubmitData;
            if (delRows.Count > 0) result["delrows"] = delRows;
            if (changedRows.Count > 0) result["changedrows"] = changedRows;

            return result;
        }

        private async Task<APIResult> ExecuteSaveDataAPI(AxPutData tstructData)
        {            
            try
            {
                APIResult result = new APIResult();
                if (tstructData.SubmitData is null)
                {
                    result.error = "Input Json is not having 'submitdata' node.";
                    return result;
                }
                var sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);

                string armWebScriptsURL = sessionVars[SESSION_DATA.SCRIPTSURL.ToString()].ToString();

                string apiUrl = armWebScriptsURL + "/ASBTStructRest.dll/datasnap/rest/TASBTstruct/savedata";
                var api = new API();
                DateTime startTime = DateTime.Now;

                JObject submitDataJson = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(tstructData));

                var saveDataJson = new JObject
                {
                    ["savedata"] = new JObject
                    {
                        ["cachedsave"] = "false",
                        ["axpapp"] = submitDataJson["Project"].ToString(),
                        ["transid"] = submitDataJson["TransId"].ToString(),
                        ["s"] = submitDataJson["ARMSessionId"].ToString(),
                        ["username"] = submitDataJson["UserName"].ToString(),
                        ["password"] = sessionVars[SESSION_DATA.PASSWORD.ToString()].ToString(),
                        ["changedrows"] = (submitDataJson["SubmitData"]["dataarray"]["data"]?["changedrows"] != null ? (JObject)submitDataJson["SubmitData"]["dataarray"]["data"]?["changedrows"] : new JObject()),
                        ["delrows"] = (submitDataJson["SubmitData"]["dataarray"]["data"]?["delrows"] != null ? (JObject)submitDataJson["SubmitData"]["dataarray"]["data"]?["delrows"] : new JObject()),
                        ["trace"] = submitDataJson["Trace"].ToString() ?? "",
                        ["recordid"] = submitDataJson["RecordId"].ToString() ?? "0",
                        ["forpaybooks"] = ((_config["AppConfig:Client"]?.IndexOf("PAYBOOKS", StringComparison.OrdinalIgnoreCase) >= 0) ? "true" : "false"),
                        ["recdata"] = new JArray()
                    },
                    ["axprops"] = JObject.Parse(sessionVars[SESSION_DATA.AXPROPS.ToString()].ToString()),
                    ["globalvars"] = JObject.Parse(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString()),
                    ["uservars"] = JObject.Parse(sessionVars[SESSION_DATA.USERVARS.ToString()].ToString()),
                    [submitDataJson["Project"].ToString()] = JObject.Parse(sessionVars[SESSION_DATA.AXAPPS.ToString()].ToString()),
                };

                if (submitDataJson["RecordId"] == null || submitDataJson["RecordId"]?.ToString() == "")
                {
                    submitDataJson["RecordId"] = "0";
                }

                JObject data = (JObject)(submitDataJson["SubmitData"]["dataarray"]["data"]?["SubmitData"] ?? submitDataJson["SubmitData"]["dataarray"]["data"]);

                var recdataArray = (JArray)saveDataJson["savedata"]["recdata"];

                foreach (var dc in data.Properties())
                {
                    if (dc.Name.StartsWith("dc"))
                    {
                        JObject dcData = (JObject)dc.Value;
                        var dcNo = dc.Name.Substring(2);
                        var record = new JObject();
                        var recidArray = new JArray();

                        foreach (var row in dcData.Properties())
                        {
                            var tempRowName = row.Name.ToString();
                            if (tempRowName == "row0") tempRowName = "row1";
                            var rowNo = Convert.ToInt16(tempRowName.Substring(3));
                            if (row.Name.StartsWith("row"))
                            {
                                JObject rowData = (JObject)row.Value;

                                string recid = rowData["axp_recid"]?.ToString() ?? "0";

                                // Remove 'axp_recid' from columns
                                rowData.Remove("axp_recid");

                                var entry = new JObject
                                {
                                    ["rowno"] = rowNo.ToString("D3"),
                                    ["text"] = recid,
                                    ["columns"] = rowData
                                };

                                recidArray.Add(entry);
                            }
                        }

                        record[$"axp_recid{dcNo}"] = recidArray;
                        recdataArray.Add(record);
                    }
                }


                var saveObj = new
                {
                    _parameters = new List<object> { saveDataJson }
                };
                var apiInput = JsonConvert.SerializeObject(saveObj);

                var armResult = await api.POSTData(apiUrl, apiInput, "application/json");
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"API URL - {apiUrl}");
                    _traceService.LogMessage($"API JSON - {apiInput}");
                    _traceService.LogMessage($"API Result - {JsonConvert.SerializeObject(armResult)}");
                }
                DateTime endTime = DateTime.Now;
                if (_traceService.IsTracingEnabled())
                {
                    TimeSpan duration = endTime - startTime;
                    _traceService.LogMessage($"API Call - Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
                }

                return ParseAxpertRestAPIResult(armResult);
            }
            catch(Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'savedata' API call.";

                return apiResult;
            }
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

        private APIResult ParseAxpertRestAPIResult(ARMResult armResult)
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

            if (!(resultJsonObj?["status"]?.ToString().ToLower() == "true" || resultJsonObj?["status"]?.ToString().ToLower() == "success"))
            {
                if (!string.IsNullOrEmpty(resultJsonObj?["result"]?.ToString()))
                    result.error = resultJsonObj?["result"]?.ToString();
                return result;
            }


            //if (!(resultJsonObj?["status"]?.ToString().ToLower() == "true" || resultJsonObj?["status"]?.ToString().ToLower() == "success") || resultJsonObj["error"] != null)
            //{

            //    if (!string.IsNullOrEmpty(resultJsonObj?["result"]?.ToString()))
            //        result.error = resultJsonObj?["result"]?.ToString();

            //    if (resultJsonObj["error"] != null)
            //    {
            //        JObject errorJsonObj = JObject.Parse(resultJsonObj["error"].ToString());

            //        result.error = errorJsonObj?["msg"]?.ToString() ;
            //    }
            //    return result;
            //}
            foreach (var property in resultJsonObj.Properties())
            {
                if (property.Name.ToLower() != "status")
                    result.data.Add(property.Name, property.Value);
            }
            return result;

        }




        private async Task<SQLResult> GetEditPermission(AxPutData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            
            if (tstructData.Roles?.Count == 0)
                tstructData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXPUT_EDITPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { tstructData.Action, tstructData.TransId, tstructData.KeyField, $"'{tstructData.KeyValue}'", tstructData.UserName, string.Join(",", tstructData.Roles) };

                var dsResult = await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.GET_AXPUT_EDITPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXPUT_EDITPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { tstructData.Action, tstructData.TransId, tstructData.KeyField, $"'{tstructData.KeyValue}'", tstructData.UserName, string.Join(",", tstructData.Roles) };

                var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
                return dsResult;
            }
        }


        public async Task<APIResult> AxGetEditableFlag(AxEditFlag tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing, true);

            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

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

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                apiResult = await GetCreateFlag(tstructData);
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                apiResult = await GetEditFlag(tstructData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile($"AxGetPermission-{tstructData.Action}-{tstructData.TransId}");
            }

            return apiResult;
        }

        private async Task<APIResult> GetEditFlag(AxEditFlag tstructData)
        {

            var result = new APIResult();

            try
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxEditFlag, AxPutData>().ForMember(dest => dest.Roles, opt => opt.Ignore()));
                var mapper = config.CreateMapper();
                AxPutData data = mapper.Map<AxPutData>(tstructData);
                data.Roles = tstructData.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
                var userPermissions = await GetEditPermission(data);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = "User does not have access to this structure.";
                    return result;
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

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"RecordId - {recordid}");
                }

                result.data["RecordId"] = recordid;
                result.data["AllowEdit"] = "T";
            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'GetEditFlag'.";

                return apiResult;
            }
            return result;
        }

        private async Task<APIResult> GetCreateFlag(AxEditFlag tstructData)
        {
            var cacheKey = _common.GenerateRedisKeyString(new string[] { tstructData.SchemaName ?? tstructData.Project, tstructData.TransId, "ARM-AX-CREATE-PERMISSION", tstructData.UserName, string.Join(",", tstructData.Roles) ?? "All" });
            var result = new APIResult();
            try
            {
                if (tstructData.CachePermissions != false)
                {
                    var cachedResult = await _redis.StringGetAsync(cacheKey);
                    if (!string.IsNullOrEmpty(cachedResult))
                    {
                        if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Cache - Access Loaded from cache", cachedResult);
                        return JsonConvert.DeserializeObject<APIResult>(cachedResult);
                    }
                }


                bool canCreate = false;
                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxEditFlag, AxPutData>().ForMember(dest => dest.Roles, opt => opt.Ignore()));
                var mapper = config.CreateMapper();
                AxPutData data = mapper.Map<AxPutData>(tstructData);
                data.Roles = tstructData.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };

                var userPermissions = await GetCreatePermission(data);

                if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
                {
                    result.success = false;
                    result.error = "User does not have access to this structure.";
                    return result;
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
                result.data["AllowCreate"] = "T";
            }
            catch (Exception ex)
            {
                var apiResult = new APIResult();
                apiResult.success = false;

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }

                apiResult.error = "Error occured in 'GetCreateFlag'.";

                return apiResult;
            }

            if (tstructData.CachePermissions != false)
                await _redis.StringSetAsync(cacheKey, JsonConvert.SerializeObject(result));

            return result;
        }


    }
}
