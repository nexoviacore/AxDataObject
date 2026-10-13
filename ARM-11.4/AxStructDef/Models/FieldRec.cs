using System;
using AxStructDef.Models;

namespace AxStructDef.Models
{
    public class FieldRec
    {
        public string TableName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int RowNo { get; set; } = 0;
        public string Value { get; set; } = string.Empty;
        public Int64 IdValue { get; set; } = 0;
        public string OldValue { get; set; } = string.Empty;
        public Int64 OldIdValue { get; set; } = 0;
        public int FrameNo { get; set; } = 0;
        public Int64 RecordId { get; set; } = 0;
        public string PrimaryTable { get; set; } = string.Empty;
        public int Orders { get; set; } = 0;
        public int OldRow { get; set; } = 0;
        public bool SourceKey { get; set; } = false;
        public int ParentRowNo { get; set; } = 0;
        public int OldParentRow { get; set; } = 0;
        public bool ZeroValue { get; set; } = false;
        public bool AutoValue { get; set; } = false;
        public bool ClientNotify { get; set; } = false;
        public TField Fld { get; set; }
        public string WebValue { get; set; } = string.Empty; // to use data comparison for making changed JSON strings
        public string EncryptValue { get; set; } = string.Empty;
    }
}
