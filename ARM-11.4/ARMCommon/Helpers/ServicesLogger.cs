using ARMCommon.Services;
using Newtonsoft.Json;

namespace ARMCommon.Helpers
{
    public class ServicesLogger
    {
        private readonly IRabbitMQProducer _iMessageProducer;
        private readonly ARMLogger _armLogger;

        public ServicesLogger(IRabbitMQProducer iMessageProducer, ARMLogger armlogger)
        {

            _iMessageProducer = iMessageProducer;
            _armLogger = armlogger;
        }
        public void DoLog(string logDetails, string service, string module)
        {
            Guid instanceId = _armLogger.InstanceId;
            var serviceName = _armLogger.ServicesList;
            if (serviceName.Contains(service.ToLower()) || serviceName.Contains("all"))
            {
                var messageObjToLog = new
                {
                    StackSTrace = service,
                    Module = module,
                    logtype = "service",
                    InstanceId = instanceId,
                    logDetails = logDetails,
                };

                _iMessageProducer.SendMessages(JsonConvert.SerializeObject(messageObjToLog), "logQueue");

            }
        }

    }
}


