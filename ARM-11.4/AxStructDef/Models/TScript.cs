
namespace AxStructDef.Models
{
    public class TScript
    {
        public string Key { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Apply { get; set; } = string.Empty;
        public string Applyon { get; set; } = string.Empty;
        public string SName { get; set; } = string.Empty;
        public bool Scripts { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public string FailMessage { get; set; } = string.Empty;
        public bool Validate { get; set; } = false;
        public bool FormControl { get; set; } = false;
        public bool BreakOnError { get; set; } = false;
        public int LocalNumber { get; set; } = 0;
        public List<ScriptRule> Rules { get; set; } = new List<ScriptRule>();
    }

    public class ScriptRule
    {
        public string Key { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public int Op { get; set; } = 0;
        public string Task { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SName { get; set; } = string.Empty;
        public string Apply { get; set; } = string.Empty;
        public string ApplyOn { get; set; } = string.Empty;
        public List<ScriptParam> Params { get; set; } = new List<ScriptParam>();
    }

    public class ScriptParam
    {
        public string Key { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public int Op { get; set; } = 0;
        public List<string> ExprSet { get; set; } = new List<string>();
    }
}
