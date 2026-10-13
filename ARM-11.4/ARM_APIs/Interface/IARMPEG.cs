using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IARMPEG
    {
        abstract Task<bool> IsValidAxpertConnection(ARMAxpertConnect axpert);
        abstract Task<object> GetActiveTasksList(string armSessionId, string appName);
        abstract Task<DataTable> GetActiveTasksForUser(string userName, string appName);
        abstract Task<ARMTask> GetActiveTask(string sessionId, string taskId, string taskType, string appName);
        abstract Task<object> GetProcessDetails(ARMProcessFlowTask processFlow);
        abstract Task<object> GetProcessTask(ARMProcessFlowTask processFlow);
        abstract Task<object> GetProcessKeyValues(ARMProcessFlowTask processFlow);
        abstract Task<object> GetKeyValue(ARMProcessFlowTask processFlow);
        abstract Task<string> ApproveTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> RejectTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> ForwardTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> ReturnTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> CheckTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> SendTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> SkipTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> ApproveToTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<string> ReturnToTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<ARMTaskAction> GetTaskActionDetails(ARMTaskAction taskAction);
        abstract Task<object> GetAxProcess(ARMProcessFlowTask processFlow);
        abstract Task<object> GetProcessDefinition(ARMProcessFlowTask processFlow);
        abstract Task<object> GetProcessList(ARMProcessFlowTask processFlow);
        abstract Task<object> GetProcessDetail(ARMProcessFlowTask processFlow);
        abstract Task<string> GetDataSourceSQLQuery(string appName, string dataSource);
        abstract Task<object> GetDataSourceData(string appName, string datasource, Dictionary<string, string> sqlParams);
        abstract Task<Dictionary<string, string>> GetActiveTaskParams(ARMProcessFlowTask processFlow);
        abstract Task<object> GetTimelineData(ARMProcessFlowTask processFlow);
        abstract Task<object> GetSendToUsers(ARMProcessFlowTask processFlow);
        abstract Task<DataTable> GetBulkActiveTasks(ARMTask taskAction);
        abstract Task<string> BulkApprovalTask(ARMTaskAction taskAction);
        abstract Task<SQLResult> GetTaskStatus(ARMTaskAction taskAction);
        abstract Task<object> GetProcessUserType(ARMProcessFlowTask processFlow);
        abstract Task<object> GetNextTaskInProcess(ARMProcessFlowTask processFlow);
        abstract Task<object> GetEditableTask(ARMProcessFlowTask processFlow);
        abstract Task<object> GetEditableForm(ARMProcessFlowTask processFlow);

        abstract Task<object> IsEditableTask(ARMTask task, ARMTaskAction taskAction);
        abstract Task<object> GetOptionalTask(ARMProcessFlowTask processFlow);
        abstract Task<object> GetNextOptionalTask(ARMTaskAction taskAction);
        abstract Task<ARMProcessFlowTask> GetProcessTaskDetails(ARMProcessFlowTask taskAction);
        abstract Task<object> GetActiveTasksList(ARMProcessFlowTask processFlow);
        abstract Task<object> GetAllActiveTasks(ARMProcessFlowTask processFlow);
        abstract Task<object> GetCompletedTasksList(ARMProcessFlowTask processFlow);
        abstract Task<object> GetActiveTasksCount(ARMProcessFlowTask processFlow);
        abstract Task<object> GetCompletedTasksCount(ARMProcessFlowTask processFlow);
        abstract Task<object> GetFilteredActiveTasksList(ARMProcessFlowTask processFlow);
        abstract Task<object> GetFilteredCompletedTasksList(ARMProcessFlowTask processFlow);
        abstract Task<DataTable> GetBulkApprovalCount(ARMTask task);
        abstract Task<object> GetReturnToTasks(ARMProcessFlowTask processFlow);
        abstract Task<object> GetApproveToTasks(ARMProcessFlowTask processFlow);

        //abstract Task<List<Dictionary<string, object>>> GetProcessDataList(ARMProcessFlowTask processFlow, List<SQLResult> sqlresultlist, List<string> type);
        abstract Task<object> ValidateAndSaveSession(ARMMailTaskAction task);
        abstract Task<object> ValidateAndSaveSessionForViewRecord(ARMViewRecordDetails record);
        abstract Task<SQLResult> ValidatePowerUsers(string username, string appname);

        abstract Task<SQLResult> GetProcessTasks(string taskid, string appname, string user);

    }
}
