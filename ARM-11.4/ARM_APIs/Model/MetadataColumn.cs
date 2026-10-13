namespace ARM_APIs.Model
{
    public class MetadataColumn
    {
        public string columnname { get; set; } = string.Empty;
        public string datatype { get; set; } = string.Empty;
        public string? length { get; set; }
        public bool nullable { get; set; }
        public string axdatatype { get; set; } = string.Empty;
    }
}
