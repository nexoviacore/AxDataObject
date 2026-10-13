using System.Globalization;

namespace AxTstructData
{
    public class TrackHistory
    {
        //public Int64? RecordId { get; set; } = 0;
        public string ModifiedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        //public string? UserName { get; set; }
        public string? Action { get; set; }
        public List<TrackField> ChangedFields { get; set; } = new List<TrackField>();
    }
    public class TrackField
    {        
        public string? FieldName { get; set; }
        public string? OldValue { get; set; } = "";      
        public string? NewValue { get; set; } = "";
        public string? DcName { get; set; } = "";
        public int? RowNo { get; set; }             
        public int? FrameNo { get; set; }               
        public Int64? TableRecId { get; set; } = 0;
        public Int64? IdValue { get; set; } = 0;
        public Int64? OldIdValue { get; set; } = 0;
        public string? RowAction { get; set; }

        public TrackField(string? fieldName, string? oldValue, string? newValue, int? rowNo, int? frameNo, Int64? tableRecId, Int64? idValue, Int64? oldIdValue, string? rowAction)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
            RowNo = rowNo;
            FrameNo = frameNo;
            TableRecId = tableRecId;
            IdValue = idValue;
            OldIdValue = oldIdValue;
            RowAction = rowAction;
            DcName = "dc" + frameNo;
        }
    }
}
