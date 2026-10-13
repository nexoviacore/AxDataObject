using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;
using static ARMCommon.Helpers.Constants;


namespace AxUtils
{
    public class AxMenuService : IAxMenuService
    {
        private readonly IAxMenuRepository _repo;
        private readonly ARMCommon.Helpers.Utils _common;
        private readonly IARMLogTraceService _traceService;
        private bool isTracing;
        private AxpertLog _axpertLog = new AxpertLog();
        public AxMenuService(IAxMenuRepository repo, Utils common, IARMLogTraceService traceService)
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;
        }

        public async Task<ARMResult> AxGetMenuAsync(ARMSession menuInput)
        {
            _axpertLog.CalledOn = DateTime.Now;
            isTracing = menuInput.Trace == true;
            _traceService.EnableTracing(isTracing);
            var result = new ARMResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(menuInput)}");

            var isValidSession = await _common.ValidateSession(menuInput.ARMSessionId);
            if (!isValidSession)
            {
                return new ARMResult(false, "SessionId is not valid");
            }
            try
            {
                menuInput.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                menuInput.AxSessionId = _common._sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();
                menuInput.UserRoles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString();
                menuInput.UserResponsibilities = _common._sessionVars[SESSION_DATA.USER_GROUP.ToString()].ToString();

                result = await GetMenuList(menuInput);
            }
            catch (Exception ex)
            {
                result = new ARMResult(false, "Error occurred in AxMenuAsync.");

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
            }
            finally
            {
                await LogAxMenuDetails(menuInput, result);
                await _common.CloseDBConnectionAsync();
            }

            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                result.result["trace"] = await _traceService.GetTraceFile($"AxMenu");
            }


            return result;

        }

        private async Task<ARMResult> GetMenuList(ARMSession menuInput)
        {
            var result = new ARMResult();
            try
            {
                var userRoles = menuInput.UserRoles.Split(',').ToList();
                var userResponsibilities = menuInput.UserResponsibilities.Split(',').ToList();
                string allResponsibilities = string.Join(", ", userResponsibilities.Select(x => $"'{x}'"));

                var menuResult = await _repo.GetMenuFromDB(userRoles.Contains(Constants.Roles.DEFAULT.ToString().ToLower()), allResponsibilities);

                if (menuResult != null && string.IsNullOrEmpty(menuResult.error))
                {
                    if (menuResult.data.Rows.Count > 0)
                    {
                        // Adding a new column for URL
                        menuResult.data.Columns.Add(new DataColumn("url", typeof(string)));

                        SQLResult listViewAsDefaultResult = await _repo.GetListViewAsDefaultStructs();
                        Dictionary<string, string> pageConfigDict = ConvertSQLResultToDictionary(listViewAsDefaultResult);

                        foreach (DataRow row in menuResult.data.Rows)
                        {
                            string pageType = row["pagetype"].ToString().Trim();
                            if (string.IsNullOrEmpty(pageType)) continue;

                            string pagePrefix = pageType.Substring(0, 1);
                            string webPagePrefix = "h" + row["name"]?.ToString().Substring(2);

                            switch (pagePrefix)
                            {
                                case "i":
                                    row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{menuInput.ARMSessionId}&pname={pageType}";
                                    break;
                                case "t":

                                    if ((pageConfigDict.ContainsKey(pageType.Substring(1)) && pageConfigDict[pageType.Substring(1)] == "false") || (pageConfigDict.ContainsKey("ALL Forms") && pageConfigDict["ALL Forms"] == "false"))
                                        row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{menuInput.ARMSessionId}&pname={pageType}";
                                    else
                                        row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{menuInput.ARMSessionId}&pname=l{pageType.Substring(1)}";
                                    break;
                                case "w":
                                    row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{menuInput.ARMSessionId}&pname={webPagePrefix}";
                                    break;
                                default:
                                    row["url"] = "";
                                    break;
                            }
                        }
                    }

                    return new ARMResult
                    {
                        result = { { "success", true }, { "message", "SUCCESS" }, { "data", menuResult.data } }
                    };
                }
                else if (menuResult != null && !string.IsNullOrEmpty(menuResult.error))
                {
                    if (_traceService.IsTracingEnabled())
                    {
                        _traceService.LogError(menuResult.error);
                    }
                    result = new ARMResult(false, menuResult.error);
                }
                else if (menuResult == null)
                {
                    result = new ARMResult(false, "Error occurred in GetMenu.");
                }
            }
            catch (Exception ex)
            {
                result = new ARMResult(false, "Error occurred in GetMenu.");

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
            }

            return result;
        }

        private Dictionary<string, string> ConvertSQLResultToDictionary(SQLResult sqlResult)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            if (sqlResult != null && string.IsNullOrEmpty(sqlResult.error) && sqlResult.data.Rows.Count > 0)
            {
                foreach (DataRow row in sqlResult.data.Rows)
                {
                    string key = row["structname"]?.ToString();
                    string value = row["propsval"]?.ToString();

                    if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                    {
                        dict.Add(key, value);
                    }
                }
            }
            return dict;
        }

        private async Task LogAxMenuDetails(ARMSession input, ARMResult response)
        {
            _axpertLog.SessionId = input.AxSessionId.Length > 30 ? input.AxSessionId.Substring(0, 30) : input.AxSessionId;
            _axpertLog.Username = input.UserName;
            _axpertLog.ServiceResult = response.result.ContainsKey("message") ? response.result["message"].ToString() : "";
            _axpertLog.ServiceName = "ARM - AxMenu API";
            await _common.InsertIntoAxpertLog(_axpertLog);
        }

    }


}
