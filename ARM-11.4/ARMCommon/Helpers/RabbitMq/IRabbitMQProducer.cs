namespace ARMCommon.Helpers
{
    public interface IRabbitMQProducer
    {
        bool SendMessage<T>(T message, string queueName);

        bool SendMessages<T>(T message, string queueName, bool trace = false , int delayTimeInMs =0, int msgexpirysecs = 0);
    }
}
