using ARMCommon.ActionFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxUtils.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class QueueStatusController : ControllerBase
    {
        private readonly IQueueStatusService _inQueueStatusService;
        public QueueStatusController(IQueueStatusService inQueueStautsService)
        {
            _inQueueStatusService = inQueueStautsService;
        }
        [ApiVersion("1")]
        [AllowAnonymous]
        [HttpPost("AxGetQueueStatus")]
        public async Task<IActionResult> AxGetQueueStatus(QueueStatusData axQueueStatus)
        {
            var result = await _inQueueStatusService.AxQueueStatus(axQueueStatus);
            if (result is not null)
            {
                return new JsonResult(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }
}
