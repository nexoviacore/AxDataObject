


namespace AxValidate.Helpers
{
    [Serializable]
    public class TFg
    {
        public string Fname { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SQLText { get; set; } = string.Empty;
        public string Colwidths { get; set; } = string.Empty;
        public string Groupfield { get; set; } = string.Empty;
        public string SelectOn { get; set; } = string.Empty;
        public string FooterStr { get; set; } = string.Empty;
        public string VExp { get; set; } = string.Empty;
        public string ParamNames { get; set; } = string.Empty;
        public List<string> Map { get; set; } = new List<string>();
        public List<string> Caps { get; set; } = new List<string>();
        public bool FromIView { get; set; } = false;
        public bool MultiSelect { get; set; } = false;
        public bool AutoShow { get; set; } = false;
        public bool ValidateRow { get; set; } = false;
        public bool HasParams { get; set; } = false;
        public bool DynamicParams { get; set; } = false;
        public bool ExecuteOnSave { get; set; } = false;
        public bool Firmbind { get; set; } = false;
        public bool SelectAllRows { get; set; } = false;
        public double TargetFrame { get; set; } = 0.0;
        public double SourceFrame { get; set; } = 0.0;
        public int AddRows { get; set; } = 0;
        public string TargetDcName { get; set; } = string.Empty;
        public string SourceDcName { get; set; } = string.Empty;
        public string CAddRows { get; set; } = string.Empty;
        
    }
}
