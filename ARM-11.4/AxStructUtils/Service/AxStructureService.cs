using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;
using AxStructDef;


namespace AxStructUtils
{
    public class AxStructureService : IAxStructureService
    {
        private readonly ARMCommon.Helpers.Utils _common;
        private readonly IARMLogTraceService _traceService;
        private AxpertLog _axpertLog = new AxpertLog();
        private bool isTracing;

        public AxStructureService(ARMCommon.Helpers.Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        private async Task<APIResult> LoadStructure(AxStructure getStructure)
        {
            var structDef = await StructDef.LoadStructure(_common, getStructure.TransId, getStructure.Roles, _traceService, getStructure.RefreshCache);
            var result = new APIResult();
            if (structDef is null)
            {
                result = new APIResult(false, "Error in Tstruct Load Structure. StructDef Not Initialized");
            }
            else
            {               
                result.success = true;
                result.data["message"] = "Structure loaded successfully.";
                result.data["structure"] = structDef;
            }

            return result;
        }


        private async Task LogAxGetStructureDetails(AxStructure input, APIResult response)
        {
            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId; ;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = (response.success ? "success" : response.error);
            _axpertLog.RecordId = 0;
            _axpertLog.StructName = input.TransId;
            _axpertLog.ServiceName = "ARM - AxGetStructure API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }

        public async Task<APIResult> AxGetStructure(AxStructure getStructure)
        {
            var result = new APIResult();
            var isValidSession = await _common.ValidateSession(getStructure.ARMSessionId);
            if (!isValidSession)
            {
                result.success = false;
                result.data["error"] = $"SessionId is not valid";
                return result;
            }
            try
            {
                _axpertLog.CalledOn = DateTime.Now;

                isTracing = getStructure.Trace == true;
                _traceService.EnableTracing(isTracing);

                if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(getStructure)}");

                var validRoles = await _common.IsValidRole(getStructure.ARMSessionId, getStructure.Roles, _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString());

                if (!validRoles)
                {
                    result.success = false;
                    result.error = $"User does not access to some of the roles.";
                }
                else
                {
                    getStructure.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                    getStructure.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                    getStructure.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();

                    if (getStructure.Roles.Count == 0)
                    {
                        getStructure.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString().Split(",").ToList();
                    }

                    result = await LoadStructure(getStructure);
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

                result.error = "Error occurred in AxGet.";

            }
            finally
            {
                await LogAxGetStructureDetails(getStructure, result);
                await _common.CloseDBConnectionAsync();
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                result.data["trace"] = await _traceService.GetTraceFile($"AxGet-{getStructure.TransId}");
            }

            return result;

        }

    }
}
