using Microsoft.AspNetCore.Mvc;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Filter;
using ARMCommon.Model;
using ARM_APIs.Interface;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using ARM_APIs.Model;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using ARMCommon.ActionFilter;
using System.Diagnostics;
using System.Data;
using NPOI.SS.Formula.Functions;

namespace ARM_APIs.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMPEGController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IRedisHelper _redis;
        private readonly IARMPEG _process;

        public ARMPEGController(IRedisHelper redis, IConfiguration config, IARMPEG process)
        {
            _redis = redis;
            _config = config;
            _process = process;
        }


        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetActiveTasks")]
        public async Task<IActionResult> ARMGetActiveTasks(ARMAxpertConnect axpert)
        {
            if (string.IsNullOrEmpty(axpert.AppName))
            {
                axpert.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var activeTasks = await _process.GetActiveTasksList(axpert.ARMSessionId, axpert.AppName);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", JsonConvert.SerializeObject(activeTasks));
            return Ok(result);

        }
        /*[RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetCompletedTasks")]
        public async Task<IActionResult> ARMGetCompletedTasks(ARMAxpertConnect axpert)
        {
            if (string.IsNullOrEmpty(axpert.AppName))
            {
                axpert.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var activeTasks = await _process.GetCompletedTasksList(axpert.ARMSessionId, axpert.AppName);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", JsonConvert.SerializeObject(activeTasks));
            return Ok(result);
        }*/

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetBulkActiveTasks")]
        public async Task<IActionResult> ARMGetBulkActiveTasks(ARMTask task)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(task.AxSessionId, task.ARMSessionId);
            if (string.IsNullOrEmpty(task.AppName))
            {
                axpert.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var activeTasks = await _process.GetBulkActiveTasks(task);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest(1068);
            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", activeTasks);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName")]
        [HttpPost("ARMGetProcessFlow")]
        public async Task<IActionResult> ARMGetProcessFlow(ARMProcessFlowTask processFlow)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(processFlow.AxSessionId, processFlow.ARMSessionId);
            if (string.IsNullOrEmpty(processFlow.AppName))
            {
                processFlow.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var processDetails = await _process.GetProcessDetails(processFlow);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "TaskId", "ToUser")]
        [HttpPost("ARMGetProcessTask")]
        public async Task<IActionResult> ARMGetProcessTask(ARMProcessFlowTask processTask)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(processTask.AxSessionId, processTask.ARMSessionId);

            if (string.IsNullOrEmpty(processTask.AppName))
            {
                processTask.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var processDetails = await _process.GetProcessTask(processTask);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "ToUser")]
        [HttpPost("ARMGetProcessKeyValues")]
        public async Task<IActionResult> ARMGetProcessKeyValues(ARMProcessFlowTask processTask)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(processTask.AxSessionId, processTask.ARMSessionId);
            if (string.IsNullOrEmpty(processTask.AppName))
            {
                processTask.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var processDetails = await _process.GetProcessKeyValues(processTask);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "RecordId")]
        [HttpPost("ARMGetKeyValue")]
        public async Task<IActionResult> ARMGetKeyValue(ARMProcessFlowTask processTask)
        {

            ARMAxpertConnect axpert = new ARMAxpertConnect(processTask.AxSessionId, processTask.ARMSessionId);

            if (string.IsNullOrEmpty(processTask.AppName))
            {
                processTask.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            var processDetails = await _process.GetKeyValue(processTask);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "TaskType", "TaskId")]
        [HttpPost("ARMDoTaskAction")]
        public async Task<IActionResult> ARMDoTaskAction(ARMTaskAction taskAction)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(taskAction.AxSessionId, taskAction.ARMSessionId);
            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            if (string.IsNullOrEmpty(taskAction.User))
            {
                taskAction.User = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }

            taskAction = await _process.GetTaskActionDetails(taskAction);
            var activeTask = await _process.GetActiveTask(axpert.ARMSessionId, taskAction.TaskId, taskAction.TaskType, taskAction.AppName);
            if (activeTask?.TaskId == null)
            {
                var taskStatus = await _process.GetTaskStatus(taskAction);

                if (taskStatus == null || !string.IsNullOrEmpty(taskStatus.error))
                {
                    return BadRequest(1071);
                }
                else if (taskStatus.data.Rows.Count > 0)
                {
                    if (Constants.AXTASKS.SKIP.ToString() == taskAction.Action.ToUpper())
                    {
                        ARMResult skipResult = new ARMResult();
                        skipResult.result.Add("message", "PEG_SKIPPED");
                        return Ok(skipResult);
                    }
                    else
                    {
                        var message = String.Format("This task has been {0} by {1} on {2}", taskStatus.data.Rows[0][0], taskStatus.data.Rows[0][1], taskStatus.data.Rows[0][2]);
                        ARMResult result = new ARMResult();
                        result.result.Add("message", message);
                        result.result.Add("messagetype", "Custom");
                        return BadRequest(result);
                    }
                }
            }

            if (activeTask?.InitiatorApproval == "F" && activeTask.ToUser == activeTask.Initiator)
            {
                return BadRequest("INITIATOR_CANNOT_APPROVE");
            }

            string taskResult;
            string resultMsg = "";
            if (Constants.AXTASKS.APPROVE.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ApproveTask(activeTask, taskAction);
                resultMsg = "PEG_APPROVED";
            }
            else if (Constants.AXTASKS.REJECT.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.RejectTask(activeTask, taskAction);
                resultMsg = "PEG_REJECTED";
            }
            else if (Constants.AXTASKS.RETURN.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ReturnTask(activeTask, taskAction);
                resultMsg = "PEG_RETURNED";
            }
            else if (Constants.AXTASKS.FORWARD.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ForwardTask(activeTask, taskAction);
                resultMsg = "PEG_FORWARDED";
            }
            else if (Constants.AXTASKS.CHECK.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.CheckTask(activeTask, taskAction);
                resultMsg = "PEG_CHECKED";
            }
            else if (Constants.AXTASKS.SEND.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.SendTask(activeTask, taskAction);
                resultMsg = "PEG_SENT";
            }
            else if (Constants.AXTASKS.SKIP.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.SkipTask(activeTask, taskAction);
                resultMsg = "PEG_SKIPPED";
            }
            else if (Constants.AXTASKS.APPROVETO.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ApproveToTask(activeTask, taskAction);
                resultMsg = "PEG_APPROVED";
            }
            else if (Constants.AXTASKS.RETURNTO.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ReturnToTask(activeTask, taskAction);
                resultMsg = "PEG_RETURNED";
            }
            else
            {
                return BadRequest(1072);
            }

            if (taskResult == Constants.RESULTS.SUCCESS.ToString())
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", resultMsg);
                return Ok(result);
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", taskResult);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "TaskType")]
        [HttpPost("ARMDoBulkAction")]
        public async Task<IActionResult> ARMDoBulkAction(ARMTaskAction taskAction)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(taskAction.AxSessionId, taskAction.ARMSessionId);
            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            taskAction = await _process.GetTaskActionDetails(taskAction);
            string taskResult;
            string resultMsg = "";
            if (Constants.AXTASKS.BULKAPPROVE.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.BulkApprovalTask(taskAction);
                resultMsg = "PEG_BULKAPPROVED";
            }
            else
            {
                return BadRequest("INVALIDTASKTYPE");
            }

            if (taskResult == Constants.RESULTS.SUCCESS.ToString())
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", resultMsg);
                return Ok(result);
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", taskResult);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName")]
        [HttpPost("ARMGetAxProcess")]
        public async Task<IActionResult> ARMGetAxProcess(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);

            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var processDetails = await _process.GetAxProcess(process);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName")]
        [HttpPost("ARMGetAxProcessDefinition")]
        public async Task<IActionResult> ARMGetAxProcessDefinition(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);

            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var processDetails = await _process.GetProcessDefinition(process);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName")]
        [HttpPost("ARMGetAxProcessList")]
        public async Task<IActionResult> ARMGetAxProcessList(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);

            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var processDetails = await _process.GetProcessList(process);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "KeyValue")]
        [HttpPost("ARMGetAxProcessDetail")]
        public async Task<IActionResult> ARMGetAxProcessDetail(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);

            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var processDetails = await _process.GetProcessDetail(process);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", processDetails);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "Datasources")]
        [HttpPost("ARMGetDataSourcesParams")]
        public async Task<IActionResult> ARMGetDataSourcesParams(ARMAxpertConnect axpert)
        {
            ARMResult result;
            if (axpert.Datasources == null || axpert.Datasources.Count == 0)
            {
                result = new ARMResult(false, "Required fields (Datasources) is missing in the input.");
                return BadRequest(JsonConvert.SerializeObject(result));
            }

            if (string.IsNullOrEmpty(axpert.AppName))
            {
                axpert.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var datasource in axpert.Datasources)
            {
                var paramList = await _process.GetDataSourceSQLQuery(axpert.AppName, datasource);
                results.Add(datasource, paramList.Split("~~")[1]);
            }

            result = new ARMResult();
            result.result.Add("success", true);
            result.result.Add("data", JsonConvert.SerializeObject(results));
            return Ok(JsonConvert.SerializeObject(result));

        }

        [RequiredFieldsFilter("ARMSessionId", "Datasources")]
        [HttpPost("ARMGetDataSourcesData")]
        public async Task<IActionResult> ARMGetDataSourcesData(ARMProcessFlowTask process)
        {
            if (process.Datasources == null || process.Datasources.Count == 0)
            {
                return BadRequest("Datasource name is mandatory");
            }
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var taskParams = await _process.GetActiveTaskParams(process);

            foreach (KeyValuePair<string, string> kvp in taskParams)
            {
                process.SqlParams.Add(kvp.Key, kvp.Value);
            }

            //axpert.SqlParams.Add("keyvalue", activeTask.KeyValue);

            Dictionary<string, Object> results = new Dictionary<string, Object>();
            foreach (var datasource in process.Datasources)
            {
                var data = await _process.GetDataSourceData(process.AppName, datasource, process.SqlParams);
                results.Add(datasource, data);

            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", results);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "KeyValue")]
        [HttpPost("ARMGetTimelineData")]
        public async Task<IActionResult> ARMGetTimelineData(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);

            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var timelineData = await _process.GetTimelineData(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", timelineData);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "KeyValue", "TaskId", "TaskType", "TaskName")]
        [HttpPost("ARMGetSendToUsers")]
        public async Task<IActionResult> ARMGetSendToUsers(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var activeTask = await _process.GetActiveTask(process.ARMSessionId, process.TaskId, process.TaskType, process.AppName);
            if (activeTask?.TaskId == null)
            {
                return BadRequest(1071);
            }

            process.SendToActor = (string.IsNullOrEmpty(activeTask.SendToActor) ? "NA" : activeTask.SendToActor);
            process.SendToFlag = (string.IsNullOrEmpty(activeTask.SendToFlag?.ToString()) ? 1 : activeTask.SendToFlag);
            process.ProcessName = activeTask.ProcessName;

            var users = await _process.GetSendToUsers(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", users);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "ToUser")]
        [HttpPost("ARMGetProcessUserType")]
        public async Task<IActionResult> ARMGetProcessUserType(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var userType = await _process.GetProcessUserType(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", userType);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "ToUser", "KeyValue")]
        [HttpPost("ARMGetNextTaskInProcess")]
        public async Task<IActionResult> ARMGetNextTaskInProcess(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var userType = await _process.GetNextTaskInProcess(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", userType);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "ToUser", "KeyValue", "TaskName", "IndexNo")]
        [HttpPost("ARMGetEditableTask")]
        public async Task<IActionResult> ARMGetEditableTask(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var userType = await _process.GetEditableTask(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", userType);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "ToUser", "RecordId")]
        [HttpPost("ARMGetEditableForm")]
        public async Task<IActionResult> ARMGetEditableForm(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var userType = await _process.GetEditableForm(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", userType);
            return Ok(result);
        }


        [RequiredFieldsFilter("ARMSessionId", "TaskId", "ToUser")]
        [HttpPost("ARMGetOptionalTask")]
        public async Task<IActionResult> ARMGetOptionalTask(ARMProcessFlowTask process)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(process.AxSessionId, process.ARMSessionId);
            if (string.IsNullOrEmpty(process.AppName))
            {
                process.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            var isOptional = await _process.GetOptionalTask(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", isOptional);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "KeyValue", "User")]
        [HttpPost("ARMSkipOptionalTasks")]
        public async Task<IActionResult> ARMSkipOptionalTasks(ARMTaskAction taskAction)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(taskAction.AxSessionId, taskAction.ARMSessionId);
            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            if (string.IsNullOrEmpty(taskAction.User))
            {
                taskAction.User = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }

            taskAction = await _process.GetTaskActionDetails(taskAction);

            bool nextOptionalTaskIsActive = true;
            string lastIndexNo = "";
            while (nextOptionalTaskIsActive)
            {
                var optionalTasks = (DataTable)await _process.GetNextOptionalTask(taskAction);

                if (optionalTasks != null && optionalTasks.Rows.Count > 0)
                {
                    if (lastIndexNo == optionalTasks.Rows[0]["indexno"].ToString())
                    {
                        nextOptionalTaskIsActive = false;
                        break;
                    }
                    else
                        lastIndexNo = optionalTasks.Rows[0]["indexno"].ToString();

                    ARMTask optionalTask = new ARMTask();
                    optionalTask.TransId = optionalTasks.Rows[0]["transid"].ToString();
                    optionalTask.TaskId = optionalTasks.Rows[0]["taskid"].ToString();
                    optionalTask.TaskName = optionalTasks.Rows[0]["taskname"].ToString();
                    optionalTask.ProcessName = optionalTasks.Rows[0]["processname"].ToString();
                    optionalTask.KeyField = optionalTasks.Rows[0]["keyfield"].ToString();
                    optionalTask.KeyValue = optionalTasks.Rows[0]["keyvalue"].ToString();

                    var skipped = await _process.SkipTask(optionalTask, taskAction);
                    if (skipped != String.Empty)
                    {
                        nextOptionalTaskIsActive = true;
                    }

                }
                else
                    nextOptionalTaskIsActive = false;
            }


            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", "Skipped");
            return Ok(result);

        }

        #region Active List - HomePage
        [RequiredFieldsFilter("ARMSessionId", "PageSize", "PageNo")]
        [HttpPost("ARMGetPendingActiveTasks")]
        public async Task<IActionResult> ARMGetPendingActiveTasks(ARMProcessFlowTask processFlow)
        {
            ARMResult result = new ARMResult();
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var activeTasks = await _process.GetActiveTasksList(processFlow);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            result.result.Add("message", "SUCCESS");
            result.result.Add("pendingtasks", activeTasks);
            result.result.Add("pageno", processFlow.PageNo);

            if (processFlow.GetTaskDetails == true)
            {
                if (string.IsNullOrEmpty(processFlow.TaskId))
                {
                    if (activeTasks is DataTable dataTable && dataTable.Rows.Count > 0)
                    {
                        processFlow.ProcessName = dataTable.Rows[0]["processname"] as string;
                        processFlow.KeyValue = dataTable.Rows[0]["keyvalue"] as string;
                        processFlow.TaskId = dataTable.Rows[0]["taskid"] as string;
                        processFlow.TaskType = dataTable.Rows[0]["tasktype"] as string;
                    }
                }
                if (!string.IsNullOrEmpty(processFlow.TaskId))
                {
                    var processDetails = await _process.GetProcessDetails(processFlow);

                    string taskType = processFlow.TaskType.Trim().ToUpper();
                    object taskDetails = new object();
                    if (taskType == Constants.TASKTYPE.APPROVE.ToString() || taskType == Constants.TASKTYPE.CHECK.ToString())
                    {
                        taskDetails = await _process.GetProcessTask(processFlow);
                    }

                    result.result.Add("processflow", processDetails);
                    result.result.Add("taskdetails", taskDetails);
                }
            }


            if (processFlow.GetCount == true)
            {
                var count = await _process.GetActiveTasksCount(processFlow);
                result.result.Add("count", ((DataTable)count).Rows[0][0]);
            }
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "PageSize", "PageNo")]
        [HttpPost("ARMGetAllActiveTasks")]
        public async Task<IActionResult> ARMGetAllActiveTasks(ARMProcessFlowTask processFlow)
        {
            ARMResult result = new ARMResult();
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var allTasks = await _process.GetAllActiveTasks(processFlow);
            if (allTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            result.result.Add("message", "SUCCESS");
            result.result.Add("tasks", allTasks);

            return Ok(result);
        }


        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetCompletedTasks")]
        public async Task<IActionResult> ARMGetCompletedTasks(ARMProcessFlowTask processFlow)
        {
            ARMResult result = new ARMResult();
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var completedTasks = await _process.GetCompletedTasksList(processFlow);
            if (completedTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            result.result.Add("message", "SUCCESS");
            result.result.Add("completedtasks", completedTasks);

            if (processFlow.GetTaskDetails == true)
            {
                if (string.IsNullOrEmpty(processFlow.TaskId))
                {
                    if (completedTasks is DataTable dataTable && dataTable.Rows.Count > 0)
                    {
                        processFlow.ProcessName = dataTable.Rows[0]["processname"] as string;
                        processFlow.KeyValue = dataTable.Rows[0]["keyvalue"] as string;
                        processFlow.TaskId = dataTable.Rows[0]["taskid"] as string;
                        processFlow.TaskType = dataTable.Rows[0]["tasktype"] as string;
                    }
                }

                if (!string.IsNullOrEmpty(processFlow.TaskId))
                {
                    var processDetails = await _process.GetProcessDetails(processFlow);

                    string taskType = processFlow.TaskType.Trim().ToUpper();
                    object taskDetails = new object();
                    if (taskType == Constants.TASKTYPE.APPROVE.ToString() || taskType == Constants.TASKTYPE.CHECK.ToString())
                    {
                        taskDetails = await _process.GetProcessTask(processFlow);
                    }

                    result.result.Add("processflow", processDetails);
                    result.result.Add("taskdetails", taskDetails);
                }

            }

            if (processFlow.GetCount == true)
            {
                var count = await _process.GetCompletedTasksCount(processFlow);
                result.result.Add("count", ((DataTable)count).Rows[0][0]);
            }
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "TaskId", "KeyValue", "TaskType")]
        [HttpPost("ARMPEGGetTaskDetails")]
        public async Task<IActionResult> ARMPEGGetTaskDetails(ARMProcessFlowTask processFlow)
        {
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var processDetails = await _process.GetProcessDetails(processFlow);
            string taskType = processFlow.TaskType.Trim().ToUpper();
            object taskDetails = new object();
            if (taskType == Constants.TASKTYPE.APPROVE.ToString() || taskType == Constants.TASKTYPE.CHECK.ToString())
            {
                taskDetails = await _process.GetProcessTask(processFlow);
            }
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("processflow", processDetails);
            result.result.Add("taskdetails", taskDetails);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetPendingActiveTasksCount")]
        public async Task<IActionResult> ARMGetPendingActiveTasksCount(ARMProcessFlowTask processFlow)
        {
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var count = await _process.GetActiveTasksCount(processFlow);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", ((DataTable)count).Rows[0][0]);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetCompletedTasksCount")]
        public async Task<IActionResult> ARMGetCompletedTasksCount(ARMProcessFlowTask processFlow)
        {
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var count = await _process.GetCompletedTasksCount(processFlow);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", ((DataTable)count).Rows[0][0]);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "PageSize", "PageNo")]
        [HttpPost("ARMGetFilteredActiveTasks")]
        public async Task<IActionResult> ARMGetFilteredActiveTasks(ARMProcessFlowTask processFlow)
        {
            ARMResult result = new ARMResult();
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var activeTasks = await _process.GetFilteredActiveTasksList(processFlow);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            result.result.Add("message", "SUCCESS");
            result.result.Add("pendingtasks", activeTasks);

            if (processFlow.GetTaskDetails == true)
            {
                if (string.IsNullOrEmpty(processFlow.TaskId))
                {
                    if (activeTasks is DataTable dataTable && dataTable.Rows.Count > 0)
                    {
                        processFlow.ProcessName = dataTable.Rows[0]["processname"] as string;
                        processFlow.KeyValue = dataTable.Rows[0]["keyvalue"] as string;
                        processFlow.TaskId = dataTable.Rows[0]["taskid"] as string;
                        processFlow.TaskType = dataTable.Rows[0]["tasktype"] as string;
                    }
                }
                if (!string.IsNullOrEmpty(processFlow.TaskId))
                {
                    var processDetails = await _process.GetProcessDetails(processFlow);

                    string taskType = processFlow.TaskType.Trim().ToUpper();
                    object taskDetails = new object();
                    if (taskType == Constants.TASKTYPE.APPROVE.ToString() || taskType == Constants.TASKTYPE.CHECK.ToString())
                    {
                        taskDetails = await _process.GetProcessTask(processFlow);
                    }

                    result.result.Add("processflow", processDetails);
                    result.result.Add("taskdetails", taskDetails);
                }
            }


            if (processFlow.GetCount == true)
            {
                var count = await _process.GetActiveTasksCount(processFlow);
                result.result.Add("count", ((DataTable)count).Rows[0][0]);
            }
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetFilteredCompletedTasks")]
        public async Task<IActionResult> ARMGetFilteredCompletedTasks(ARMProcessFlowTask processFlow)
        {
            ARMResult result = new ARMResult();
            processFlow = await _process.GetProcessTaskDetails(processFlow);
            var completedTasks = await _process.GetFilteredCompletedTasksList(processFlow);
            if (completedTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest("PROCESSDATANOTAVAILABLE");
            }

            result.result.Add("message", "SUCCESS");
            result.result.Add("completedtasks", completedTasks);

            if (processFlow.GetTaskDetails == true)
            {
                if (string.IsNullOrEmpty(processFlow.TaskId))
                {
                    if (completedTasks is DataTable dataTable && dataTable.Rows.Count > 0)
                    {
                        processFlow.ProcessName = dataTable.Rows[0]["processname"] as string;
                        processFlow.KeyValue = dataTable.Rows[0]["keyvalue"] as string;
                        processFlow.TaskId = dataTable.Rows[0]["taskid"] as string;
                        processFlow.TaskType = dataTable.Rows[0]["tasktype"] as string;
                    }
                }

                if (!string.IsNullOrEmpty(processFlow.TaskId))
                {
                    var processDetails = await _process.GetProcessDetails(processFlow);

                    string taskType = processFlow.TaskType.Trim().ToUpper();
                    object taskDetails = new object();
                    if (taskType == Constants.TASKTYPE.APPROVE.ToString() || taskType == Constants.TASKTYPE.CHECK.ToString())
                    {
                        taskDetails = await _process.GetProcessTask(processFlow);
                    }

                    result.result.Add("processflow", processDetails);
                    result.result.Add("taskdetails", taskDetails);
                }

            }

            if (processFlow.GetCount == true)
            {
                var count = await _process.GetCompletedTasksCount(processFlow);
                result.result.Add("count", ((DataTable)count).Rows[0][0]);
            }
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetBulkApprovalCount")]
        public async Task<IActionResult> ARMGetBulkApprovalCount(ARMTask task)
        {
            if (string.IsNullOrEmpty(task.AppName))
            {
                task.AppName = _redis.HashGet(task.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }

            if (string.IsNullOrEmpty(task.ToUser))
            {
                task.ToUser = _redis.HashGet(task.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }
            var activeTasks = await _process.GetBulkApprovalCount(task);
            if (activeTasks.ToString() == Constants.RESULTS.NO_RECORDS.ToString())
            {
                return BadRequest(1068);
            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", activeTasks);
            return Ok(result);
        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "IndexNo")]
        [HttpPost("ARMGetApproveToTasks")]
        public async Task<IActionResult> ARMGetApproveToTasks(ARMProcessFlowTask process)
        {
            var tasks = await _process.GetApproveToTasks(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", tasks);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "ProcessName", "IndexNo")]
        [HttpPost("ARMGetReturnToTasks")]
        public async Task<IActionResult> ARMGetReturnToTasks(ARMProcessFlowTask process)
        {
            var tasks = await _process.GetReturnToTasks(process);

            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", tasks);
            return Ok(result);

        }

        [RequiredFieldsFilter("ARMSessionId", "TaskType", "TaskId")]
        [HttpPost("ARMMailDoTaskAction")]
        public async Task<IActionResult> ARMMailDoTaskAction(ARMTaskAction taskAction)
        {
            ARMAxpertConnect axpert = new ARMAxpertConnect(taskAction.AxSessionId, taskAction.ARMSessionId);
            if (string.IsNullOrEmpty(taskAction.AppName))
            {
                taskAction.AppName = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.APPNAME.ToString());
            }
            if (string.IsNullOrEmpty(taskAction.User))
            {
                taskAction.User = _redis.HashGet(axpert.ARMSessionId, Constants.SESSION_DATA.USERNAME.ToString());
            }

            taskAction = await _process.GetTaskActionDetails(taskAction);
            var activeTask = await _process.GetActiveTask(axpert.ARMSessionId, taskAction.TaskId, taskAction.TaskType, taskAction.AppName);
            if (activeTask?.TaskId == null)
            {
                var taskStatus = await _process.GetTaskStatus(taskAction);

                if (taskStatus == null || !string.IsNullOrEmpty(taskStatus.error))
                {
                    return BadRequest(1071);
                }
                else
                {
                    var message = String.Format("This task has been {0} by {1} on {2}", taskStatus.data.Rows[0][0], taskStatus.data.Rows[0][1], taskStatus.data.Rows[0][2]);
                    ARMResult result = new ARMResult();
                    result.result.Add("message", message);
                    result.result.Add("messagetype", "Custom");
                    return BadRequest(result);
                }
            }

            if (activeTask?.InitiatorApproval == "F" && activeTask.ToUser == activeTask.Initiator)
            {
                return BadRequest("INITIATOR_CANNOT_APPROVE");
            }

            var processtask = await _process.GetProcessTasks(taskAction.TaskId, taskAction.AppName, taskAction.User);
            string taskResult;
            string resultMsg = "";
            if (Constants.AXTASKS.APPROVE.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ApproveTask(activeTask, taskAction);
                resultMsg = "PEG_APPROVED";
            }
            else if (Constants.AXTASKS.REJECT.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.RejectTask(activeTask, taskAction);
                resultMsg = "PEG_REJECTED";
            }
            else if (Constants.AXTASKS.RETURN.ToString() == taskAction.Action.ToUpper())
            {
                taskResult = await _process.ReturnTask(activeTask, taskAction);
                resultMsg = "PEG_RETURNED";
            }
            else
            {
                return BadRequest(1072);
            }

            if (taskResult == Constants.RESULTS.SUCCESS.ToString())
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", resultMsg);
                result.result.Add("taskjson", processtask.data);
                return Ok(result);
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", taskResult);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
        }

       

        #endregion
    }
}
