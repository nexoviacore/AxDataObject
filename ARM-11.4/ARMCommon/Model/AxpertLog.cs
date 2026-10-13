using System;
namespace ARMCommon.Model
{
    public class AxpertLog
    {
        public string? SessionId { get; set; }

        public string? Username { get; set; }

        public DateTime? CalledOn { get; set; }

        public string? StructName { get; set; }

        public Int64? RecordId { get; set; }

        public int? TimeTaken { get; set; }

        public int? DbConnTime { get; set; }

        public int? DbTimeTaken { get; set; }

        public string? ServiceName { get; set; }

        public string? ServiceResult { get; set; }

        public DateTime? CallFinished { get; set; }
        public string? CallDetails { get; set; }

    }

    public class AxAudit
    {
        public string? SessionId { get; set; }

        public string? Username { get; set; }

        public DateTime? LoginTime { get; set; }

        public string? Status { get; set; }

        public string? Reason { get; set; }

        public string? IP { get; set; }
        public string? Other { get; set; }

        public DateTime? LogoutTime { get; set; }
        public string? NoLogout { get; set; }

        public DateTime? ServerTime { get; set; }

    }
}
