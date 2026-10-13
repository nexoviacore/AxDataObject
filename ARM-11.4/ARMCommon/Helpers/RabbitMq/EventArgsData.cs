namespace ARMCommon.Helpers.RabbitMq
{
    public class EventArgsData
    {
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public IDictionary<string, object> Headers { get; set; }
        public byte[] Body { get; set; }
        public string Message { get; set; }
    }
}
