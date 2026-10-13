using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARM_APIs.Service;
using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class GetSqlMetaDataController : ControllerBase
    {
        private readonly ISqlMetadataService _sqlmetadataService;

        public GetSqlMetaDataController(ISqlMetadataService metadataService)
        {
            _sqlmetadataService = metadataService;

        }

        [ServiceFilter(typeof(ApiResponseFilter))]
        [RequiredFieldsFilter("AppName", "SqlQuery")]
        [HttpPost("GetSqlMetaData")]
        public async Task<IActionResult> GetSqlMetaData([FromBody] DbRequest request)
        {
            var result = new ARMResult();

            if (!request.SqlQuery.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                result.result.Add("success", false);
                result.result.Add("message", "Only SELECT queries are allowed for metadata retrieval.");
                return BadRequest(result);
            }

            try
            {
                var metadata = await _sqlmetadataService.GetMetadataAsync(request);
                result.result.Add("success", true);
                result.result.Add("message", "Success");
                result.result.Add("data", metadata);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                result.result.Add("success", false);
                result.result.Add("message", ex.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                result.result.Add("success", false);
                result.result.Add("message", ex.Message);
                return StatusCode(500, result);
            }
        }
    }
}
