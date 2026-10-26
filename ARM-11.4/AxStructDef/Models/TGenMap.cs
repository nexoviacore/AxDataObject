using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxStructDef.Models
{
    public class TGenMap
    {
        public string Name { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string TargetTransId { get; set; } = string.Empty;
        public string TargetCaption { get; set; } = string.Empty;
        public string SourceDC { get; set; } = string.Empty;
        public string GenControlField { get; set; } = string.Empty;
        public bool AllowEmpty { get; set; }
        public string Condition { get; set; } = string.Empty;
        public List<GenMapField> FieldMappings { get; set; } = new List<GenMapField>();
        public bool AllowAddRow { get; set; }
        public bool AllowDeleteRow { get; set; }
        public string OnPostEvent { get; set; } = string.Empty; // a9
        public string GenTransId { get; set; } = string.Empty; // a10
        public string OnApproveEvent { get; set; } = string.Empty; // a17
        public string OnRejectEvent { get; set; } = string.Empty; // a18
        public string JsonStr { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty; // a59
    }

    public class GenMapField
    {
        public string TargetField { get; set; } = string.Empty;
        public string TargetCaption { get; set; } = string.Empty;
        public string SourceField { get; set; } = string.Empty;
        public string SourceCaption { get; set; } = string.Empty;
        public int TargetRow { get; set; }
        public int SourceRow { get; set; }
        public string Control { get; set; } = string.Empty;
        public string ControlCaption { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Type { get; set; } = "field";
        public string Order { get; set; } = string.Empty;
        public bool IsID { get; set; }
        public bool SourceAsGrid { get; set; }
        public string IdValue { get; set;  }
    }
}
