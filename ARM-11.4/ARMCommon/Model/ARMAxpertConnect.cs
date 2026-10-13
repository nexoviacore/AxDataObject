namespace ARMCommon.Model
{
    public class ARMAxpertConnect
    {
        public string? Key { get; set; }
        public string? AxSessionId { get; set; }
        public string? ARMSessionId { get; set; }
        public string? User { get; set; }
        public string? AppName { get; set; }
        public List<string>? Datasources { get; set; }

        public string? Roles { get; set; }
        public string? SchemaName { get; set; }
        public string? Language { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
        public ARMAxpertConnect(string axSessionId, string armSessionId)
        {
            AxSessionId = axSessionId;
            ARMSessionId = armSessionId;
        }
    }
}
