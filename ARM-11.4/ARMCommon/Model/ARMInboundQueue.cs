namespace ARMCommon.Model
{
    public class ARMInboundQueue
    {
        public string Project { get; set; }
        public string SecretKey { get; set; }
        public string? QueueData { get; set; }        
        public string QueueName { get; set; }
        public string? UserName { get; set; }
        public string? UserAuthKey { get; set; }
        public string? SignalRClient { get; set; }
        public int? Delay { get; set; }
        public bool? Trace { get; set; }
        public string? ResponseQueue { get; set; }
        public string? Seed { get; set; }
        public string? Token { get; set; }
        public string? IsActive { get; set; }
        public string? URL { get; set; }
        public string? Method { get; set; }
        public string? APIDesc { get; set; }
        public Dictionary<string, object>? SubmitData { get; set; }

    }
}
