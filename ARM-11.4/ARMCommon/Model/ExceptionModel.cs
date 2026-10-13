using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{
    public class ExceptionModel
    {
        public string HttpMethod { get; set; }
        public string? Requestbody { get; set; }
        public string? QueryStringValue { get; set; }
        public string? RequestBody { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionStackSTrace { get; set; }
        public string? InnerExceptionMessage { get; set; }
        public string? InnerExceptionStackTrace { get; set; }
    }

    public class Logs
    {
        public string appname { get; set; }
        public List<string>? API { get; set; }
        public List<string>? Service { get; set; }
        [NotMapped]
        public List<ARMUserGroup>? UserGroups { get; set; }


    }
}
