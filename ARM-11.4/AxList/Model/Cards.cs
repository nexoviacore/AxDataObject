namespace AxList
{

    public class CardsData
    {
        public string ARMSessionId { get; set; }
        public string? Roles { get; set; }
        public string? UserName { get; set; }
        public string? AppName { get; set; }
        public string? SchemaName { get; set; }
        public bool HomePageCards { get; set; } = true;
        public bool RefreshData { get; set; } = false;
        public bool IsMobile { get; set; } = false;
        public bool Trace { get; set; } = false;        
    }
}
