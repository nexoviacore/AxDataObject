namespace ARMCommon.Model
{
    public class ProcessListForAxpert
    {
        public string Title { get; set; }
        public List<NodeData> AddNewNode { get; set; }
        public List<ListData> List { get; set; }
    }

    public class NodeData
    {
        public string transid { get; set; }
        public string Caption { get; set; }
    }

    public class ListData
    {
        public string Fromuser { get; set; }
        public string eventtime { get; set; }
        public string ProcessName { get; set; }
        public string TaskIcon { get; set; }
        public string TaskCaption { get; set; }
        public string TaskType { get; set; }
        public string TaskStatus { get; set; }
        public string Keyfield { get; set; }
        public string Keyvalue { get; set; }
        public string transid { get; set; }
        public string Caption { get; set; }
        public int? recordid { get; set; }

        public string? nexttask { get; set; }
    }
}
