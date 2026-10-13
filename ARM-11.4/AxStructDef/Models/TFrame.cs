using System.Collections.Generic;

namespace AxStructDef.Models
{
    public class TFrame
    {
        public int FrameNo { get; set; } = 0;
        public int Parent { get; set; } = 0;
        public int PageNo { get; set; } = 0;
        public int PopParent { get; set; } = 0;
        public int PopIndex { get; set; } = 0;
        public int StartIndex { get; set; } = 0;
        public int FieldCount { get; set; } = 0;
        public int RowCount { get; set; } = 0;
        public int LastFieldNo { get; set; } = 0;
        public string TableName { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public bool AsGrid { get; set; } = false;
        public bool Popup { get; set; } = false;
        public bool AllowChange { get; set; } = false;
        public bool ReadOnly { get; set; } = false;
        public bool AllowEmpty { get; set; } = false;
        public bool HasDataRows { get; set; } = false;
        public bool AllowAddRow { get; set; } = false;
        public bool AllowDeleteRow { get; set; } = false;
        public List<string> PopupOn { get; set; } = new List<string>();
        public List<string> OnValidate { get; set; } = new List<string>();
        public Dictionary<int, Int64> RecordIdDict { get; set; } = new Dictionary<int, Int64>();
        public Dictionary<int, Int64> OldRecordIdDict { get; set; } = new Dictionary<int, Int64>();
        public Dictionary<string, string> RowActionDict { get; set; } = new Dictionary<string, string>();
        public dynamic Comp { get; set; } // TWinControl, assumed UI control
        public string ActRowDeps { get; set; } = string.Empty;
        public string JsonStr { get; set; } = string.Empty;
        public string MustLoadSql { get; set; } = string.Empty;
        public string DcSQL { get; set; } = string.Empty;
        public List<string> DcSQLGroup { get; set; } = new List<string>();
    }
}