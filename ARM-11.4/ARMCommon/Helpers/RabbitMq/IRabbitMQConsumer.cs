

using RabbitMQ.Client;

namespace ARMCommon.Helpers.RabbitMq
{
    public interface IRabbitMQConsumer
    {
        // delegate void MyDelegate(EventArgsData data);
        void DoConsume(string queueName, RabbitMQConsumer.MyDelegate function);
        void DeleteQueue(string queueName);
        void DoConsumeWorkerService(string queueName, RabbitMQConsumer.MyDelegate function);
        void DoConsumeWorkerServiceNew(string queueName, Func<string, Task<string>> function, CancellationToken stoppingToken);
        void DoConsumeWorkerServiceStable(string queueName, RabbitMQConsumer.MyDelegate function, CancellationToken stoppingToken);
        void DoConsumeWorkerServiceJob(string queueName, RabbitMQConsumer.MyDelegate function);
    }
}
