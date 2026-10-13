namespace ARMCommon.Model
{
    public class ARMNotify
    {
        public string NotificationType { get; set; }
        public string? TemplateId { get; set; }

        public string? TemplateString { get; set; }
        public Dictionary<string, string>? NotificationData { get; set; }

        public EmailDetails? EmailDetails { get; set; }
    }

    public class EmailDetails
    {
        public IEnumerable<string> To { get; set; }
        public IEnumerable<string>? Bcc { get; set; }
        public IEnumerable<string>? cc { get; set; }
        public string? Subject { get; set; }

    }

}
