using ARMCommon.ActionFilter;
using Microsoft.AspNetCore.Mvc;

namespace AxStructUtils
{
    [Route("api/v1")]
    [ApiController]
    public class AppStatusController : ControllerBase
    {
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpGet("AppStatus")]
        public ActionResult ARMAppStatus()
        {
            return Ok("APPISRUNNING");
        }
    }
}