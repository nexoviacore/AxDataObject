using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ARM_APIs.Model
{
    public class AxScriptRequest
    {
        public string ARMSessionId { get; set; }
        public string? UserName { get; set; }
        public string? Project { get; set; }
        public string? Script { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? RecordId { get; set; } = "0";
        public bool Trace { get; set; } = false;        
        public Dictionary<string, string> APIParams { get; set; } = new Dictionary<string, string>();

    }
}
