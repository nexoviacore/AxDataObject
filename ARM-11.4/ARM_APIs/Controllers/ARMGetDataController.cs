
using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARM_APIs.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMGetDataController : ControllerBase
    {

        private readonly IRedisHelper _redis;
        private readonly IPostgresHelper _postGres;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly IGetData _getdata;

        public ARMGetDataController(IRedisHelper redis, IPostgresHelper postGres, IConfiguration config, Utils common, IGetData getData)
        {
            _redis = redis;
            _postGres = postGres;
            _config = config;
            _common = common;
            _getdata = getData;

        }
        [RequiredFieldsFilter("datasource")]
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMGetDataRequest")]
        public async Task<IActionResult> ARMGetDataRequest(ARMGetDataRequest data)
        {
            var Id = Guid.NewGuid();
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("dataId", Id);
            return Ok(result);
        }

        
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [RequiredFieldsFilter("datasource")]
        [HttpPost("ARMGetDataResponse")]
        public async Task<IActionResult> ARMGetDataResponse(ARMGetDataRequest data)
        {
            string key = string.Empty;
            string redisdata = string.Empty;
            if (!string.IsNullOrEmpty(data.dataId))
            {
                key = data.datasource + data.dataId.ToString();
                redisdata = await _redis.StringGetAsync(key);
            }
            
            ARMResult result = new ARMResult();
            if (string.IsNullOrEmpty(redisdata))
            {
                var loginuser = await _getdata.GetLoginUser(data.ARMSessionId);
                SQLResult getDataFromDB = await _getdata.GetDataSourceData(loginuser["APPNAME"], data.datasource, data.sqlParams);
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", getDataFromDB.data);
                return Ok(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", redisdata);
                return Ok(result);
            }
        }

        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [RequiredFieldsFilter("datasource")]
        [HttpPost("GetDataFromDatasource_Old")]
        public async Task<IActionResult> GetDataFromDatasource_Old(ARMGetDataRequest data)
        {
            SQLResult dataResult = await _getdata.GetDataFromDatasource(data);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(dataResult.error))
            {
                result.result.Add("message", dataResult.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", dataResult.data);
                return Ok(result);
            }

        }

        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [RequiredFieldsFilter("datasource")]
        [HttpPost("GetDataFromADS")]
        public async Task<IActionResult> GetDataFromADS(ARMGetDataRequest data)
        {
            SQLDataSetResult dataResult = await _getdata.GetDataFromADS(data);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(dataResult.error))
            {
                result.result.Add("message", dataResult.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", dataResult.DataSet.Tables[0]);
                return Ok(result);
            }

        }

    }

}
