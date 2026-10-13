namespace ARMNotificationHub
{
    public class MobileNotification
    {
        public Guid? guid { get; set; }

        public string ARMSessionId { get; set; }
        public string? firebaseId { get; set; }

        public string? ImeiNo { get; set; }
        public string? status { get; set; }
    }
}
