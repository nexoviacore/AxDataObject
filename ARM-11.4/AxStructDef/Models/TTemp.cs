
namespace AxStructDef.Models
{
    public class TTemp
    {
        public string MenuPosition { get; set; } = "default";
        public List<string> MenuList { get; set; } = [];
        public bool Listview { get; set; } = false;
        public string Layout { get; set; } = "default";
        public string FormCaption { get; set; } = string.Empty;
        public bool DefinitionPage { get; set; } = false;
        public bool IncludePage { get; set; } = false;
        public List<string> ChangedDc { get; set; } = [];
    }
}
