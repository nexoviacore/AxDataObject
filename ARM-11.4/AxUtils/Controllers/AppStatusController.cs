using ARMCommon.ActionFilter;
using Microsoft.AspNetCore.Mvc;

namespace AxUtils
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