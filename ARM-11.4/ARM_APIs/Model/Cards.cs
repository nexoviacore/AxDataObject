namespace ARM_APIs.Model
{
    public class Cards
    {
        public string ARMSessionId { get; set; }
        public string? CardId { get; set; }
        public bool? IsHomePage { get; set; }
        public Dictionary<string, string>? SqlParams { get; set; }
        public Dictionary<string, string>? GlobalParams { get; set; }
        public string? Roles { get; set; }
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public bool? HomePageCards { get; set; }
        public bool? RefreshData { get; set; }
        public bool? IsMobile { get; set; }

        public bool? Trace { get; set; }

    }

    public class HTMLPlugin
    {
        public string ARMSessionId { get; set; }
        public string? Name { get; set; }
        public string? Context { get; set; }
        public string? HTMLText { get; set; }
        public string? Action { get; set; }
    }      
}
