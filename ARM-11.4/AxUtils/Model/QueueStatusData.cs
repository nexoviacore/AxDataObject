namespace AxUtils
{
    public class QueueStatusData
    {
        public string QueueName { get; set; }
    }
    public class QueueStatusDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string result { get; set; }
    }

    public enum QueueStatus
    {
        ServerDown,
        InvalidSettings,
        QueueNotFound,
        NoConsumer,
        ConsumerActive
    }
}
