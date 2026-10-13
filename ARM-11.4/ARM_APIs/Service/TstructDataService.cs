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
    public class TstructDataService : ITstructDataService
    {
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private Dictionary<string, object> sessionVars;
        private bool isTracing;
        public TstructDataService(IRedisHelper redis, IConfiguration config, Utils common, IARMLogTraceService traceService)
        {
            _redis = redis;
            _config = config;
            _common = common;
            _traceService = traceService;
        }


        public async Task<APIResult> AxDataPut(TstructData tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

            sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);
            tstructData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            tstructData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (string.IsNullOrEmpty(tstructData.AxOwner))
                tstructData.AxOwner = tstructData.UserName;

            var accessResult = new APIResult();

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                accessResult = await CheckCreateAccess(tstructData);
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                accessResult = await CheckEditAccess(tstructData);
            }
            else
            {
                accessResult.success = false;
                accessResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
                return accessResult;
            }

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");

                    accessResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }

                return accessResult;
            }
            else
            {
                var apiResult = new APIResult();

                if (tstructData.RapidSave == true)
                    apiResult = await ExecuteSubmitDataAPI(tstructData);
                else
                    apiResult = await ExecuteSaveDataAPI(tstructData);

                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                    apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }



                return apiResult;

            }

        }

        public async Task<APIResult> AxDataPutV2(TstructData tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

            sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);
            tstructData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            tstructData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (string.IsNullOrEmpty(tstructData.AxOwner))
                tstructData.AxOwner = tstructData.UserName;

            var accessResult = new APIResult();

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                accessResult = await CheckCreateAccess(tstructData);
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                accessResult = await CheckEditAccess(tstructData);
            }
            else
            {
                accessResult.success = false;
                accessResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
                return accessResult;
            }

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");

                    accessResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }

                return accessResult;
            }
            else
            {
                var apiResult = new APIResult();

                if (tstructData.RapidSave == true)
                    apiResult = await ExecuteSubmitDataAPI(tstructData);
                else
                    apiResult = await ExecuteSaveDataAPI(tstructData);

                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                    apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }



                return apiResult;

            }

        }

        private async Task<APIResult> CheckCreateAccess(TstructData tstructData)
        {
            /*
            CanCreate=false, ShouldExclude='' OnlyInclude=''
            For every row in User Access Data Result 
                If role=self and Axowner <> loginuser, skip to next row -- TODO
                else if constraint is not empty and any CS Value is not found in AxTransAttribute, skip to next row -- TODO
                if access=create set CanCreate to true 
                    if ShouldExclude is not none then ShouldExlcude = ShouldExclude + value in Exclude column 
                    else ShouldExclude = None
                    if element <> All then OnlyInclude=OnlyInclude + Element else OnlyInclude=''
            End For
            If CanCreate is false, raise exception ""access not given"" 
            If Request JSON contains any fields or DCs in ShouldExclude, raise exception.
            if OnlyInclude <> '' 
               if Request JSOn contains any fields or DCs other than that is in OnlyInclude, raise exception
            end if"
            */

            var result = new APIResult();

            bool canCreate = false;
            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            var userPermissions = await GetUserPermissionsForTstructs(tstructData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            foreach (DataRow dr in userPermissions.data.Rows)
            {
                var roleName = dr["axroles"].ToString();
                var constraint = dr["axuserconstraints"].ToString();
                var excludeElems = dr["structure_exclude_access"].ToString();
                var includeElems = dr["structure_elements"].ToString();

                if (string.IsNullOrEmpty(constraint))
                    constraint = "None";

                if (tstructData.Roles != null && tstructData.Roles.Count > 0 && !tstructData.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Skipped Roles - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                    continue;
                }

                if (roleName.ToUpper() == Constants.TSTRUCT_DATA_VARS.SELF.ToString() && tstructData.UserName != tstructData.AxOwner)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                    continue;
                }
                else
                {
                    var validConstraints = ValidateConstraints(tstructData?.AxTransAttribute, constraint);
                    if (!validConstraints.IsValid)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                        continue;
                    }
                    else
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Matching constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                        canCreate = true;
                        if (!string.IsNullOrEmpty(excludeElems))
                        {
                            if (!excludedElements.Contains(none, StringComparer.OrdinalIgnoreCase))
                            {
                                excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x)));
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
                                includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x)));
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
            }

            if (!canCreate)
            {
                result.success = false;
                result.error = "User does not have permissions to create this record.";
                if (_traceService.IsTracingEnabled())
                    result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                return result;
            }

            if (excludedElements.Count == 0) excludedElements.Add(none);
            if (includeElements.Count == 0) includeElements.Add(all);

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Excluded Elements - {string.Join(",", excludedElements)}");
                _traceService.LogMessage($"Included Elements - {string.Join(",", includeElements)}");
            }

            var tstructValidation = await ValidateElements(tstructData, excludedElements, includeElements);

            if (!tstructValidation.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructValidation.Errors);
            }


            return result;
        }

        private async Task<APIResult> CheckEditAccess(TstructData tstructData)
        {
            /*
            "CanEdit=false, ShouldExclude='' OnlyInclude=''
            For every row in User Access Data Result 
              If role=self and owner <> loginuser, skip to next row
              else if constraint is not empty and any CS Value is not found in TransAttribute, skip to next row
              if access=edit set CanEdit to true 
                if ShouldExclude is not none then ShouldExlcude = ShouldExclude + value in Exclude column 
                else ShouldExclude = None
                if element <> All then OnlyInclude=OnlyInclude + Element else OnlyInclude=''
            End For
            If CanEdit is false, raise exception ""access not given"" 
            If Request JSON contains any fields or DCs in ShouldExclude, raise exception.
            if OnlyInclude <> '' 
               if Request JSOn contains any fields or DCs other than that is in OnlyInclude, raise exception
            end if"
            */

            var result = new APIResult();

            bool canEdit = false;
            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            var userPermissions = await GetUserPermissionsForTstructs(tstructData);
            var userAttributes = await GetUserAttributes(tstructData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            if (userAttributes != null && userAttributes.data != null && userAttributes.data.Rows.Count > 0)
            {
                tstructData.RecordId = userAttributes.data.Rows[0]["recordid"].ToString();
                tstructData.AxOwner = userAttributes.data.Rows[0]["owner"].ToString();
                tstructData.AxTransAttribute = userAttributes.data.Rows[0]["transattribute"].ToString();
            }
            else
            {
                tstructData.RecordId = "0";
                tstructData.AxOwner = tstructData.UserName;
                tstructData.AxTransAttribute = "";
            }

            if (tstructData.RecordId == "0")
            {
                result.success = false;
                result.error = "Record does not exists for above keyfield & keyvalue.";
                return result;
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            foreach (DataRow dr in userPermissions.data.Rows)
            {
                var roleName = dr["axroles"].ToString();
                var constraint = dr["axuserconstraints"].ToString();
                var excludeElems = dr["structure_exclude_access"].ToString();
                var includeElems = dr["structure_elements"].ToString();

                if (string.IsNullOrEmpty(constraint))
                    constraint = "None";

                if (tstructData.Roles != null && tstructData.Roles.Count > 0 && !tstructData.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Skipped Roles - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                    continue;
                }

                if (roleName.ToUpper() == Constants.TSTRUCT_DATA_VARS.SELF.ToString() && tstructData.UserName != tstructData.AxOwner)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                    continue;
                }
                else
                {
                    var validConstraints = ValidateConstraints(tstructData?.AxTransAttribute, constraint);
                    if (!validConstraints.IsValid)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                        continue;
                    }
                    else
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Matching constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                        canEdit = true;
                        if (!string.IsNullOrEmpty(excludeElems))
                        {
                            if (!excludedElements.Contains(none, StringComparer.OrdinalIgnoreCase))
                            {
                                excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x)));
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
                                includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x)));
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
            }

            if (!canEdit)
            {
                result.success = false;
                result.error = "User does not have permissions to edit this record.";
                if (_traceService.IsTracingEnabled())
                    result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                return result;
            }

            if (excludedElements.Count == 0) excludedElements.Add(none);
            if (includeElements.Count == 0) includeElements.Add(all);

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Excluded Elements - {string.Join(",", excludedElements)}");
                _traceService.LogMessage($"Included Elements - {string.Join(",", includeElements)}");
            }

            var tstructValidation2 = await ValidateEditJson(tstructData);
            if (!tstructValidation2.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructValidation2.Errors);
            }

            var tstructValidation = await ValidateElements(tstructData, excludedElements, includeElements);

            if (!tstructValidation.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructValidation.Errors);
            }

            

            return result;
        }

        private async Task<APIResult> CheckViewAccess(TstructGetData getData, SQLResult userPermissions = null, SQLResult userAttributes = null)
        {
            /*
            ADS name, Transid, Param - Recordid or KeyFieldName and KeyFieldValue
            Get the records for this ADS from AxUserPermissions & AxPermissions table where Access=Edit/Delete/View depending on action for this user's roles. If no rows returned, raise exception "No access"
            Get the Owner & TransAttribute field from Transid_UserControl table for the given record.
            "CanView=false, ShouldExclude=''
            For every row in User Access Data Result 
              if role=self and owner <> loginuser, skip to next row 
              else If constraint is not empty and any CS Value is not found in TransAttribute, skip to next row
              if access=view set CanView to true 
                 if ShouldExclude is not none then ShouldExlcude = ShouldExclude + value in Exclude column 
                else ShouldExclude = None
            End For
            If CanView is false, raise exception ""access not given"""
            Fire SQL in ADS. Remove all columns that are in ShouldExclude and return result JSON

            */

            var result = new APIResult();

            bool canView = false;
            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> rules = new List<string>();
            if (userPermissions == null)
                userPermissions = await GetUserPermissionsForADS(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            if (getData.DoUserAttributesCheck == true)
            {
                TstructData tstructData = new TstructData();
                tstructData.Project = getData.Project;
                tstructData.KeyField = getData.KeyField;
                tstructData.KeyValue = getData.KeyValue;
                tstructData.TransId = getData.TransId;
                if (userAttributes == null)
                    userAttributes = await GetUserAttributes(tstructData);



                if (userAttributes != null && userAttributes.data != null && userAttributes.data.Rows.Count > 0)
                {
                    getData.RecordId = userAttributes.data.Rows[0]["recordid"].ToString();
                    getData.AxOwner = userAttributes.data.Rows[0]["owner"].ToString();
                    getData.AxTransAttribute = userAttributes.data.Rows[0]["transattribute"].ToString();
                }
                else
                {
                    getData.RecordId = "0";
                    getData.AxOwner = getData.UserName;
                    getData.AxTransAttribute = "";
                }

                if (getData.RecordId == "0")
                {
                    result.success = false;
                    result.error = "Record does not exists for above keyfield & keyvalue.";
                    return result;
                }
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            foreach (DataRow dr in userPermissions.data.Rows)
            {
                var roleName = dr["axroles"].ToString();
                var constraint = dr["axuserconstraints"].ToString();
                var excludeElems = dr["structure_exclude_access"].ToString();
                var includeElems = dr["structure_elements"].ToString();
                var axRules = dr["axrules"].ToString();

                if (string.IsNullOrEmpty(constraint))
                    constraint = "None";


                if (getData.Roles != null && getData.Roles.Count > 0 && !getData.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Skipped Roles - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                    continue;
                }

                if (roleName.ToUpper() == Constants.TSTRUCT_DATA_VARS.SELF.ToString() && getData.UserName != getData.AxOwner)
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                    continue;
                }
                else
                {
                    var validConstraints = ValidateConstraints(getData?.AxTransAttribute, constraint);
                    if (!validConstraints.IsValid)
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Invalid constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");
                        continue;
                    }
                    else
                    {
                        if (_traceService.IsTracingEnabled())
                            _traceService.LogMessage($"Matching constraints - {roleName}||{constraint}||{excludeElems}||{includeElems}");

                        canView = true;
                        if (!string.IsNullOrEmpty(excludeElems))
                        {
                            if (!excludedElements.Contains(none, StringComparer.OrdinalIgnoreCase))
                            {
                                excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x)));
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
                                includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x)));
                                includeElements = includeElements.Distinct().ToList();
                            }
                            else
                            {
                                includeElements.Clear();
                                includeElements.Add(all);
                            }
                        }

                        if (!string.IsNullOrEmpty(axRules))
                        {
                            rules.AddRange(axRules.Split("~"));
                        }
                    }
                }
            }

            if (!canView)
            {
                result.success = false;
                result.error = "User does not have permissions to view this record.";
                if (_traceService.IsTracingEnabled())
                    result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                return result;
            }

            if (excludedElements.Count == 0) excludedElements.Add(none);
            if (includeElements.Count == 0) includeElements.Add(all);

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"Excluded Elements - {string.Join(",", excludedElements)}");
                _traceService.LogMessage($"Included Elements - {string.Join(",", includeElements)}");
            }

            result.data["ExcludedElements"] = excludedElements.Distinct().ToList();
            result.data["IncludedElements"] = includeElements.Distinct().ToList();
            result.data["AxRules"] = rules.Distinct().ToList();

            return result;
        }

        private TstructDataValidationResult ValidateConstraints(List<string> transAttributeList, List<string> constraintList)
        {
            var result = new TstructDataValidationResult();
            if (transAttributeList == null || constraintList == null)
            {
                result.IsValid = false;
                return result;
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            if (constraintList.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                result.IsValid = true;
                return result;
            }

            // Find items in constraintList that are not in transAttributeList
            var missingAttributes = constraintList
                .Where(constraintItem => !transAttributeList.Contains(constraintItem, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (missingAttributes.Any())
            {
                result.IsValid = false;
            }

            return result;
        }

        private TstructDataValidationResult ValidateConstraints(string transAttributeStr, string constraintStr)
        {
            var result = new TstructDataValidationResult { IsValid = true };
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();

            if (!string.IsNullOrEmpty(constraintStr) && constraintStr.ToUpper() != none && string.IsNullOrEmpty(transAttributeStr))
            {
                result.IsValid = false;
                return result;
            }

            if ($",{constraintStr},".Contains($",{none},", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            List<string> transAttributeList = transAttributeStr?.Split(',')
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList() ?? new List<string>();


            // Split by ',' to get AND conditions
            var constraintGroups = constraintStr.Split(',')
                .Select(group => group.Split('|') // Split by '|' to get OR conditions
                                      .Select(s => s.Trim())
                                      .ToList())
                .ToList();

            // Validate constraints
            foreach (var group in constraintGroups)
            {
                // Check if at least one value from the OR group exists in transAttributeList
                bool groupSatisfied = group.Any(option => transAttributeList.Contains(option, StringComparer.OrdinalIgnoreCase));

                if (!groupSatisfied)
                {
                    result.IsValid = false;
                    return result;
                }
            }

            return result;
        }

        private async Task<TstructDataValidationResult> ValidateElements(TstructData tstructData, List<string> excludeElems, List<string> includeElems)
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

                // Special case handling for "All" and "None"
                var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
                var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
                bool includeAll = includeElems.Contains(all, StringComparer.OrdinalIgnoreCase);
                bool excludeNone = excludeElems.Contains(none, StringComparer.OrdinalIgnoreCase);

                // Process containers (dc1, dc2, etc.)
                var containers = dataObject.Properties()
                    .Where(p => p.Name.StartsWith("dc", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Validate exclude rules
                if (!excludeNone)
                {
                    foreach (var excludeElem in excludeElems)
                    {
                        // Check full container exclusion
                        if (!excludeElem.Contains('.'))
                        {
                            if (dataObject.Property(excludeElem) != null)
                            {
                                result.IsValid = false;
                                result.Errors.Add($"DC '{excludeElem}' is in the exclude list .");
                            }
                        }
                        // Check field exclusion with path notation (dc2.attrib6)
                        else
                        {
                            string[] parts = excludeElem.Split('.');
                            if (parts.Length == 2)
                            {
                                string containerName = parts[0];
                                string fieldName = parts[1];

                                var container = dataObject[containerName] as JObject;
                                if (container != null)
                                {
                                    foreach (var row in container.Properties())
                                    {
                                        var rowObj = row.Value as JObject;
                                        if (rowObj != null && rowObj.Property(fieldName) != null)
                                        {
                                            result.IsValid = false;
                                            result.Errors.Add($"Field '{containerName}.{fieldName}' in {row.Name} is in the exclude list.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Validate include rules
                if (!includeAll)
                {
                    // Process include elements that specify full containers (dc2)
                    var includedContainers = includeElems
                        .Where(e => !e.Contains('.'))
                        .ToList();

                    // Process include elements that specify fields with paths (dc2.attrib6)
                    var includedFields = includeElems
                        .Where(e => e.Contains('.'))
                        .Select(e =>
                        {
                            var parts = e.Split('.');
                            return new { Container = parts[0], Field = parts[1] };
                        })
                        .ToList();

                    // Get unique container names from field paths
                    var includedFieldContainers = includedFields
                        .Select(f => f.Container)
                        .Distinct()
                        .ToList();

                    // Combine all allowed container names
                    var allowedContainers = includedContainers
                        .Union(includedFieldContainers)
                        .ToList();

                    // Check that containers in the data are in the allowed list
                    foreach (var container in containers)
                    {
                        string containerName = container.Name;

                        if (!allowedContainers.Contains(containerName, StringComparer.OrdinalIgnoreCase))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"DC '{containerName}' is not in the include list.");
                        }
                        else if (includedFields.Any(f => f.Container.Equals(containerName, StringComparison.OrdinalIgnoreCase)))
                        {
                            // If this container has specific fields to include, validate each field
                            var allowedFields = includedFields
                                .Where(f => f.Container.Equals(containerName, StringComparison.OrdinalIgnoreCase))
                                .Select(f => f.Field)
                                .ToList();

                            var containerObj = container.Value as JObject;
                            if (containerObj != null)
                            {
                                foreach (var row in containerObj.Properties())
                                {
                                    var rowObj = row.Value as JObject;
                                    if (rowObj != null)
                                    {
                                        foreach (var field in rowObj.Properties())
                                        {
                                            if (!allowedFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                                            {
                                                result.IsValid = false;
                                                result.Errors.Add($"Field '{field.Name}' in {containerName}.{row.Name} is not in include list.");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
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

        private async Task<SQLResult> GetUserPermissionsForTstructs(TstructData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_AXUSERPERMISSIONS_TSTRUCTS.ToString();
            string[] paramName = { "@username", "@transid", "@action" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { tstructData.UserName.ToString().ToLower(), tstructData.TransId.ToLower(), $"%{tstructData.Action.ToLower()}%" };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<SQLResult> GetUserPermissionsForADS(TstructGetData getData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_AXUSERPERMISSIONS_ADS.ToString();
            string[] paramName = { "@username", "@structid", "@action" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { getData.UserName.ToString().ToLower(), getData.ADSName.ToLower(), $"%{getData.Action.ToLower()}%" };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<SQLResult> GetUserPermissionsForADSGroup(TstructGetData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_AXUSERPERMISSIONS_ADSGROUP.ToString();
            string[] paramName = { "@username", "@structid", "@action" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { getData.UserName.ToString().ToLower(), getData.ADSGroupName.ToLower(), $"%{getData.Action.ToLower()}%" };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<SQLResult> GetUserAttributes(TstructData tstructData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_AXUSERATTRIBUTES.ToString();
            string[] paramName = { "@transid", "@keyfield", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { tstructData.TransId, tstructData.KeyField, tstructData.KeyValue };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<APIResult> ExecuteSubmitDataAPI(TstructData tstructData)
        {
            APIResult result = new APIResult();
            if (tstructData.SubmitData is null)

            {
                result.error = "Input Json is not having 'submitdata' node.";
                return result;
            }

            if (!tstructData.SubmitData.ContainsKey("dataarray"))
            {
                var tempSubmitData = tstructData.SubmitData;
                tempSubmitData = AddRowNodeIfMissing(tempSubmitData);
                tstructData.SubmitData = new Dictionary<string, object>();
                tstructData.SubmitData.Add("dataarray", new Dictionary<string, object> { { "data", tempSubmitData } });
            }

            string submitDataJson = System.Text.Json.JsonSerializer.Serialize(tstructData.SubmitData);
            JObject submitDataObj = JObject.Parse(submitDataJson);
            AddAxpertNodesToJson(ref submitDataObj, tstructData);

            string apiInput = JsonConvert.SerializeObject(new { submitdata = submitDataObj });

            string armWebScriptsURL = await _common.GetARMWebScriptsURL(tstructData.Project);

            string apiUrl = armWebScriptsURL + "/ASBRapidSaveRest.dll/datasnap/rest/TASBRapidSaveRest/submitdata";
            var api = new API();
            DateTime startTime = DateTime.Now;
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");
            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage($"API URL - {apiUrl}");
                _traceService.LogMessage($"API JSON - {apiInput}");
                _traceService.LogMessage($"API Result - {JsonConvert.SerializeObject(apiResult)}");
            }
            DateTime endTime = DateTime.Now;
            if (_traceService.IsTracingEnabled())
            {
                TimeSpan duration = endTime - startTime;
                _traceService.LogMessage($"API Call - Started: {startTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {endTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
            }

            return ParseAxpertRestAPIResult(apiResult);

            //if (apiResult2.success || apiResult2.data?["success"]?.ToString().ToLower() == "true")
            //{
            //    var sqls = GenerateTrackChangesInsertQueries(tstructData.TransId, apiInput);

            //    await InsertTrackChangesSQLs(tstructData, sqls);
            //}

            //return apiResult2;

        }

        private Dictionary<string, object> AddRowNodeIfMissing(Dictionary<string, object> jsonObj)
        {
            var updatedJson = new Dictionary<string, object>();

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
                    updatedJson[kvp.Key] = tempObj; // Keep as is if "row1" exists
                }

            }

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
            

        //    return updatedJson;
        //}

        private Dictionary<string, object> AddRowNodesAndRecordIdsWithDataTable(Dictionary<string, object> submitData, DataTable recordIdTable)
        {
            var updatedJson = new Dictionary<string, object>();

            // Create a lookup dictionary from DataTable
            var recordIdMap = new Dictionary<string, string>();
            foreach (DataRow row in recordIdTable.Rows)
            {
                var dcname = row["dcname"].ToString().ToLower();
                var rowno = Convert.ToInt32(row["rowno"]);
                var recordid = row["recordid"].ToString();

                var key = $"{dcname}_row{rowno}";
                recordIdMap[key] = recordid;
            }

            foreach (var dc in submitData)
            {
                var tempNodeStr = System.Text.Json.JsonSerializer.Serialize(dc.Value);
                var dcContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempNodeStr);

                bool isGrid = dcContent.Keys.Count > 0 && dcContent.Keys.First().StartsWith("row");

                if (!isGrid)
                {
                    var rowKey = "row0";
                    var dcNameRowKey = $"{dc.Key.ToLower()}_row0";
                    var recid = recordIdMap.ContainsKey(dcNameRowKey) ? recordIdMap[dcNameRowKey] : "0";

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
                        var rowIndex = int.Parse(rowKey.Replace("row", ""));
                        var dcNameRowKey = $"{dc.Key.ToLower()}_row{rowIndex}";
                        var recid = recordIdMap.ContainsKey(dcNameRowKey) ? recordIdMap[dcNameRowKey] : "0";

                        if (!rowData.ContainsKey("axp_recid"))
                        {
                            rowData["axp_recid"] = recid;
                        }

                        updatedRows[rowKey] = rowData;
                    }

                    updatedJson[dc.Key] = updatedRows;
                }
            }

            // Apply deletion/edit logic on updatedJson
            return ProcessFinalData(updatedJson);
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

                    if (rowData.TryGetValue("axrow_action", out var action) && action.ToString() == "delete")
                    {
                        string recid = rowData.ContainsKey("axp_recid") ? rowData["axp_recid"].ToString() : $"row{rowIndex}id";
                        allDeletedEntries.Add((rowIndex, dcNumber, dcName, recid));

                        if (!deletedIndicesByDc.ContainsKey(dcName))
                            deletedIndicesByDc[dcName] = new List<int>();

                        deletedIndicesByDc[dcName].Add(rowIndex);
                        continue;
                    }

                    bool isEdited = false;
                    if (rowData.TryGetValue("axrow_action", out var editAction) && editAction.ToString() == "edit")
                    {
                        rowData.Remove("axrow_action");
                        isEdited = true;
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



        private Dictionary<string, object> ProcessFinalDataOld(Dictionary<string, object> submitData)
        {
            var result = new Dictionary<string, object>();
            var delRows = new Dictionary<string, object>();
            var changedRows = new Dictionary<string, string>();

            var finalSubmitData = new Dictionary<string, object>();
            var allDeletedEntries = new List<(int rowIndex, int dcNumber, string dcName, string recid)>();

            foreach (var dcPair in submitData)
            {
                string dcName = dcPair.Key;
                int dcNumber = int.Parse(dcName.Replace("dc", ""));
                //var dcContent = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(dcPair.Value));
                var tempNodeStr = System.Text.Json.JsonSerializer.Serialize(dcPair.Value);
                var dcContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempNodeStr);


                var keptRows = new List<Dictionary<string, object>>();

                foreach (var row in dcContent)
                {
                    var rowKey = row.Key;
                    //var rowData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(row.Value));
                    var rowNodeStr = System.Text.Json.JsonSerializer.Serialize(row.Value);
                    var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(rowNodeStr);

                    int rowIndex = int.Parse(rowKey.Replace("row", ""));

                    if (rowData.TryGetValue("axrow_action", out var action) && action.ToString() == "delete")
                    {
                        string recid = rowData.ContainsKey("axp_recid") ? rowData["axp_recid"].ToString() : "0";
                        allDeletedEntries.Add((rowIndex, dcNumber, dcName, recid));
                        continue;
                    }

                    if (rowData.TryGetValue("axrow_action", out var editAction) && editAction.ToString() == "edit")
                    {
                        rowData.Remove("axrow_action");
                        changedRows[dcName] = "1";
                    }

                    keptRows.Add(rowData);
                }

                if (keptRows.Any())
                {
                    var newDcObj = new Dictionary<string, object>();
                    for (int i = 0; i < keptRows.Count; i++)
                    {
                        newDcObj[$"row{i}"] = keptRows[i];
                    }
                    finalSubmitData[dcName] = newDcObj;
                }
            }

            // Sort the deleted entries by rowIndex descending per requirement
            var sortedDeleted = allDeletedEntries.OrderByDescending(x => x.rowIndex).ToList();

            delRows["deletedrno"] = string.Join(",", sortedDeleted.Select(x => x.rowIndex));
            delRows["deleteddcno"] = string.Join(",", sortedDeleted.Select(x => x.dcNumber));

            // Group by dcName and fill recid arrays dynamically
            var dcGroups = sortedDeleted.GroupBy(x => x.dcName);
            foreach (var group in dcGroups)
            {
                string recidKey = $"axp_recid{group.Key.Replace("dc", "")}";
                delRows[group.Key] = new Dictionary<string, object>
                {
                    [recidKey] = group.Select(x => x.recid).ToList()
                };
            }

            result["SubmitData"] = finalSubmitData;
            if (delRows.Count > 2) result["delrows"] = delRows;
            if (changedRows.Count > 0) result["changedrows"] = changedRows;

            return result;
        }

        private async Task<APIResult> ExecuteSaveDataAPI(TstructData tstructData)
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
                    ["cachedsave"] = "true",
                    ["axpapp"] = submitDataJson["Project"].ToString(),
                    ["transid"] = submitDataJson["TransId"].ToString(),
                    ["s"] = submitDataJson["ARMSessionId"].ToString(),
                    ["username"] = submitDataJson["UserName"].ToString(),
                    ["password"] = sessionVars[SESSION_DATA.PASSWORD.ToString()].ToString(),
                    ["changedrows"] = (submitDataJson["SubmitData"]["dataarray"]["data"]?["changedrows"] != null ? (JObject)submitDataJson["SubmitData"]["dataarray"]["data"]?["changedrows"] : new JObject()),
                    ["delrows"] = (submitDataJson["SubmitData"]["dataarray"]["data"]?["delrows"] != null ? (JObject)submitDataJson["SubmitData"]["dataarray"]["data"]?["delrows"] : new JObject()),
                    ["trace"] = submitDataJson["Trace"].ToString() ?? "",
                    ["recordid"] = submitDataJson["RecordId"].ToString() ?? "0",
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



        private async Task<SQLResult> GetData(string dbType, string sql, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        private void AddAxpertNodesToJson(ref JObject jsonObject, TstructData tstructData)
        {
            var tempSubmitData = jsonObject;

            _common.AddJsonKey(ref jsonObject, "username", tstructData.UserName);
            _common.UpdateOrAddJsonKey(ref jsonObject, "project", tstructData.Project);
            _common.UpdateOrAddJsonKey(ref jsonObject, "name", tstructData.TransId);
            _common.UpdateOrAddJsonKey(ref jsonObject, "trace", tstructData.Trace.ToString().ToLower());

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "keyfield", "");
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "keyvalue", "");
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "recordid", "0");
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "mode", "new");
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "keyfield", "");
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "keyvalue", "");
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "recordid", tstructData.RecordId);
                _common.UpdateOrAddJsonKeyToDataNode(ref jsonObject, "mode", "edit");
            }

            AxpertRestAPIToken axpertToken = new AxpertRestAPIToken(tstructData.UserName);
            _common.RemoveJsonKey(ref jsonObject, "username");
            _common.UpdateOrAddJsonKey(ref jsonObject, "token", axpertToken.token);
            _common.UpdateOrAddJsonKey(ref jsonObject, "seed", axpertToken.seed);
            _common.UpdateOrAddJsonKey(ref jsonObject, "userauthkey", axpertToken.userAuthKey);
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



        public async Task<APIResult> AxDataGet(TstructGetData getData)
        {
            isTracing = getData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getData)}");

            sessionVars = await _common.GetSessionData(getData.ARMSessionId);
            getData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            getData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (string.IsNullOrEmpty(getData.AxOwner))
                getData.AxOwner = getData.UserName;

            var apiResult = new APIResult();
            if (getData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                if (!string.IsNullOrEmpty(getData.ADSGroupName))
                    apiResult = await GetADSGroupData(getData);
                else if (!string.IsNullOrEmpty(getData.ADSName))
                    apiResult = await GetADSData(getData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {getData.Action.ToUpper()}.";

            }

            return apiResult;

        }

        private async Task<APIResult> GetADSData(TstructGetData getData, SQLResult userPermissions = null, SQLResult userAttributes = null, string adsName = "")
        {
            if (!string.IsNullOrEmpty(adsName))
                getData.ADSName = adsName;
            else
                adsName = getData.ADSName;

            var accessResult = await CheckViewAccess(getData, userPermissions, userAttributes);

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                if (isTracing && string.IsNullOrEmpty(getData.ADSGroupName))
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");

                    accessResult.data["trace"] = await _traceService.GetTraceFile("AxDataGet-" + adsName);
                }
                accessResult.data["adsname"] = adsName;
                return accessResult;
            }
            else
            {
                var apiResult = new APIResult();
                var adsResult = await GetADSDataFromDB(getData);
                var excludedCols = (List<string>)accessResult.data["ExcludedElements"];
                var includeCols = (List<string>)accessResult.data["IncludedElements"];
                var rules = (List<string>)accessResult.data["AxRules"];
                if (string.IsNullOrEmpty(adsResult.error) && adsResult.DataSet != null && adsResult.DataSet.Tables.Count > 0)
                {
                    var dt = adsResult.DataSet.Tables[0];
                    var newDt = await HandleIncludeExcludeCols(dt, excludedCols, includeCols, rules);

                    apiResult.success = true;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["data"] = newDt;
                }
                else
                {
                    apiResult.success = false;
                    apiResult.data["adsname"] = adsName;
                    apiResult.data["error"] = adsResult.error;
                }


                if (isTracing && string.IsNullOrEmpty(getData.ADSGroupName))
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(adsResult)}");
                    apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataGet-" + adsName);
                }

                return apiResult;

            }
        }


        private async Task<APIResult> GetADSGroupData(TstructGetData getData)
        {

            var result = new APIResult();

            bool canView = false;
            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> rules = new List<string>();
            var userPermissions = await GetUserPermissionsForADSGroup(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            TstructData tstructData = new TstructData();
            tstructData.Project = getData.Project;
            tstructData.KeyField = getData.KeyField;
            tstructData.KeyValue = getData.KeyValue;
            tstructData.TransId = getData.TransId;
            var userAttributes = await GetUserAttributes(tstructData);



            List<string> adsNames = userPermissions.data.AsEnumerable().Select(row => row.Field<string>("structure_id")).Distinct().ToList();

            List<Task<APIResult>> adsData = new List<Task<APIResult>>();

            foreach (var adsName in adsNames)
            {
                getData.ADSName = adsName;
                var tempUserPermissions = new SQLResult();
                tempUserPermissions.data = userPermissions.data.AsEnumerable().Where(row => row.Field<string>("structure_id") == adsName).CopyToDataTable();
                adsData.Add(GetADSData(getData, tempUserPermissions, userAttributes, adsName));
            }

            var adsDataResults = await Task.WhenAll(adsData);

            var apiResult = new APIResult();
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

            apiResult.data["adsgroupname"] = getData.ADSGroupName;
            apiResult.data["data"] = resultList;

            if (isTracing && !string.IsNullOrEmpty(getData.ADSGroupName))
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(adsDataResults)}");
                apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataGet-" + getData.ADSGroupName);
            }

            return apiResult;

        }


        private async Task<SQLDataSetResult> GetADSDataFromDB(TstructGetData getData)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GETDATASOURCESSQL.ToString();
            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { getData.ADSName?.ToLower() };

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

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, null, getData.GlobalParams);
            return dsResult;
        }

        private async Task<SQLDataSetResult> GetDataSet(string dbType, string sql, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLsFromSQLAsync(sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
        }


        private async Task<DataTable> HandleIncludeExcludeCols(DataTable dt, List<string> excludeCols, List<string> includeCols, List<string> rules)
        {
            DataTable newTable = new DataTable();
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            // ✅ If includeCols = ["all"], include all columns
            if (includeCols.Contains(all, StringComparer.OrdinalIgnoreCase))
            {
                includeCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            }

            // ✅ If excludeCols = ["none"], no columns are excluded
            if (excludeCols.Contains(none, StringComparer.OrdinalIgnoreCase))
            {
                excludeCols.Clear();
            }

            // ✅ Filter final list of columns to include
            var finalColumns = includeCols
                .Where(col => !excludeCols.Contains(col, StringComparer.OrdinalIgnoreCase) && dt.Columns.Contains(col))
                .ToList();

            // ✅ Add final columns to new table
            foreach (string col in finalColumns)
            {
                newTable.Columns.Add(col);
            }

            // ✅ Parse masking rules
            var maskRules = rules
                .Where(r => r.StartsWith("Mask|"))
                .Select(r => r.Split('|'))
                .Where(r => r.Length == 3)
                .ToDictionary(r => r[1], r => r[2]);

            // ✅ Copy filtered data to new table
            foreach (DataRow row in dt.Rows)
            {
                DataRow newRow = newTable.NewRow();

                foreach (string col in finalColumns)
                {
                    if (maskRules.ContainsKey(col) && row[col] != DBNull.Value)
                    {
                        newRow[col] = ApplyMask(row[col].ToString(), maskRules[col]);
                    }
                    else
                    {
                        newRow[col] = row[col];
                    }
                }

                newTable.Rows.Add(newRow);
            }

            return newTable;
        }

        private string ApplyMask(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return input;

            input = input.Trim();

            int length = input.Length;
            string[] parts = pattern.Split('*');

            if (pattern.StartsWith("*"))
            {
                // ✅ Mask all except last N characters
                int visibleLength = parts[1].Length;
                return new string('*', length - visibleLength) + input.Substring(length - visibleLength);
            }
            else if (pattern.EndsWith("*"))
            {
                // ✅ Mask all except first N characters
                int visibleLength = parts[0].Length;
                return input.Substring(0, visibleLength) + new string('*', length - visibleLength);
            }
            else if (pattern.Contains("*"))
            {
                int prefixLength = parts[0].Length;
                int suffixLength = parts[1].Length;
                return input.Substring(0, prefixLength) +
                       new string('*', length - prefixLength - suffixLength) +
                       input.Substring(length - suffixLength);
            }

            return input; // No masking applied if pattern doesn't match
        }


        private List<string> GenerateTrackChangesInsertQueries(string transId, string inputJson)
        {
            List<string> sqlQueries = new List<string>();
            JObject json = JObject.Parse(inputJson);

            // Get root properties
            string recordId = json["submitdata"]["dataarray"]["recordid"]?.ToString() ?? "0";
            string username = json["submitdata"]["name"]?.ToString() ?? "unknown";
            string modifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Current Timestamp

            JObject data = (JObject)json["submitdata"]["dataarray"]["data"];

            int frameNo = 1; // Start frame number

            foreach (var dc in data.Properties())
            {
                if (dc.Name.StartsWith("dc")) // Check for dynamic DC keys
                {
                    JObject dcData = (JObject)dc.Value;

                    int rowNo = 1;
                    foreach (var row in dcData.Properties())
                    {
                        JObject rowData = (JObject)row.Value;

                        foreach (var field in rowData.Properties())
                        {
                            string fieldName = field.Name;
                            string newValue = field.Value.ToString().Replace("'", "''"); // Escape single quotes

                            if (!string.IsNullOrEmpty(newValue)) // Only insert if value is present
                            {
                                string sql = $@"INSERT INTO {transId}history 
(recordid, modifieddate, username, fieldname, newvalue, rowno, frameno) 
VALUES 
({recordId}, '{modifiedDate}', '{username}', '{fieldName}', '{newValue}', {rowNo}, {frameNo});";

                                sqlQueries.Add(sql);
                            }
                        }
                        rowNo++;
                    }
                    frameNo++;
                }
            }

            return sqlQueries;
        }

        private async Task InsertTrackChangesSQLs(TstructData tstructData, List<string> sqls)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            foreach (var sql in sqls)
            {
                await dbHelper.ExecuteNonQueryAsync(sql, connectionString);
            }
        }

        public async Task<APIResult> AxADSDataGet(ADSGetData adsData)
        {
            isTracing = adsData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(adsData)}");

            sessionVars = await _common.GetSessionData(adsData.ARMSessionId);
            adsData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            adsData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            if (string.IsNullOrEmpty(adsData.AxOwner))
                adsData.AxOwner = adsData.UserName;

            var apiResult = new APIResult();
            if (adsData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap<ADSGetData, TstructGetData>());
                var mapper = config.CreateMapper();
                TstructGetData tstructData = mapper.Map<TstructGetData>(adsData);

                if (!string.IsNullOrEmpty(adsData.ADSGroupName))
                    apiResult = await GetADSGroupData(tstructData);
                else if (!string.IsNullOrEmpty(adsData.ADSName))
                    apiResult = await GetADSData(tstructData);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {adsData.Action.ToUpper()}.";

            }

            return apiResult;

        }

        public async Task<APIResult> AxGet(TstructDataGet getData)
        {
            isTracing = getData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getData)}");

            sessionVars = await _common.GetSessionData(getData.ARMSessionId);
            getData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            getData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            var config = new MapperConfiguration(cfg => cfg.CreateMap<TstructDataGet, TstructGetData>());
            var mapper = config.CreateMapper();
            TstructGetData getDataTemp = mapper.Map<TstructGetData>(getData);

            var apiResult = new APIResult();
            if (getData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
            {
                apiResult = await GetTstructLoadData(getDataTemp);
            }
            else
            {
                apiResult.success = false;
                apiResult.data["error"] = $"Invalid action - {getData.Action.ToUpper()}.";

            }

            return apiResult;

        }

        private async Task<APIResult> GetTstructLoadData(TstructGetData getData)
        {

            var accessResult = await CheckTstructViewAccess(getData);

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");
                    accessResult.data["trace"] = await _traceService.GetTraceFile("AxDataGet-" + getData.TransId);
                }
                accessResult.data["transid"] = getData.TransId;
                return accessResult;
            }
            else
            {
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


                if (isTracing && string.IsNullOrEmpty(getData.ADSGroupName))
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(loadDataResult)}");
                    apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataGet-" + getData.TransId);
                }

                return apiResult;

            }
        }

        private async Task<APIResult> CheckTstructViewAccess(TstructGetData getData)
        {
            /*
            ADS name, Transid, Param - Recordid or KeyFieldName and KeyFieldValue
            Get the records for this ADS from AxUserPermissions & AxPermissions table where Access=Edit/Delete/View depending on action for this user's roles. If no rows returned, raise exception "No access"
            Get the Owner & TransAttribute field from Transid_UserControl table for the given record.
            "CanView=false, ShouldExclude=''
            For every row in User Access Data Result 
              if role=self and owner <> loginuser, skip to next row 
              else If constraint is not empty and any CS Value is not found in TransAttribute, skip to next row
              if access=view set CanView to true 
                 if ShouldExclude is not none then ShouldExlcude = ShouldExclude + value in Exclude column 
                else ShouldExclude = None
            End For
            If CanView is false, raise exception ""access not given"""
            Fire SQL in ADS. Remove all columns that are in ShouldExclude and return result JSON

            */

            var result = new APIResult();

            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();

            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();
            List<string> maskedFields = new List<string>();
            var userPermissions = await GetUserPermissionsForTstruct(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            var recordid = "";
            foreach (DataRow dr in userPermissions.data.Rows)
            {
                var roleName = "";
                var axRules = "";
                var constraint = "";

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
                        excludedDCElements.AddRange(excludeDCs.Split(',').Where(x => !excludedDCElements.Contains(x)));
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
                        includeDCElements.AddRange(includeDCs.Split(',').Where(x => !includeDCElements.Contains(x)));
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
                        excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x)));
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
                        includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x)));
                        includeElements = includeElements.Distinct().ToList();
                    }
                    else
                    {
                        includeElements.Clear();
                        includeElements.Add(all);
                    }
                }
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

            result.data["ExcludedDCs"] = excludedDCElements.Distinct().ToList();
            result.data["IncludedDCs"] = includeDCElements.Distinct().ToList();
            result.data["ExcludedFields"] = excludedElements.Distinct().ToList();
            result.data["IncludedFields"] = includeElements.Distinct().ToList();
            result.data["MaskedFields"] = maskedFields.Distinct().ToList();
            result.data["RecordId"] = recordid;

            return result;
        }

        private async Task<SQLResult> GetUserPermissionsForTstruct(TstructGetData getData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(getData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_TSTRUCTPERMISSIONS.ToString();
            string[] paramName = { "@transid", "@keyfield", "@keyvalue", "@username", "@roles" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { getData.TransId, getData.KeyField, $"'{getData.KeyValue}'", getData.UserName, string.Join(",", getData.Roles) };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<APIResult> ExecuteLoadDataAPI(TstructGetData getData, APIResult accessResult)
        {
            APIResult result = new APIResult();
            var sessionVars = await _common.GetSessionData(getData.ARMSessionId);

            string armWebScriptsURL = sessionVars[SESSION_DATA.SCRIPTSURL.ToString()].ToString();

            string apiUrl = armWebScriptsURL + "/ASBTStructRest.dll/datasnap/rest/TASBTStruct/LoadData";
            var api = new API();
            DateTime startTime = DateTime.Now;

            JObject dataJson = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(getData));

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
                ["axprops"] = JObject.Parse(sessionVars[SESSION_DATA.AXPROPS.ToString()].ToString()),
                ["globalvars"] = JObject.Parse(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString()),
                ["uservars"] = JObject.Parse(sessionVars[SESSION_DATA.USERVARS.ToString()].ToString()),
                [dataJson["Project"].ToString()] = JObject.Parse(sessionVars[SESSION_DATA.AXAPPS.ToString()].ToString()),
            };

            var apiInput = JsonConvert.SerializeObject(loadDataJson);

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

            var resultNode = ParseAxpertLoadDataRestAPIResult(armResult, accessResult);
            //var rss = ParseLoadDataJson(resultNode.data["result"].ToString());
            return resultNode;
        }

        private Dictionary<string, Dictionary<string, Dictionary<string, object>>> ParseLoadDataJson(JObject jObject, APIResult accessResult)
        {
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
            //var jObject = JObject.Parse(jsonString);
            var items = jObject["result"].Where(item => item["t"].ToString() != "dv").ToList();

            string currentDc = null;
            int currentRow = 0;
            var rowData = new Dictionary<string, object>();

            var excludedDCs = (List<string>)accessResult.data["ExcludedDCs"];
            var includeDCs = (List<string>)accessResult.data["IncludedDCs"];
            var excludedFields = (List<string>)accessResult.data["ExcludedFields"];
            var includeFields = (List<string>)accessResult.data["IncludedFields"];
            var maskedFields = (List<string>)accessResult.data["MaskedFields"];
            foreach (var item in items)
            {
                var type = item["t"].ToString();
                var name = item["n"]?.ToString();

                if (type == "dc")
                {
                    // Save previous DC data if exists
                    if (currentDc != null)
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

                        // Merge rowData with existing data
                        foreach (var kvp in rowData)
                        {
                            result[currentDc][rowKey][kvp.Key] = kvp.Value;
                        }
                    }

                    // Start new DC
                    currentDc = name;
                    currentRow = 1;
                    rowData = new Dictionary<string, object>();
                }
                else
                {
                    var row = item["r"]?.ToString();
                    if (row != null && int.TryParse(row, out int rowNum))
                    {
                        if (rowNum == 0) rowNum = 1;

                        if (rowNum != currentRow)
                        {
                            // Save current row data
                            if (currentDc != null)
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

                                // Merge rowData with existing data
                                foreach (var kvp in rowData)
                                {
                                    result[currentDc][rowKey][kvp.Key] = kvp.Value;
                                }
                            }

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
                            rowData[name] = value;
                        }
                    }
                }
            }

            // Save the last DC data
            if (currentDc != null)
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

                // Merge rowData with existing data
                foreach (var kvp in rowData)
                {
                    result[currentDc][rowKey][kvp.Key] = kvp.Value;
                }
            }

            return result;
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

            var excludedDCs = (List<string>)accessResult.data["ExcludedDCs"];
            var includeDCs = (List<string>)accessResult.data["IncludedDCs"];
            var excludedFields = (List<string>)accessResult.data["ExcludedFields"];
            var includeFields = (List<string>)accessResult.data["IncludedFields"];
            var maskedFields = (List<string>)accessResult.data["MaskedFields"];

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
                                rowData[name] = ApplyFieldMask(value, maskRules[name]);
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

        private string ApplyFieldMask(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return input;

            input = input.Trim();
            if (input.Length == 0)
                return input;

            // Case 1: Simple digit pattern (e.g., "999")
            if (pattern.All(c => c == '9'))
            {
                int showLength = Math.Min(pattern.Length, input.Length);
                return input.Substring(0, showLength) +
                       new string('.', Math.Max(0, input.Length - showLength));
            }

            // Case 2: Pattern with range separator (.. or ** or any repeated non-digit)
            var separator = GetRepeatedNonDigitSeparator(pattern);
            if (separator != null)
            {
                string[] parts = pattern.Split(new[] { separator }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    int prefixLength = parts[0].Count(c => c == '9');
                    int suffixLength = parts[1].Count(c => c == '9');

                    if (prefixLength + suffixLength > input.Length)
                        return input; // Pattern longer than input

                    char maskChar = separator[0]; // Use the first char of separator as mask character

                    return input.Substring(0, prefixLength) +
                           new string(maskChar, input.Length - prefixLength - suffixLength) +
                           input.Substring(input.Length - suffixLength);
                }
            }

            // Case 3: Prefix pattern (e.g., "99***")
            var prefixMatch = System.Text.RegularExpressions.Regex.Match(pattern, @"^(9+)([^9]+)$");
            if (prefixMatch.Success)
            {
                int showLength = prefixMatch.Groups[1].Length;
                char maskChar = prefixMatch.Groups[2].Value[0];
                return input.Substring(0, Math.Min(showLength, input.Length)) +
                       new string(maskChar, Math.Max(0, input.Length - showLength));
            }

            // Case 4: Suffix pattern (e.g., "***99")
            var suffixMatch = System.Text.RegularExpressions.Regex.Match(pattern, @"^([^9]+)(9+)$");
            if (suffixMatch.Success)
            {
                int showLength = suffixMatch.Groups[2].Length;
                char maskChar = suffixMatch.Groups[1].Value[0];
                return new string(maskChar, Math.Max(0, input.Length - showLength)) +
                       input.Substring(Math.Max(0, input.Length - showLength));
            }

            // Case 5: Mixed pattern with custom mask characters (e.g., "99XX99")
            if (pattern.Any(c => c != '9'))
            {
                StringBuilder result = new StringBuilder();
                int inputPos = 0;

                foreach (char c in pattern)
                {
                    if (inputPos >= input.Length)
                        break;

                    if (c == '9')
                    {
                        result.Append(input[inputPos++]);
                    }
                    else
                    {
                        result.Append(c);
                        inputPos++;
                    }
                }

                // If we have remaining characters, mask them with the last mask character found
                if (inputPos < input.Length && pattern.Any(p => p != '9'))
                {
                    char lastMaskChar = pattern.Last(p => p != '9');
                    result.Append(new string(lastMaskChar, input.Length - inputPos));
                }

                return result.ToString();
            }

            // Default: return original input if no pattern matched
            return input;
        }

        private string GetRepeatedNonDigitSeparator(string pattern)
        {
            for (int i = 0; i < pattern.Length - 1; i++)
            {
                if (!char.IsDigit(pattern[i]) && pattern[i] == pattern[i + 1])
                {
                    int start = i;
                    while (i < pattern.Length && pattern[i] == pattern[start])
                        i++;
                    return pattern.Substring(start, i - start);
                }
            }
            return null;
        }



        public async Task<APIResult> AxPut(TstructData tstructData)
        {
            isTracing = tstructData.Trace == true;
            _traceService.EnableTracing(isTracing);

            if (isTracing)
                _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

            sessionVars = await _common.GetSessionData(tstructData.ARMSessionId);
            tstructData.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            tstructData.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

            var accessResult = new APIResult();

            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
            {
                accessResult = await CheckTstructCreateAccess(tstructData);
            }
            else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
            {
                accessResult = await CheckTstructEditAccess(tstructData);
            }
            else
            {
                accessResult.success = false;
                accessResult.data["error"] = $"Invalid action - {tstructData.Action.ToUpper()}.";
                return accessResult;
            }

            if (!string.IsNullOrEmpty(accessResult.error))
            {
                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");

                    accessResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }

                return accessResult;
            }
            else
            {

                var apiResult = new APIResult();

                if (tstructData.RapidSave == true)
                    apiResult = await ExecuteSubmitDataAPI(tstructData);
                else
                    apiResult = await ExecuteSaveDataAPI(tstructData);

                if (isTracing)
                {
                    _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(apiResult)}");
                    apiResult.data["trace"] = await _traceService.GetTraceFile("AxDataPut");
                }



                return apiResult;

            }

        }

        private async Task<APIResult> CheckTstructCreateAccess(TstructData tstructData)
        {
            var result = new APIResult();

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

                if (!string.IsNullOrEmpty(create) && create.ToUpper() == "T")
                {
                    canCreate = true;
                    break;
                }

            }

            if (!canCreate)
            {
                result.success = false;
                result.error = "User does not have permissions to create this record.";
                if (_traceService.IsTracingEnabled())
                    result.data["permissions"] = JsonConvert.SerializeObject(userPermissions.data);
                return result;
            }

            var tstructValidation = await ValidateCreateJson(tstructData);

            if (!tstructValidation.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructValidation.Errors);
            }

            return result;
        }

        private async Task<SQLResult> GetCreatePermission(TstructData tstructData)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(tstructData.Project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_TSTRUCTCREATEPERMISSION.ToString();
            string[] paramName = { "@transid", "@username", "@roles" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            if (tstructData.Roles?.Count == 0)
                tstructData.Roles.Add("All");

            object[] paramValue = { tstructData.TransId.ToLower(), tstructData.UserName.ToString().ToLower(), string.Join(",", tstructData.Roles) };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        private async Task<SQLResult> GetTstructRecordIDs(TstructData tstructData)
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

        private async Task<TstructDataValidationResult> ValidateCreateJson(TstructData tstructData)
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

        private async Task<APIResult> CheckTstructEditAccess(TstructData tstructData)
        {
            /*
            ADS name, Transid, Param - Recordid or KeyFieldName and KeyFieldValue
            Get the records for this ADS from AxUserPermissions & AxPermissions table where Access=Edit/Delete/View depending on action for this user's roles. If no rows returned, raise exception "No access"
            Get the Owner & TransAttribute field from Transid_UserControl table for the given record.
            "CanView=false, ShouldExclude=''
            For every row in User Access Data Result 
              if role=self and owner <> loginuser, skip to next row 
              else If constraint is not empty and any CS Value is not found in TransAttribute, skip to next row
              if access=view set CanView to true 
                 if ShouldExclude is not none then ShouldExlcude = ShouldExclude + value in Exclude column 
                else ShouldExclude = None
            End For
            If CanView is false, raise exception ""access not given"""
            Fire SQL in ADS. Remove all columns that are in ShouldExclude and return result JSON

            */

            var result = new APIResult();
            List<string> excludedDCElements = new List<string>();
            List<string> includeDCElements = new List<string>();
            List<string> excludedElements = new List<string>();
            List<string> includeElements = new List<string>();


            var config = new MapperConfiguration(cfg => cfg.CreateMap<TstructData, TstructGetData>());
            var mapper = config.CreateMapper();
            TstructGetData getData = mapper.Map<TstructGetData>(tstructData);

            var userPermissions = await GetUserPermissionsForTstruct(getData);

            if (userPermissions == null || userPermissions.data == null || userPermissions.data.Rows.Count == 0)
            {
                result.success = false;
                result.error = "User does not have access to this structure.";
                return result;
            }

            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();
            var recordid = "";
            foreach (DataRow dr in userPermissions.data.Rows)
            {
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
                        excludedDCElements.AddRange(excludeDCs.Split(',').Where(x => !excludedDCElements.Contains(x)));
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
                        includeDCElements.AddRange(includeDCs.Split(',').Where(x => !includeDCElements.Contains(x)));
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
                        excludedElements.AddRange(excludeElems.Split(',').Where(x => !excludedElements.Contains(x)));
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
                        includeElements.AddRange(includeElems.Split(',').Where(x => !includeElements.Contains(x)));
                        includeElements = includeElements.Distinct().ToList();
                    }
                    else
                    {
                        includeElements.Clear();
                        includeElements.Add(all);
                    }
                }
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
                _traceService.LogMessage($"RecordId - {recordid}");
            }

            result.data["ExcludedDCs"] = excludedDCElements.Distinct().ToList();
            result.data["IncludedDCs"] = includeDCElements.Distinct().ToList();
            result.data["ExcludedFields"] = excludedElements.Distinct().ToList();
            result.data["IncludedFields"] = includeElements.Distinct().ToList();
            result.data["RecordId"] = recordid;
            tstructData.RecordId = recordid;

            var tstructFieldsValidation = await ValidateIncludeExcludeFields(tstructData.SubmitData, excludedDCElements.Distinct().ToList(), includeDCElements.Distinct().ToList(), excludedElements.Distinct().ToList(),includeElements.Distinct().ToList());

            if (!tstructFieldsValidation.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructFieldsValidation.Errors);
            }

            var tstructValidation = await ValidateEditJson(tstructData);

            if (!tstructValidation.IsValid)
            {
                result.success = false;
                result.error = string.Join(",", tstructValidation.Errors);
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
                                    if(skipFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
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

        private async Task<TstructDataValidationResult> ValidateEditJson(TstructData tstructData)
        {
            var result = new TstructDataValidationResult();

            try
            {
                tstructData.DCRows = GetDCWiseRowNumbers(tstructData.SubmitData);
                var dcRowData = new SQLResult();
                if (!string.IsNullOrEmpty(tstructData.DCRows))
                    dcRowData = await GetTstructRecordIDs(tstructData);

                if (!tstructData.SubmitData.ContainsKey("dataarray"))
                {
                    var tempSubmitData = tstructData.SubmitData;
                    tempSubmitData = AddRowNodesAndRecordIdsWithDataTable(tempSubmitData, dcRowData.data); //AddRowNodeIfMissing(tempSubmitData);
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


    }
}
