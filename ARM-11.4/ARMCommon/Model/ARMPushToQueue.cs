namespace ARM_APIs.Model
{
    public class ARMQueueData
    {
        public string queuedata { get; set; }
        public Dictionary<string, object>? queuejson { get; set; }
        public string queuename { get; set; }
        public string? signalrclient { get; set; }
        public string? apidesc { get; set; }
        public int? timespandelay { get; set; }
        public bool? trace { get; set; }
        public string? responsequeuename { get; set; }
        public int? MsgExpirySecs { get; set; }
    }

    public class ARMSendToQueue
    {
        public string Project { get; set; }
        public string SecretKey { get; set; }
        public string QueueData { get; set; }
        public string QueueName { get; set; }
        public string? UserName { get; set; }
        public string? SignalRClient { get; set; }
        public int? Delay { get; set; }
        public bool? Trace { get; set; }
        public string? ResponseQueue { get; set; }
        public string? Seed { get; set; }
        public string? Token { get; set; }

    }
}
