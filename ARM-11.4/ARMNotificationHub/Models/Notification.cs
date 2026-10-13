namespace ARMNotificationHub.Models
{
    public class Notification
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }



}
