using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxUtils.Interface;
using Newtonsoft.Json;

namespace AxUtils
{
    public class QueueStatusService : IQueueStatusService
    {
        private readonly IQueueStatusRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public QueueStatusService(
            IQueueStatusRepository repo,
            Utils common,
            IARMLogTraceService traceService)
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;
        }

        public async Task<QueueStatusDataResult> AxQueueStatus(
            QueueStatusData axQueueStatus)
        {
            QueueStatusDataResult result;

            try
            {
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogMessage(
                        $"Input JSON - {JsonConvert.SerializeObject(axQueueStatus)}");
                }

                result = await _repo.AxQueueStatus(axQueueStatus);
            }
            catch (Exception ex)
            {
                result = new QueueStatusDataResult
                {
                    Success = false,
                    Message = "Error occurred in AxQueueStatus.",
                    result = "Exception"
                };

                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(
                        ex.Message + ex.InnerException);
                }
            }

            if (_traceService.IsTracingEnabled())
            {
                _traceService.LogMessage(
                    $"Result JSON - {JsonConvert.SerializeObject(result)}");
            }

            return result;
        }
    }
}