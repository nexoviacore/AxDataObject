//using ARMCommon.Helpers;
//using ARMCommon.Interface;
//using ARMCommon.Model;
//using AxDataObject.Models;
//using AxDataObject.Interface;
//using Newtonsoft.Json;
//using static ARMCommon.Helpers.Constants;
//using AutoMapper;

//namespace AxDataObject.Service;

//public class AxUserPermissionService : IAxUserPermissionService
//{
//    private readonly IAxGetService _getService;
//    private readonly IAxPutService _putService;
//    private readonly Utils _common;
//    private readonly IARMLogTraceService _traceService;
//    private bool isTracing;

//    public AxUserPermissionService(Utils common, IARMLogTraceService traceService, IAxGetService getService, IAxPutService putService) 
//    {
//        _common = common;
//        _traceService = traceService;
//        _putService = putService;
//        _getService = getService;
//    }
    

//    public async Task<APIResult> GetUserPermission(AxUserPermission tstructData)
//    {
//        isTracing = tstructData.Trace == true;
//        _traceService.EnableTracing(isTracing);
//        var result = new APIResult();
//        if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(tstructData)}");

//        var isValidSession = await _common.ValidateSession(tstructData.ARMSessionId);
//        if (!isValidSession)
//        {
//            result.success = false;
//            result.data["error"] = $"SessionId is not valid";
//            return result;
//        }

//        try
//        {
//            var validRoles = await _common.IsValidRole(tstructData.ARMSessionId, tstructData.Roles, _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

//            if (!validRoles)
//            {
//                result.success = false;
//                result.data["error"] = $"User does not access to some of the roles.";
//                return result;
//            }

//            tstructData.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
//            tstructData.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
//            tstructData.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();

//            if (tstructData.Roles.Count == 0)
//            {
//                tstructData.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString().Split(",").ToList();
//            }

//            tstructData.Dimensions = _common.GetAxgApplicableDimensionsString();
            
//            if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.VIEW.ToString())
//            {
//                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxUserPermission, AxGet>());
//                var mapper = config.CreateMapper();
//                AxGet getData = mapper.Map<AxGet>(tstructData);

//                result = await _getService.CheckTstructViewAccess(getData);
//            }
//            else
//            {
//                var config = new MapperConfiguration(cfg => cfg.CreateMap<AxUserPermission, AxPut>());
//                var mapper = config.CreateMapper();
//                AxPut putData = mapper.Map<AxPut>(tstructData);

//                if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.CREATE.ToString())
//                {
//                    result = await _putService.CheckTstructCreateAccess(putData);
//                }
//                else if (tstructData.Action.ToUpper() == Constants.TSTRUCT_ACTIONS.EDIT.ToString())
//                {
//                    result = await _putService.CheckTstructEditAccess(putData);

//                    if (string.IsNullOrEmpty(result.error))
//                    {
//                        result.data["ExcludedDCs"] = string.Join(",", (List<string>)result.data["ExcludedDCs"]);
//                        result.data["IncludedDCs"] = string.Join(",", (List<string>)result.data["IncludedDCs"]);
//                        result.data["ExcludedFields"] = string.Join(",", (List<string>)result.data["ExcludedFields"]);
//                        result.data["IncludedFields"] = string.Join(",", (List<string>)result.data["IncludedFields"]);
//                    }
//                }
//                else
//                {
//                    result.success = false;
//                    result.error = $"Invalid action - {putData.Action.ToUpper()}.";
//                }
//            }
            

//        }
//        catch (Exception ex)
//        {
//            result = new APIResult();
//            result.success = false;

//            if (_traceService.IsTracingEnabled())
//            {
//                _traceService.LogError(ex.Message + ex.InnerException);
//            }

//            result.error = ex.Message;

//        }
//        finally
//        {
//            await _common.CloseDBConnectionAsync();
//        }

//        if (isTracing)
//        {
//            _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
//            result.data["trace"] = await _traceService.GetTraceFile($"AxPut");
//        }


//        return result;
//    }

//}


