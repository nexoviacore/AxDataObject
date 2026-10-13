using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;
using System.Data;
using System.Text.RegularExpressions;

namespace AxList
{
    public class ADSDataService : IADSDataService
    {
        private readonly IADSDataRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;        

        public ADSDataService(IADSDataRepository repo, IARMLogTraceService traceService, Utils common)
        {
            _repo = repo;
            _traceService = traceService;
            _common = common;
        }
        
        public async Task<ARMResult> GetDataFromDatasource(ADSDataRequest request)
        {
            if (request.Trace)
            {
                _traceService.EnableTracing(true);
                _traceService.LogMessage("GetDataFromDatasource API call started.");
                _traceService.LogMessage($"Start Time: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(request)}");
            }

            var result = new ARMResult();
            try
            {
                Dictionary<string, object> sessionVars = await _common.GetSessionData(request.ARMSessionId);

                if (sessionVars == null || sessionVars.Count == 0)
                {
                    return new ARMResult(false, "Session data is not available.");
                }

                // Extract session information
                request.AppName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                request.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                request.SchemaName = sessionVars[SESSION_DATA.SCHEMANAME.ToString()].ToString();
                request.Roles = sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString();
                
                result = await GetADSDataAsync(request);                
            }
            catch (Exception ex)
            {
                result.result["success"] = false;
                if(ex.Message.IndexOf("is missing in input params.") > -1)
                    result.result["message"] = ex.Message;
                else
                    result.result["message"] = "Error occurred. Please check with administrator.";

                if (request.Trace) _traceService.LogError("Error in GetADSData : " + ex.Message + ex.StackTrace + ex.InnerException);
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }

            if (request.Trace) result.result["Trace"] = await _traceService.GetTraceFile($"GetDataFromDatasource-{request.UserName}");

            return result;

        }

        public async Task<ARMResult> GetADSDataAsync(ADSDataRequest request)
        {
            var result = new ARMResult();
            
            var adsDataSource = await _repo.GetADSDefinitionAsync(request.SchemaName, request.DataSource);
            if (adsDataSource == null)
            {
                return new ARMResult(false, $"ADS definition '{request.DataSource}' not found.");
            }

            // Validate user access
            if (!await _repo.ValidateUserAccess(request.Roles, adsDataSource.Roles))
            {
                return new ARMResult(false, "User doesn't have access to this ADS.");
            }

            return await GetADSDataWithCachingAsync(adsDataSource, request, request.SchemaName);
        }

        private async Task<ARMResult> GetADSDataWithCachingAsync(ADSDataSource dataSource, ADSDataRequest request, string schemaName)
        {            
            var sqlParams = _common.ValidateSQLAndExtractParams(dataSource.SQLText, dataSource.SQLParams, _common._globalVars, request.SQLParams);
            string dataCacheKey = "";
            var result = new ARMResult();
            if (dataSource.CacheData == "T" && !request.RefreshCache)
            {
                dataCacheKey = _repo.GenerateDataCacheKey(schemaName, request.DataSource, sqlParams);

                // Try to get data from cache
                string cachedData = await _repo.GetADSDataFromCacheAsync(dataCacheKey);
                
                if (cachedData != null)
                {
                    result.result["success"] = true;
                    result.result["name"] = request.DataSource;
                    result.result["data"] = JsonConvert.DeserializeObject<System.Data.DataTable>(cachedData);
                    return result;
                }
            }

            // Data not in cache, get from database
            var dbData = await _repo.GetADSDataFromDB(dataSource.SQLText, sqlParams);
            if (dbData != null && !string.IsNullOrEmpty(dbData.error))
            {
                result.result["success"] = false;
                result.result["name"] = request.DataSource;
                result.result["error"] = dbData.error;
                result.result["message"] = dbData.error;                
                return result;
            }            

            if (!string.IsNullOrEmpty(dataSource.EncryptedFlds))
            {
                var encryptedFields = dataSource.EncryptedFlds.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                if (encryptedFields.Count > 0)
                    dbData.data = AXPEncryptData.CallDecryptDataTable(dbData.data, encryptedFields);
            }

            if (dataSource.CacheData == "T")            // Save data to cache
            {
                if(string.IsNullOrEmpty(dataCacheKey))
                    dataCacheKey = _repo.GenerateDataCacheKey(schemaName, request.DataSource, sqlParams);

                var cacheInterval = dataSource.CacheInterval.Split(" ")[0];
                if (string.IsNullOrEmpty(cacheInterval))
                    cacheInterval = "1"; //Setting 1 hour
                await _repo.SaveADSDataToCacheAsync(dataCacheKey, dbData.data, Convert.ToInt32(cacheInterval) * 60);
            }

            result.result["success"] = true;
            result.result["name"] = request.DataSource;
            result.result["data"] = dbData.data;

            return result;
        }

        public async Task<APIResult> GetADSDataForAxList(AxList listData, string adsName, ADSDataRequest adsDataRequest = null)
        {
            var accessResult = new APIResult();
            var apiResult = new APIResult();
            var adsDataSource = await _repo.GetADSDefinitionAsync(adsDataRequest.SchemaName, adsDataRequest.DataSource);
            if (adsDataSource == null)
            {
                apiResult.success = false; 
                apiResult.data["adsname"] = adsName;
                apiResult.data["error"] = $"ADS definition '{adsDataRequest.DataSource}' not found.";
                return apiResult;
            }

            string adsSQL = "";

            if (!listData.ADS)
            {
                // Validate user access based on ADS Permission.
                accessResult = await GetADSPermission(listData.TransId, adsName, listData.UserName, listData.Roles, listData.KeyField, listData.KeyValue);

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage($"GetADSPermission Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                }

                if (!string.IsNullOrEmpty(accessResult.error))
                {
                    accessResult.data["adsname"] = adsName;
                    return accessResult;
                }

                var filterCond = "NA";
                if (!string.IsNullOrEmpty(listData.TransId))
                    filterCond = accessResult.data["FilterCond"]?.ToString() ?? "NA";

                adsSQL = await _repo.GetADSSQLForAxList(listData, adsName, filterCond);
                if (string.IsNullOrEmpty(adsSQL))
                {
                    apiResult.success = false;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["error"] = $"Not able to load ADS {adsName} sql.";
                    return apiResult;
                }
            }
            else
            {
                adsSQL = adsDataSource.SQLText;

                // Validate user access based on roles
                if (!await _repo.ValidateUserAccess(listData.RolesStr, adsDataSource.Roles))
                {
                    return new APIResult(false, "User doesn't have access to this ADS.");
                }
            }

            adsDataSource.SQLText = ApplyDynamicFiltersSortingPagination(adsSQL, listData, false); //Do pagination, sorting, orderby logics.

            var adsResult = await GetADSDataWithCachingAsync(adsDataSource, adsDataRequest, adsDataRequest.SchemaName);

            if (adsResult.result.ContainsKey("success") && adsResult.result["success"].ToString() == true.ToString() && adsResult.result.ContainsKey("data") && adsResult.result["data"] != null)
            {
                apiResult.success = true;
                apiResult.data["adsname"] = adsName;

                if (accessResult == null || accessResult.data == null || accessResult.data.Count == 0)
                {
                    accessResult = new APIResult();
                    accessResult.data["ExcludedFields"] = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
                    accessResult.data["IncludedFields"] = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
                    accessResult.data["MaskedFields"] = "";
                    accessResult.data["FilterCond"] = "";
                }

                apiResult.data["data"] = await _common.HandleIncludeExcludeCols((DataTable)adsResult.result["data"], accessResult);

                if (listData.GetAllRecordsCount)
                {
                    var sqlParams = _common.ValidateSQLAndExtractParams(adsSQL, adsDataSource.SQLParams, _common._globalVars, adsDataRequest.SQLParams);
                    var countSql = ApplyDynamicFiltersSortingPagination(adsSQL, listData, true);
                    var totalRecords = await _repo.GetADSDataTotalCountFromDB(countSql, sqlParams);
                    apiResult.data["totalrecords"] = totalRecords.ToString();
                }
            }
            else
            {
                apiResult.success = false;
                apiResult.data["adsname"] = adsName;
                if (adsResult.result.ContainsKey("message"))
                    apiResult.data["error"] = adsResult.result["message"];
            }

            //if (_traceService.IsTracingEnabled())
            //{
            //    _traceService.LogMessage($"GetADSDataFromDB Result JSON - {JsonConvert.SerializeObject(adsResult)}");
            //}

            return apiResult;


        }

        public async Task<APIResult> GetADSPermission(string transId, string adsName, string userName, List<string> roles, string keyField, string keyValue)
        {
            var result = new APIResult();
            var userPermissions = await _repo.GetADSDataViewPermissionFromDB(transId, adsName, userName, roles, keyField, keyValue);

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

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();

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

            return result;
        }

        public string ApplyDynamicFiltersSortingPagination(string sql, AxList listData, bool getTotalRecordsCount = false)
        {
            int pageNo = listData.PageNo ?? 1;
            int pageSize = listData.PageSize ?? 100;

            if (listData.PageSize != null && listData.PageSize == 0)
            {
                pageNo = 0;
                pageSize = 0;
            }

            string tempSQL = sql.ToLower();

            if (tempSQL.IndexOf("--ax_select_columns") > -1)
            {
                string selectColumns = "*";
                if (listData.Select_Columns != null && listData.Select_Columns.Count > 0)
                    selectColumns = string.Join(",", listData.Select_Columns);

                sql = Regex.Replace(sql, Regex.Escape("--ax_select_columns"), $" {selectColumns} ", RegexOptions.IgnoreCase);
            }

            if (tempSQL.IndexOf("--ax_aggregations") > -1)
            {
                string aggregations = "";
                if (listData.Aggregations != null && listData.Aggregations.Count > 0)
                {
                    aggregations = "," + string.Join(", ", listData.Aggregations.Select(kv => $"{kv.Value} AS {kv.Key}"));
                }

                sql = Regex.Replace(sql, Regex.Escape("--ax_aggregations"), $" {aggregations} ", RegexOptions.IgnoreCase);
            }

            if ((tempSQL.IndexOf("--ax_ui_filter_withwhere") > -1 || tempSQL.IndexOf("--ax_ui_filter") > -1) && listData.Filters != null && listData.Filters.Count > 0)
            {
                List<string> conditions = new();

                // Filtering Logic
                foreach (var filter in listData.Filters)
                {
                    string fldname = filter["fldname"].ToString();
                    string datatype = filter["datatype"].ToString().ToUpper();

                    switch (datatype)
                    {
                        case "TEXT":
                            {
                                string value = filter["value"].ToString().Replace("'", "''");
                                string condition = filter.ContainsKey("condition") ? filter["condition"].ToString().ToUpper() : "EQUALS";

                                if (condition == "STARTSWITH")
                                    conditions.Add($" lower({fldname}) LIKE '{value.ToLower()}%' ");
                                else if (condition == "ENDSWITH")
                                    conditions.Add($" lower({fldname}) LIKE '%{value.ToLower()}' ");
                                else if (condition == "CONTAINS")
                                    conditions.Add($" lower({fldname}) LIKE '%{value.ToLower()}%' ");
                                else if (condition == "NOT CONTAINS")
                                    conditions.Add($" lower({fldname}) NOT LIKE '%{value.ToLower()}%' ");
                                else if (condition == "NOT EQUALS")
                                    conditions.Add($" lower({fldname}) != '{value.ToLower()}' ");
                                else
                                    conditions.Add($" lower({fldname}) = '{value.ToLower()}' ");
                            }
                            break;

                        case "DATE":
                        case "TIMESTAMP":
                            if (_common._dbVars == null || _common._dbVars.Count == 0)
                            {
                                var dbConfig = _common.GetDBConfigurationSync(_common._appName);
                                _common._dbVars = _common.GetDBVarsList(dbConfig["DBType"]);
                            }

                            List<string> dateConditions = new();
                            if (filter.ContainsKey("from"))
                            {
                                string dbDateFrom = _common.ConvertClientDateToDBDateFormat(datatype, filter["from"].ToString(), listData.AxClient_DateFormat);
                                dateConditions.Add($" {fldname} >= {_common._dbVars["dbdatestring"].Replace("$DATE$", "'" + dbDateFrom + "'")} ");
                            }
                            if (filter.ContainsKey("to"))
                            {
                                string dbDateTo = _common.ConvertClientDateToDBDateFormat(datatype, filter["to"].ToString(), listData.AxClient_DateFormat);
                                dateConditions.Add($" {fldname} <= {_common._dbVars["dbdatestring"].Replace("$DATE$", "'" + dbDateTo + "'")} ");
                            }
                            if (dateConditions.Any())
                                conditions.Add(string.Join(" AND ", dateConditions));
                            break;

                        case "DROPDOWN":
                            {
                                var dropdownValues = System.Text.Json.JsonSerializer.Deserialize<List<string>>(System.Text.Json.JsonSerializer.Serialize(filter["value"]));
                                string condition = filter.ContainsKey("condition") ? filter["condition"].ToString().ToUpper() : "IN";

                                if (string.IsNullOrEmpty(condition)) condition = "IN";

                                if (dropdownValues.Count > 0)
                                {
                                    var formatted = dropdownValues.Select(v => $"'{v.ToString().ToLower().Replace("'", "''")}'");
                                    conditions.Add($" lower({fldname}) {condition} ({string.Join(",", formatted)}) ");
                                }
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

                    sql = Regex.Replace(sql, Regex.Escape("--ax_ui_filter_withwhere"), whereClause, RegexOptions.IgnoreCase);
                    sql = Regex.Replace(sql, Regex.Escape("--ax_ui_filter"), andClause, RegexOptions.IgnoreCase);
                }
            }

            if (tempSQL.IndexOf("--ax_groupby") > -1)
            {
                if (listData.GroupBy_Columns != null && listData.GroupBy_Columns.Count > 0)
                {
                    string groupByColumns = string.Join(",", listData.GroupBy_Columns);

                    sql = Regex.Replace(sql, Regex.Escape("--ax_groupby"), $" group by {groupByColumns} ", RegexOptions.IgnoreCase);
                }
                else
                    sql = Regex.Replace(sql, Regex.Escape("--ax_groupby"), $" ", RegexOptions.IgnoreCase);
            }

            if (!getTotalRecordsCount)
            {
                if (tempSQL.IndexOf("--ax_orderby") > -1 && listData.Sorting != null && listData.Sorting.Count > 0)
                {
                    string orderByClause = string.Join(" , ", listData.Sorting.Select(s =>
                        $"{s["fldname"]} {s["sort_order"].ToUpper()}"));

                    sql = Regex.Replace(sql, Regex.Escape("--ax_orderby_append"), $" , {orderByClause} ", RegexOptions.IgnoreCase);
                    sql = Regex.Replace(sql, Regex.Escape("--ax_orderby"), $" ORDER BY {orderByClause} ", RegexOptions.IgnoreCase);
                }
            }
            else
            {
                sql = Regex.Replace(sql, Regex.Escape("--ax_orderby_append"), "", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, Regex.Escape("--ax_orderby"), "", RegexOptions.IgnoreCase);
            }

            if (!getTotalRecordsCount)
            {
                if (tempSQL.IndexOf("--ax_pagination") > -1 && Convert.ToInt32(pageNo) > 0 && Convert.ToInt32(pageSize) > 0)
                {
                    var dbSettings = _common.GetDBConfigurationSync(_common._appName);
                    int pNo = Convert.ToInt32(pageNo);
                    int pSize = Convert.ToInt32(pageSize);

                    if (dbSettings["DBType"] == Constants.DBTYPE.POSTGRE.ToString())
                    {
                        int offset = (pNo - 1) * pSize;
                        sql = Regex.Replace(sql, Regex.Escape("--ax_pagination"),
                            $" LIMIT {pSize} OFFSET {offset} ", RegexOptions.IgnoreCase);
                    }
                    else if (dbSettings["DBType"] == Constants.DBTYPE.ORACLE.ToString())
                    {
                        int startRow = ((pNo - 1) * pSize) + 1;
                        int endRow = pNo * pSize;

                        sql = Regex.Replace(sql, Regex.Escape("--ax_pagination"), $" ", RegexOptions.IgnoreCase);

                        sql = $"select  *  from (select  a.* , rownum axrnum_forpagination  from ( {sql} ) a  ) where axrnum_forpagination between {startRow} and {endRow} ";
                    }
                    else if (dbSettings["DBType"] == Constants.DBTYPE.MSSQL.ToString())
                    {
                        int offset = (pNo - 1) * pSize;
                        if (listData.Sorting != null && listData.Sorting.Count > 0)
                            sql = Regex.Replace(sql, Regex.Escape("--ax_pagination"), $" OFFSET {offset} ROWS FETCH NEXT {pSize} ROWS ONLY", RegexOptions.IgnoreCase);
                        else
                            sql = Regex.Replace(sql, Regex.Escape("--ax_pagination"), $" ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pSize} ROWS ONLY", RegexOptions.IgnoreCase);
                    }
                }
            }
            else
            {
                sql = Regex.Replace(sql, Regex.Escape("--ax_pagination"), "", RegexOptions.IgnoreCase);

                var dbSettings = _common.GetDBConfigurationSync(_common._appName);
                if (dbSettings["DBType"] == Constants.DBTYPE.POSTGRE.ToString() || dbSettings["DBType"] == Constants.DBTYPE.MSSQL.ToString())
                {
                    sql = $"SELECT COUNT(*) FROM (\n {sql.Trim()} \n) AS count_subquery";
                }
                else if (dbSettings["DBType"] == Constants.DBTYPE.ORACLE.ToString())
                {
                    sql = $"SELECT COUNT(*) FROM (\n {sql.Trim()} \n)";
                }
            }

            return sql;
        }


    }
}
