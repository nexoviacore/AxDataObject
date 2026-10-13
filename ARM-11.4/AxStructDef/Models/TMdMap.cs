using System.Data;

namespace AxStructDef.Models
{
    public class TMdMap
    {
        public string TransId { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ControlField { get; set; } = string.Empty;
        public string MasterTransId { get; set; } = string.Empty;
        public bool InitOnDel { get; set; } = false;
        public bool Append { get; set; } = false;
        public bool PostOnApprove { get; set; } = false;
        public bool PostOnReject { get; set; } = false;
        public int SourceFrame { get; set; } = -1;
        public string MasterSearchField { get; set; } = string.Empty;
        public string DetailSearchField { get; set; } = string.Empty;
        public List<MdMapField> FieldMappings { get; set; } = new List<MdMapField>();
    }

    public class MdMapField
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public OperationType Operation { get; set; } = OperationType.Normal;
        public int DecimalPlaces { get; set; } = 0;
        public string SourceValue { get; set; } = string.Empty;
    }

    public enum OperationType
    {
        Normal,     
        Add,     
        Less,     
        Highest,
        Lowest, 
        Inc, 
        Dec,
        Replace
    }
}
