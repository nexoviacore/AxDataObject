using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARM_APIs.Interface;
using ARM_APIs.Model;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;
using System.Data;
using AutoMapper;
using StackExchange.Redis;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace ARM_APIs.Service
{
    public class AxScriptService : IAxScriptService
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxScriptService(IARMLogTraceService traceService, Utils common)
        {
            _traceService = traceService;
            _common = common;
        }

        public async Task<APIResult> AxExecuteScriptViaRESTDLLAsync(AxScriptRequest request)
        {
            if (request.Trace)
            {
                _traceService.EnableTracing(true);
                _traceService.LogMessage("AxScript API (Via RESTDLL) call started.");
                _traceService.LogMessage($"Start Time: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(request)}");
            }

            var apiResult = new APIResult();
            try
            {
                Dictionary<string, object> sessionVars = await _common.GetSessionData(request.ARMSessionId);

                if (sessionVars == null || sessionVars.Count == 0)
                {
                    apiResult.success = false;
                    apiResult.data["error"] = $"Session data is not available.";
                    return apiResult;
                }

                // Extract session information
                request.Project = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                request.UserName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();

                apiResult = await AxExecuteScriptViaRESTDLL(request);
            }
            catch (Exception ex)
            {
                apiResult.success = false;
                apiResult.data["error"] = "Error occurred. Please check with administrator.";
                if (request.Trace) _traceService.LogError("Error in AxScript (Via RESTDLL): " + ex.Message + ex.StackTrace + ex.InnerException);
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }

            if (request.Trace)
            {
                _traceService.LogMessage("AxScript API Result JSON: " + JsonConvert.SerializeObject(apiResult));
                apiResult.data["Trace"] = await _traceService.GetTraceFile($"AxScript_RESTDLL-{request.Name ?? ""}-{request.Script ?? ""}");
            }
            return apiResult;
        }

        public async Task<APIResult> AxExecuteScriptViaRESTDLL(AxScriptRequest request)
        {
            var sessionData = await GetSessionDataForScript(request);
            var result = new APIResult();
            var scriptObj = new Dictionary<string, object>();
            var executeScriptObj = new Dictionary<string, object>();
            AxpertRestAPIToken axpertToken = new AxpertRestAPIToken(request.UserName);
            executeScriptObj["project"] = request.Project;
            executeScriptObj["token"] = axpertToken.token;
            executeScriptObj["seed"] = axpertToken.seed;
            executeScriptObj["s"] = sessionData["AxSessionId"];
            executeScriptObj["username"] = request.UserName;
            executeScriptObj["userauthkey"] = axpertToken.userAuthKey;
            executeScriptObj["script"] = request.Script;
            executeScriptObj["type"] = request.Type;
            executeScriptObj["name"] = request.Name;
            executeScriptObj["recordid"] = request.RecordId?.ToString() ?? "0";
            executeScriptObj["trace"] = request.Trace.ToString().ToLower();
            scriptObj["executescript"] = executeScriptObj;

            if (request.APIParams != null && request.APIParams.Count > 0)
            {
                scriptObj["apiparams"] = request.APIParams;
            }

            if (sessionData.ContainsKey("AxApps"))
                scriptObj["axapps"] = sessionData["AxApps"];

            if (sessionData.ContainsKey("AxProps"))
                scriptObj["axprops"] = sessionData["AxProps"];

            if (sessionData.ContainsKey("GlobalVars"))
                scriptObj["globalvars"] = sessionData["GlobalVars"];

            if (sessionData.ContainsKey("UserVars"))
                scriptObj["uservars"] = sessionData["UserVars"];

            string armScriptsURL = _common.GetARMScriptsURL(request.Project);
            string apiUrl = armScriptsURL + "/ASBScriptRest.dll/datasnap/rest/TASBScriptRest/executescript";
            var api = new API();
            string apiInput = JsonConvert.SerializeObject(scriptObj);
            if (request.Trace)
            {
                _traceService.LogMessage("API URL: " + apiUrl);
                _traceService.LogMessage("API JSON: " + apiInput);
            }
            var apiResult = await api.POSTData(apiUrl, apiInput, "application/json");

            if (request.Trace) _traceService.LogMessage("REST API Result JSON: " + JsonConvert.SerializeObject(apiResult));

            return _common.ParseAxpertRestAPIResult(apiResult);

        }

        private async Task<Dictionary<string, string>> GetSessionDataForScript(AxScriptRequest taskAction)
        {
            var isValidSession = await _common.ValidateSession(taskAction.ARMSessionId);
            if (!isValidSession)
            {
                throw new Exception($"SessionId is not valid");
            }

            var sessionData = new Dictionary<string, string>();
            sessionData["AxSessionId"] = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();
            taskAction.Project = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();

            var userVars = _common._sessionVars[SESSION_DATA.USERVARS.ToString()].ToString();
            if (userVars == "{}")
                sessionData["UserVars"] = "<uservars></uservars>";
            else
            {
                XmlDocument userVarsDoc = JsonConvert.DeserializeXmlNode("{\"uservars\": " + userVars + "}");
                sessionData["UserVars"] = userVarsDoc.OuterXml;
            }

            var gloVars = _common._sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString();
            if (gloVars == "{}")
                sessionData["GlobalVars"] = "<globalvars></globalvars>";
            else
            {
                XmlDocument gloVarsDoc = JsonConvert.DeserializeXmlNode("{\"globalvars\": " + gloVars + "}");
                sessionData["GlobalVars"] = gloVarsDoc.OuterXml;
            }

            var axApps = await _common.GetAxApps(taskAction.Project);
            var axProps = await _common.GetAxProps(taskAction.Project);

            sessionData["AxApps"] = axApps;
            sessionData["AxProps"] = axProps;

            return sessionData;

        }
    }
}
