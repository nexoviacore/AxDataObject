using ARMCommon.Model;

namespace AxUtils
{
    public interface IQueueStatusService
    {
        Task<QueueStatusDataResult> AxQueueStatus(QueueStatusData axQueueStatus);
    }
}
