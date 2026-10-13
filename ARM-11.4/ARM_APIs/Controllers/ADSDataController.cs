using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARM_APIs.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ADSDataController : ControllerBase
    {
        //private readonly IADSDataService _adsDataService;

        public ADSDataController()
        {
          //  _adsDataService = adsDataService;
        }

        //[ServiceFilter(typeof(ValidateSessionFilter))]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //[RequiredFieldsFilter("datasource")]
        //[HttpPost("GetDataFromDatasource_OldV2")]
        //public async Task<IActionResult> GetDataFromDatasource_OldV2(ADSDataRequest request)
        //{
        //    try
        //    {
        //        var dataResult = await _adsDataService.GetDataFromDatasource(request);
        //        dataResult.result.Add("messagetype", "Custom");                
        //        if (dataResult.result["Success"] != null && Convert.ToBoolean(dataResult.result["Success"]) != true)
        //        {
        //            return BadRequest(dataResult);
        //        }
        //        else
        //        {
        //            dataResult.result.Add("message", "SUCCESS");
        //            return Ok(dataResult);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ARMResult result = new ARMResult();
        //        result.result.Add("messagetype", "Custom");
        //        result.result.Add("error", $"Error: {ex.Message}");
        //        result.result.Add("success", false);
        //        return StatusCode(500, result);
        //    }
        //}

        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [RequiredFieldsFilter("datasource")]
        [HttpPost("GetDataFromDatasource")]
        public async Task<IActionResult> GetDataFromDatasource(ADSDataRequest request)
        {
            ARMResult result = new ARMResult();
            result.result.Add("messagetype", "Custom");
            result.result.Add("error", $"GetDataFromDataSource API is deprecated. Please use AxList API instead.");
            result.result.Add("success", false);
            return BadRequest(result);
        }
    }
}
