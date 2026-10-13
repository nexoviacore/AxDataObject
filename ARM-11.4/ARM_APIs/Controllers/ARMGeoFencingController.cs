using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using StackExchange.Redis;

namespace ARM_APIs.Controllers
{

    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMGeoFencingController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IARMGeoFencing _geofencing;

        public ARMGeoFencingController(IConfiguration configuration, IARMGeoFencing geofencing)
        {
            _config = configuration;
            _geofencing = geofencing;

        }

        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMGetGeoFencingData")]
        public async Task<IActionResult> ARMGetGeoFencingData(ARMSession model)
        {
            var data = await _geofencing.GetGeoFencingData(model.ARMSessionId);
            if (data.Rows.Count > 0)
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data);
                return Ok(result);

            }
            else
            {
                return BadRequest("NORECORD");
            }


        }

        [AllowAnonymous]
        [RequiredFieldsFilter("ARMSessionid", "identifier", "current_name", "current_lat", "current_long", "src_name", "src_lat", "src_long", "is_withinradius")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMPushGeoFencingData")]
        public async Task<IActionResult> ARMPushGeoFencingData(ARMGeoFencingModel geofencing)
        {
            string tableAdded = await _geofencing.SaveGeoFencingData(geofencing);
            if (tableAdded.ToLower() == Constants.RESULTS.INSERTED.ToString().ToLower())
            {
                return Ok("SUCCESS");
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "DATAINSERTIONFAILED");
                result.result.Add("data", tableAdded);
                return BadRequest(result);
            }

        }

        [AllowAnonymous]
        [RequiredFieldsFilter("project", "identifier", "username", "current_name",  "current_loc", "expectedlocations", "interval")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMUpdateLocation")]
        public async Task<IActionResult> ARMUpdateLocation(ARMUpdateLocationModel location)
        {
            string tableAdded = await _geofencing.UpdateGeoLocation(location);
            if (tableAdded.ToLower() == Constants.RESULTS.INSERTED.ToString().ToLower())
            {
                return Ok("SUCCESS");
            }
            else
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "DATAINSERTIONFAILED");
                result.result.Add("data", tableAdded);
                return BadRequest(result);
            }

        }

    }
}
