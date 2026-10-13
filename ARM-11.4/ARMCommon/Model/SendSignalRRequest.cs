using System.ComponentModel.DataAnnotations;

namespace ARMCommon.Model
{
    public class SendSignalRRequest
    {
        public string? Project { get; set; }
        public string UserId { get; set; }
 
        public string Message { get; set; }

        public string? CalledFrom { get; set; }
    }
}
