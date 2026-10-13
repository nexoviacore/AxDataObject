namespace ARM_APIs.Model
{
    

    public class ADSDataRequest
    {
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string DataSource { get; set; }
        public string? ARMSessionId { get; set; }
        public Dictionary<string, string>? SQLParams { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
        public bool RefreshCache { get; set; } = false;
        public bool Trace { get; set; } = false;
        public string? SchemaName { get; set; }
        public string? Roles { get; set; }

    }
}
