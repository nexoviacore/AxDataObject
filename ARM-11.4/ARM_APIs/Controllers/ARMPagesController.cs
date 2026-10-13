using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMPagesController : ControllerBase
    {

        private readonly IARMLogin _login;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly IRedisHelper _redis;
        private readonly INotificationHelper _notification;
        private readonly IARMPages _pages;
        private readonly IARMPEG _process;
        public ARMPagesController(IARMLogin login, IRedisHelper redis, IConfiguration config, INotificationHelper notification, IARMPages pages, IARMPEG process)
        {
            _login = login;
            _redis = redis;
            _config = config;
            _notification = notification;
            _pages = pages;
            _process = process; 
        }

        [AllowAnonymous]
        [HttpGet("ARMPages")]
        public async Task<IActionResult> ARMPages()
        {
            string pageName = HttpContext.Request.Query["page"].ToString();
            string sessionId = HttpContext.Request.Query["session"].ToString();
            string html = "";
            bool isError = false;
            if (string.IsNullOrEmpty(pageName) || string.IsNullOrEmpty(sessionId))
            {
                html = "<h1>Error: Required fields (Page/Session details) is missing in the input.</h1>";
                isError = true;
            }

            if (!isError && !_pages.SessionExists(sessionId))
            {
                html = "<h1>Error: Session is not available. Please re-login and try again.</h1>";
                isError = true;
            }
            var appName = _redis.HashGet(sessionId, Constants.SESSION_DATA.APPNAME.ToString());
            if (!isError && string.IsNullOrEmpty(appName))
            {
                html = "<h1>Error: App is not available. Please check with administrator.</h1>";
                isError = true;
            }
            if (!isError)
            {
                html = _pages.GetPage(appName, pageName);
                if (html == Constants.RESULTS.NO_RECORDS.ToString())
                {
                    html = "<h1>Error: Page is not available. Please check with administrator.</h1>";
                }
                else
                {
                    var armToken = _redis.HashGet(sessionId, Constants.SESSION_DATA.ARMTOKEN.ToString());
                    if (!string.IsNullOrEmpty(armToken) && html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        var url = _config["AxpertWeb_URL_" + appName];

                        html = Regex.Replace(html, "</body>", $"<script type='text/javascript'>var armToken='{armToken}';var armSessionId='{sessionId}';var axpertUrl='{url}';</script></body>", RegexOptions.IgnoreCase);
                    }
                }
            }
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }

        [AllowAnonymous]
        [HttpGet("ARMMailTaskAction")]
        public async Task<IActionResult> ARMMailTaskAction()
        {
            string taskid = HttpContext.Request.Query["TaskId"].ToString();
            string userid = HttpContext.Request.Query["userid"].ToString();
            string appname = HttpContext.Request.Query["appname"].ToString();
            string html = "";
            bool isError = false;
            if (string.IsNullOrEmpty(taskid) || string.IsNullOrEmpty(appname) || string.IsNullOrEmpty(userid))
            {
                html = "<h1>Error: Required fields (TaskId/UserId/AppName details) is missing in the input.</h1>";
                isError = true;
            }
            SQLResult poweruser = new SQLResult();
            poweruser = await _process.ValidatePowerUsers(userid, appname);
            if (!isError && !string.IsNullOrEmpty(poweruser.error))
            {
                html = "<h1>Invalid User/Project Details</h1>";
                isError = true;
            }
            if (!isError)
            {
                SQLResult sqlresult = new SQLResult();
                string taskstatus = "";
                sqlresult = await _process.GetProcessTasks(taskid,appname,userid);
                if (!string.IsNullOrEmpty(sqlresult.error))
                {
                    html = $"<h1>Error:'exception' while fetching ProcessTask </h1>".Replace("exception", sqlresult.error);
                }
                else
                {
                    if (sqlresult.data is DataTable dataTable && dataTable.Rows.Count > 0)
                    {
                        taskstatus = dataTable.Rows[0]["taskstatus"] as string;
                    }

                    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                    html = $@"<html>
                        <head>
                            <meta charset=""utf-8"">
                            <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                            <title>Mail Approval</title>
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                            <link rel=""stylesheet"" href=""../../UI/axpertUI/plugins.bundle.css"">
                            <link rel=""stylesheet"" href=""../../UI/axpertUI/style.bundle.css"">
                            <link rel=""stylesheet"" href=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.css"">
                            <link rel=""stylesheet"" href=""../../css/mailApprove.css"">
                        </head>
                        <body>
                            <div class=""parent clearfix d-none"" id=""mailAuthentication"">
                            </div>
                            <div class=""content d-flex flex-column flex-column-fluid d-none"" id=""activeMailStatus"">
                            </div>
                           
                            <div id=""waitDiv"" class=""page-loader rounded-2 bg-radial-gradient"">
                                <div class=""loader-box-wrapper d-flex bg-white p-20 shadow rounded"">
                                    <span class=""loader""></span>
                                </div>
                            </div>

                            <script src=""../../UI/axpertUI/plugins.bundle.js""></script>
                            <script src=""../../UI/axpertUI/scripts.bundle.js""></script>
                            <script type=""text/javascript"" src=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.js""></script>
                            <script type=""text/javascript"" src=""../../Js/alerts.min.js""></script>
                            <script type=""text/javascript"" src=""../../Js/md5.min.js""></script>
                            <script type=""text/javascript"" src=""../../js/handlebars.js""></script>
                            <script type=""text/javascript"" src=""../../js/mailApprove.js?v={timestamp}""></script></body>";

                    if (!string.IsNullOrEmpty(taskstatus))
                    {
                        string tempHtml = "<script type='text/javascript'>var taskstatus='completed';var taskdetails='$taskdetails$';</script></html>";
                      html = html + tempHtml.Replace("$taskdetails$", JsonConvert.SerializeObject(sqlresult.data));

                    }
                    else
                    {
                        string tempHtml = "<script type='text/javascript'>var taskstatus='pending';var taskdetails='$taskdetails$';</script></html>";
                        html = html + tempHtml.Replace("$taskdetails$", JsonConvert.SerializeObject(sqlresult.data));
                    }
                }
            }
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }

        [AllowAnonymous]
        [HttpGet("ARMViewRecord")]
        public async Task<IActionResult> ARMViewRecord()
        {            
            string userid = HttpContext.Request.Query["userid"].ToString();
            string appname = HttpContext.Request.Query["appname"].ToString();
            string transid = HttpContext.Request.Query["transid"].ToString();
            string recordid = HttpContext.Request.Query["recordid"].ToString();
            string html = "";
            bool isError = false;
            if (string.IsNullOrEmpty(transid) || string.IsNullOrEmpty(recordid) || string.IsNullOrEmpty(appname) || string.IsNullOrEmpty(userid))
            {
                html = "<h1>Error: Required fields (TransId/RecordId/UserId/AppName details) is missing in the input.</h1>";
                isError = true;
            }
            SQLResult poweruser = new SQLResult();
            poweruser = await _process.ValidatePowerUsers(userid, appname);
            if (!isError && !string.IsNullOrEmpty(poweruser.error))
            {
                html = "<h1>Invalid User/Project Details</h1>";
                isError = true;
            }
            if (!isError)
            {
                SQLResult sqlresult = new SQLResult();
                string taskstatus = "";
                                  
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                html = $@"<html>
                    <head>
                        <meta charset=""utf-8"">
                        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                        <title>View Record</title>
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                        <link rel=""stylesheet"" href=""../../UI/axpertUI/plugins.bundle.css"">
                        <link rel=""stylesheet"" href=""../../UI/axpertUI/style.bundle.css"">
                        <link rel=""stylesheet"" href=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.css"">
                        <link rel=""stylesheet"" href=""../../css/mailApprove.css"">
                    </head>
                    <body>
                        <div class=""parent clearfix d-none"" id=""mailAuthentication"">
                        </div>
                        <div class=""content d-flex flex-column flex-column-fluid d-none"" id=""activeMailStatus"">
                        </div>
                           
                        <div id=""waitDiv"" class=""page-loader rounded-2 bg-radial-gradient"">
                            <div class=""loader-box-wrapper d-flex bg-white p-20 shadow rounded"">
                                <span class=""loader""></span>
                            </div>
                        </div>

                        <script src=""../../UI/axpertUI/plugins.bundle.js""></script>
                        <script src=""../../UI/axpertUI/scripts.bundle.js""></script>
                        <script type=""text/javascript"" src=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.js""></script>
                        <script type=""text/javascript"" src=""../../Js/alerts.min.js""></script>
                        <script type=""text/javascript"" src=""../../Js/md5.min.js""></script>
                        <script type=""text/javascript"" src=""../../js/handlebars.js""></script>
                        <script type=""text/javascript"" src=""../../js/ViewRecord.js?v={timestamp}""></script></body>";

                    string tempHtml = "<script type='text/javascript'>var taskstatus='pending';var taskdetails='';</script></html>";
                    html = html + tempHtml.Replace("$taskdetails$", JsonConvert.SerializeObject(sqlresult.data));
            }
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }

        [AllowAnonymous]
        [HttpGet("ARMViewReport")]
        public async Task<IActionResult> ARMViewReport()
        {
            string userid = HttpContext.Request.Query["userid"].ToString();
            string appname = HttpContext.Request.Query["appname"].ToString();
            string iname = HttpContext.Request.Query["iname"].ToString();
            string html = "";
            bool isError = false;
            if (string.IsNullOrEmpty(iname) || string.IsNullOrEmpty(appname) || string.IsNullOrEmpty(userid))
            {
                html = "<h1>Error: Required fields (IName/UserId/AppName details) is missing in the input.</h1>";
                isError = true;
            }
            SQLResult poweruser = new SQLResult();
            poweruser = await _process.ValidatePowerUsers(userid, appname);
            if (!isError && !string.IsNullOrEmpty(poweruser.error))
            {
                html = "<h1>Invalid User/Project Details</h1>";
                isError = true;
            }
            if (!isError)
            {
                SQLResult sqlresult = new SQLResult();
                string taskstatus = "";

                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                html = $@"<html>
                    <head>
                        <meta charset=""utf-8"">
                        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                        <title>View Record</title>
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                        <link rel=""stylesheet"" href=""../../UI/axpertUI/plugins.bundle.css"">
                        <link rel=""stylesheet"" href=""../../UI/axpertUI/style.bundle.css"">
                        <link rel=""stylesheet"" href=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.css"">
                        <link rel=""stylesheet"" href=""../../css/mailApprove.css"">
                    </head>
                    <body>
                        <div class=""parent clearfix d-none"" id=""mailAuthentication"">
                        </div>
                        <div class=""content d-flex flex-column flex-column-fluid d-none"" id=""activeMailStatus"">
                        </div>
                           
                        <div id=""waitDiv"" class=""page-loader rounded-2 bg-radial-gradient"">
                            <div class=""loader-box-wrapper d-flex bg-white p-20 shadow rounded"">
                                <span class=""loader""></span>
                            </div>
                        </div>

                        <script src=""../../UI/axpertUI/plugins.bundle.js""></script>
                        <script src=""../../UI/axpertUI/scripts.bundle.js""></script>
                        <script type=""text/javascript"" src=""../../ThirdParty/jquery-confirm-master/jquery-confirm.min.js""></script>
                        <script type=""text/javascript"" src=""../../Js/alerts.min.js""></script>
                        <script type=""text/javascript"" src=""../../Js/md5.min.js""></script>
                        <script type=""text/javascript"" src=""../../js/handlebars.js""></script>
                        <script type=""text/javascript"" src=""../../js/ViewReport.js?v={timestamp}""></script></body>";

                string tempHtml = "<script type='text/javascript'>var taskstatus='pending';var taskdetails='';</script></html>";
                html = html + tempHtml.Replace("$taskdetails$", JsonConvert.SerializeObject(sqlresult.data));
            }
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }


        [RequiredFieldsFilter("TaskId", "UserId", "AppName", "Password")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMMailTaskActionDetails")]
        public async Task<IActionResult> ARMMailTaskActionDetails(ARMMailTaskAction task)
        {
            var taskdetails = await _process.ValidateAndSaveSession(task);
            if (taskdetails.ToString() == Constants.RESULTS.INVALIDPOWERUSER.ToString())
            {
                return BadRequest("INVALIDCREDENTIAL");
            }
            else
            {
                var processtask = await _process.GetProcessTasks(task.TaskId, task.AppName, task.UserId);
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("token", task.token);
                result.result.Add("ARMSessionId", task.ARMSessionId);
                result.result.Add("taskjson", processtask.data);
                return Ok(result);

            }
        }


        [RequiredFieldsFilter("TransId", "RecordId", "UserId", "AppName", "Password")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMViewRecordDetails")]
        public async Task<IActionResult> ARMViewRecordDetails(ARMViewRecordDetails record)
        {
            var taskdetails = await _process.ValidateAndSaveSessionForViewRecord(record);
            if (taskdetails.ToString() == Constants.RESULTS.INVALIDPOWERUSER.ToString())
            {
                return BadRequest("INVALIDCREDENTIAL");
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("token", record.token);
                result.result.Add("ARMSessionId", record.ARMSessionId);
                result.result.Add("taskjson", "");
                return Ok(result);

            }
        }

        [RequiredFieldsFilter("Iname", "UserId", "AppName", "Password")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMViewReportDetails")]
        public async Task<IActionResult> ARMViewReportDetails(ARMViewRecordDetails record)
        {
            var taskdetails = await _process.ValidateAndSaveSessionForViewRecord(record);
            if (taskdetails.ToString() == Constants.RESULTS.INVALIDPOWERUSER.ToString())
            {
                return BadRequest("INVALIDCREDENTIAL");
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("token", record.token);
                result.result.Add("ARMSessionId", record.ARMSessionId);
                result.result.Add("taskjson", "");
                return Ok(result);

            }
        }

    }
}
