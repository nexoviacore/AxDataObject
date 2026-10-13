namespace ARMCommon.Model
{
    public class ApiRequest
    {
        public string? Project { get; set; }
        public string? Url { get; set; }
        public string? Method { get; set; }
        public Dictionary<string, string>? ParameterString { get; set; }
        public Dictionary<string, string>? HeaderString { get; set; }
        public object? RequestString { get; set; }
        public Dictionary<string, string>? UrlParams { get; set; }
        public string? APIDesc { get; set; }

        public string? Status { get; set; }
        public string? Response { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

    }
}
