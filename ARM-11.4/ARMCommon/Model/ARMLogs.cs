namespace ARMCommon.Model
{
    public class ARMLogs
    {
        public Guid Id { get; set; }
        public string? path { get; set; }
        public string? module { get; set; }
        public string? logdetails { get; set; }
        public string? username { get; set; }
        public string? logtime { get; set; }
        public string logtype { get; set; }

        public string? instanceid { get;set; }

    }
}
