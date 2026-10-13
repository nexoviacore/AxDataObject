using System.Data;

namespace ARM_APIs.Model
{
    public class ARMTask
    {
        public string? AppName { get; set; }
        public string? AxSessionId { get; set; }
        public string? ARMSessionId { get; set; }
        public string? TaskId { get; set; }
        public string? TaskName { get; set; }
        public string? TaskType { get; set; }
        public string? ProcessName { get; set; }
        public string? TransId { get; set; }
        public string? KeyValue { get; set; }
        public string? KeyField { get; set; }
        public string? ToUser { get; set; }
        public string? SendToActor { get; set; }
        public int? SendToFlag { get; set; }
        public string? Initiator { get; set; }
        public string? InitiatorApproval { get; set; }
        public string? Amendment { get; set; }
        public string? RecordId { get; set; }

        public ARMTask()
        {
        }
        public ARMTask(DataRow drTask)
        {
            TaskId = drTask["TaskId"].ToString();
            TaskName = drTask["TaskName"].ToString();
            TaskType = drTask["TaskType"].ToString();
            ProcessName = drTask["ProcessName"].ToString();
            TransId = drTask["TransId"].ToString();
            KeyValue = drTask["KeyValue"].ToString();
            KeyField = drTask["KeyField"].ToString();
            ToUser = drTask["ToUser"].ToString();
            SendToActor = drTask["sendtoactor"].ToString();
            SendToFlag = (string.IsNullOrEmpty(drTask["allowsendflg"]?.ToString()) ? 1 : Convert.ToInt32(drTask["allowsendflg"]));
            Initiator = drTask["Initiator"].ToString();
            InitiatorApproval = drTask["Initiator_Approval"].ToString();
            Amendment = drTask["Amendment"].ToString();
            RecordId = drTask["RecordId"].ToString();
        }
    }

    public class ARMTaskAction
    {
        public string? AxSessionId { get; set; }
        public string? ARMSessionId { get; set; }
        public string? ProcessName { get; set; }
        public string? KeyValue { get; set; }
        public string? TaskId { get; set; }
        public string? Action { get; set; }
        public string? TaskType { get; set; }
        public string? User { get; set; }
        public string? AppName { get; set; }
        public string? Password { get; set; }
        public string? StatusReason { get; set; }
        public string? StatusText { get; set; }
        public string? ReturnTo { get; set; }
        public string? SendTo { get; set; }
        public bool? Trace { get; set; }
        public string? ToTask { get; set; }
    }
    public class ARMMailTaskAction
    {
        public string? TaskId { get; set; }
        public string? UserId { get; set; }
        public string? AppName { get; set; }
        public string? Password { get; set; }
        public string? Action { get; set; }
        public string? token { get; set; }
        public string? ARMSessionId { get; set; }

    }

    public class ARMViewRecordDetails
    {
        public string? TransId { get; set; }
        public string? RecordId { get; set; }
        public string? UserId { get; set; }
        public string? AppName { get; set; }
        public string? Password { get; set; }
        public string? Action { get; set; }
        public string? token { get; set; }
        public string? ARMSessionId { get; set; }        
        public string? Iname { get; set; }
    }

    public class ARMProcessFlowTask
    {
        public string? ARMSessionId { get; set; }
        public string? AxSessionId { get; set; }
        public string? ProcessName { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public string? TaskName { get; set; }
        public string? TaskId { get; set; }
        public string? TaskType { get; set; }
        public string? ToUser { get; set; }
        public string? RecordId { get; set; }
        public string? AxUserName { get; set; }
        public string? AppName { get; set; }
        public string? AxPassword { get; set; }
        public List<string>? Datasources { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; }
        public string? CardIds { get; set; }
        public int? SendToFlag { get; set; }
        public string? SendToActor { get; set; }
        public int? IndexNo { get; set; }
        public bool? GetTaskDetails { get; set; }
        public bool? GetCount { get; set; }
        public int? PageSize { get; set; }
        public int? PageNo { get; set; }
        public string? FromUser { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SearchText { get; set; }
        public string? TransId { get; set; }

        public string? Filter { get; set; }
    }
}
