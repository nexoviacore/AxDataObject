using ARMCommon.ActionFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxUtils
{
    [Route("api/v{version:apiVersion}")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxInMemoryController : ControllerBase
    {
        private readonly IAxInMemoryService _inMemoryService;
        public AxInMemoryController(IAxInMemoryService inMemoryService)
        {
            _inMemoryService = inMemoryService;
        }
        [ApiVersion("1")]
        [AllowAnonymous]
        [HttpPost("AxRedisRead")]
        public async Task<IActionResult> AxRedisRead(AxInMemReadData inMemInput)
        {
            var result = await _inMemoryService.AxReadInMemory(inMemInput);
            if (result is not null)
            {
                return new JsonResult(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [ApiVersion("1")]
        [AllowAnonymous]
        [HttpPost("AxRedisWrite")]
        public async Task<IActionResult> AxRedisWrite(AxInMemWriteData inMemInput)
        {
            var result = await _inMemoryService.AxWriteInMemory(inMemInput);
            if (result is not null)
            {
                return new JsonResult(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [ApiVersion("1")]
        [AllowAnonymous]
        [HttpPost("AxRedisDelete")]
        public async Task<IActionResult> AxRedisDelete(AxInMemDeleteData inMemInput)
        {
            var result = await _inMemoryService.AxDeleteInMemory(inMemInput);
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
