namespace AxTstructData
{

    public class TstructDataValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class AxGet
    {
        public string ARMSessionId { get; set; }
        public string TransId { get; set; }
        public string? AxSessionId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public Int64? RecordId { get; set; } = 0;
        public string Action { get; set; } = "view";
        public bool? Trace { get; set; }
        public string? KeyField { get; set; }
        public string? KeyValue { get; set; }
        public List<string>? Roles { get; set; } = new List<string>();
        public bool CachePermissions { get; set; } = true;
        public string CalledFrom { get; set; } = string.Empty;
        public bool AxpertWSFormat { get; set; } = false;
        public string AxClient_DateFormat { get; set; } = "";
        public string Dimensions { get; set; } = "";
        public bool MilliSecsInTimeStamp { get; set; } = false;
    }

    public class AxPutInput
    {
        public string ARMSessionId { get; set; }
        public string AxSessionId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public bool? Trace { get; set; }
        public bool ValidateOnly { get; set; } = false;
        public string AxClient_DateFormat { get; set; } = "";
        public List<string>? Roles { get; set; } = new List<string>();
        public bool MilliSecsInTimeStamp { get; set; } = false;
        public List<AxPut> Data { get; set; }

    }
    public class AxPut
    {
        public string TransId { get; set; }

        public Int64 RecordId { get; set; } = 0;
        public string Action { get; set; } = "";
        public string? KeyField { get; set; } = "";
        public string? KeyValue { get; set; } = "";
        public string? UserName { get; set; }
        public string? ModifiedOn { get; set; } = "";
        public string? ReturnField { get; set; } = "";
        public List<string>? Roles { get; set; } = new List<string>();
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> SubmitData { get; set; } = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        public string Dimensions { get; set; } = "";
        public List<AxAttachments> Attachments { get; set; } = new List<AxAttachments>();
    }

    public class AxDelete
    {
        public string ARMSessionId { get; set; }
        public string AxSessionId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public bool? Trace { get; set; }
        public string TransId { get; set; }

        public Int64 RecordId { get; set; } = 0;
        public string Action { get; set; } = "";
        public string? KeyField { get; set; } = "";
        public string? KeyValue { get; set; } = "";
        public List<string>? Roles { get; set; } = new List<string>();

        public string? Remarks { get; set; }
        public string Dimensions { get; set; } = "";
        public string? ModifiedOn { get; set; } = "";
        public bool MilliSecsInTimeStamp { get; set; } = false;
    }

    public class AxUserPermission
    {
        public string ARMSessionId { get; set; }
        public string AxSessionId { get; set; }
        public string? Project { get; set; }
        public bool? Trace { get; set; }
        public string TransId { get; set; }
        public bool CachePermissions { get; set; } = true;

        public Int64 RecordId { get; set; } = 0;
        public string Action { get; set; } = "";
        public string? KeyField { get; set; } = "";
        public string? KeyValue { get; set; } = "";
        public string? UserName { get; set; }
        public List<string>? Roles { get; set; } = new List<string>();
        public string Dimensions { get; set; } = "";
    }


    public class AxAttachments
    {
        public string AttachmentType { get; set; }
        public string TransId { get; set; }
        public string FieldName { get; set; }
        public Int64 RecordId { get; set; } = 0;
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; }
        public string DataFormat { get; set; }
        public bool Delete { get; set; } = false;
    }

    public class AxAttachmentResponse
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string AttachmentType { get; set; }
        public string DataFormat { get; set; }
        public string FileData { get; set; }
    }

}
