namespace AxStructUtils
{
    public class AxStructure
    {
        public string ARMSessionId { get; set; }
        public string AxSessionId { get; set; }
        public string TransId { get; set; }
        public string? Project { get; set; }
        public string? UserName { get; set; }
        public bool Trace { get; set; } = false;

        public List<string>? Roles { get; set; } = new List<string>();
        public bool RefreshCache { get; set; } = false;
    }
}
