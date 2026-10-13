namespace ARMCommon.Model
{
    public class TstructDef()
    {
        public string TransId { set; get; }
        public string? RecordId { set; get; }
        public string ARMSessionId { set; get; }
        public string? AppName { set; get; }
        public string? UserName { set; get; }
        public bool? Trace { set; get; }
        public bool? MetaData { set; get; }
        public List<Object>? RecData { set; get; }
    }
}
