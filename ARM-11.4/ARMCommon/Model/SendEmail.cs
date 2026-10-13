namespace ARMCommon.Model
{
    public class SendEmail
    {
        public string SenderEmail { get; set; }
        public string Username { get; set; }
        public string JobId { get; set; }
        public int interval { get; set;}
        public bool DeletePendingJobs { get; set; }
    }
}
