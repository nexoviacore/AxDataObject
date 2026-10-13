namespace ARMCommon.Model
{
    public class ARMPublishedAPI
    {
        public string? ARMSessionId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string? PublicKey { get; set; }
        public string? SecretKey { get; set; }
        public string? ApiRequeststring { get; set; }
        public string? ApiSuccess { get; set; }
        public string? ApiError { get; set; }
        public string? ApiType { get; set; }
        public string? StructCap { get; set; }
        public string? ObjName { get; set; }
        public string? ScriptCap { get; set; }
        public string? PrintForm { get; set; }
        public string? UnameUi { get; set; }
        public string? Uname { get; set; }
        public Dictionary<string, string>? SQLParams { get; set; }
        public Dictionary<string, object>? SubmitData { get; set; }
        public Dictionary<string, object>? GetReportParams { get; set; }
        public Dictionary<string, object>? GetReport { get; set; }
        public Dictionary<string, object>? GetPrintForm { get; set; }
        public Dictionary<string, object>? GetSqlData { get; set; }
        public Dictionary<string ,object>? ExecuteScript { get; set; }

        public string? recordid { get; set; }
        public string? type { get; set; }
        public bool? trace { get; set; }


    }

    public class ARMAPIDetails
    {
        public string? AppName { get; set; }
        public string? PublicKey { get; set; }
        public string? SecretKey { get; set; }
        public string? ApiRequeststring { get; set; }
        public string? ApiSuccess { get; set; }
        public string? ApiError { get; set; }
        public string? ApiType { get; set; }
        public string? StructCap { get; set; }
        public string? ObjName { get; set; }
        public string? ScriptCap { get; set; }
        public string? PrintForm { get; set; }
        public string? UnameUi { get; set; }
        public string? Uname { get; set; }
        public Dictionary<string, string>? SQLParams { get; set; }


        


    }
}
