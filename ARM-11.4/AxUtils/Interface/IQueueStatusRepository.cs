using ARMCommon.Model;

namespace AxUtils.Interface
{
    public interface IQueueStatusRepository
    {
        Task<QueueStatusDataResult> AxQueueStatus(QueueStatusData queueStatusData);

        QueueStatus CheckConsumerStatus(string rmQueueURL, string queueName);
    }
}
