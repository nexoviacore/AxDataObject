using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxUtils.Interface;
using Newtonsoft.Json;

namespace AxUtils.Service
{
    public class AxInMemoryService : IAxInMemoryService
    {
        private readonly IAxInMemoryRepository _repo;
        private readonly ARMCommon.Helpers.Utils _common;
        private readonly IARMLogTraceService _traceService;
        private bool isTracing;
        public AxInMemoryService(IAxInMemoryRepository repo, Utils common, IARMLogTraceService traceService)
        {
            _repo = repo;
            _common = common;
            _traceService = traceService;
        }

        public async Task<AxInMemDataResult> AxReadInMemory(AxInMemReadData menuData)
        {
            var result = new AxInMemDataResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(menuData)}");
            try
            {
                result = await _repo.AxReadInMemory(menuData);
            }
            catch (Exception ex)
            {
                result = new AxInMemDataResult
                {
                    Success = false,
                    Message = "Error occurred in AxReadInMemory."
                };
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
            }
            finally
            {
            }
            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                //result.result["trace"] = await _traceService.GetTraceFile($"AxInMemoryRead");
            }
            return result;
        }

        public async Task<AxInMemDataResult> AxWriteInMemory(AxInMemWriteData menuData)
        {
            var result = new AxInMemDataResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(menuData)}");
            try
            {
                result = await _repo.AxWriteInMemory(menuData);
            }
            catch (Exception ex)
            {
                result = new AxInMemDataResult
                {
                    Success = false,
                    Message = "Error occurred in AxWriteInMemory."
                };
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
            }
            finally
            {
            }
            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                //result.result["trace"] = await _traceService.GetTraceFile($"AxInMemoryWrite");
            }
            return result;
        }

        public async Task<AxInMemDeleteDataResult> AxDeleteInMemory(AxInMemDeleteData menuData)
        {
            var result = new AxInMemDeleteDataResult();
            if (isTracing) _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(menuData)}");
            try
            {
                result = await _repo.AxDeleteInMemory(menuData);
            }
            catch (Exception ex)
            {
                result = new AxInMemDeleteDataResult
                {
                    Success = false,
                    Message = "Error occurred in AxDeleteInMemory."
                };
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(ex.Message + ex.InnerException);
                }
            }
            finally
            {
            }
            if (isTracing)
            {
                _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
                //result.result["trace"] = await _traceService.GetTraceFile($"AxInMemoryRead");
            }
            return result;
        }
    }
}
