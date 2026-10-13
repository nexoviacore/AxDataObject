using ARM_APIs.Model;
using ARMCommon.Model;

namespace ARM_APIs.Services
{
    public interface IARMPushToQueue
    {
       abstract bool PushToQueue(ARMQueueData queueData);
        abstract Task<string> SendToQueue(ARMInboundQueue queueData, ARMInboundQueue queueObj);
        abstract Task<SQLResult> GetInboundQueue(string appName, string queueName);
        abstract Task<ARMInboundQueue> GetQueueObject(SQLResult apiDetails);
    }
}
