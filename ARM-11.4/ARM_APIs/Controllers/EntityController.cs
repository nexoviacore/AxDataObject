using ARM_APIs.Model;
using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;
using Microsoft.IdentityModel.Tokens;
using ARMCommon.Filter;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class EntityController : ControllerBase
    {

        private readonly IEntityService _entity;
        private readonly IARMLogTraceService _traceService;
        public EntityController(IEntityService entity, IARMLogTraceService traceService)
        {
            _entity = entity;
            _traceService = traceService;
        }

        //[RequiredFieldsFilter("ARMSessionId", "TransId")]
        //[HttpPost("GetEntityListData")]
        //public async Task<IActionResult> GetEntityListData(Entity entity)
        //{
        //    SQLResult entityList;
        //    if (string.IsNullOrEmpty(entity.Filter))
        //        entityList = await _entity.GetEntityListData(entity);
        //    else
        //        entityList = await _entity.GetFilteredEntityListData(entity);

        //    var entityMetaData = new SQLResult();
        //    if (entity.MetaData == true)
        //    {
        //        entityMetaData = await _entity.GetEntityMetaData(entity);
        //    }

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityMetaData.error))
        //    {
        //        result.result.Add("message", entityMetaData.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else if (!string.IsNullOrEmpty(entityList.error))
        //    {
        //        result.result.Add("message", entityList.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        if (entityList.data?.Rows?.Count == 0 || (entityList.data?.Rows?.Count > 0 && (string.IsNullOrEmpty(entityList.data?.Rows[0][0]?.ToString()) || entityList.data?.Rows[0][0]?.ToString() == "[]")))
        //            result.result.Add("count", 0);

        //        result.result.Add("list", entityList.data);

        //        if (entity.MetaData == true)
        //        {
        //            result.result.Add("metadata", entityMetaData.data);
        //        }
        //        return Ok(result);
        //    }
        //}

        //

        //[RequiredFieldsFilter("ARMSessionId", "TransId")]
        //[HttpPost("GetEntityMetaData")]
        //public async Task<IActionResult> GetEntityMetaData(Entity entity)
        //{

        //    var entityMetaData = await _entity.GetEntityMetaData(entity);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityMetaData.error))
        //    {
        //        result.result.Add("message", entityMetaData.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("metadata", entityMetaData.data);
        //        return Ok(result);
        //    }
        //}

        //[ApiVersion("2")]
        //[RequiredFieldsFilter("ARMSessionId", "TransId")]
        //[HttpPost("GetEntityMetaData")]
        //public async Task<IActionResult> GetEntityMetaDataV2(Entity entity)
        //{

        //    var entityMetaData = await _entity.GetEntityMetaDataV2(entity);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityMetaData.error))
        //    {
        //        result.result.Add("message", entityMetaData.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("metadata", entityMetaData.data);
        //        return Ok(result);
        //    }
        //}

        [RequiredFieldsFilter("ARMSessionId", "TransId")]
        [HttpPost("GetEntityChartsData")]
        public async Task<IActionResult> GetEntityChartsData(EntityCharts entity)
        {
            var entityCharts = await _entity.GetEntityChartsData(entity);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(entityCharts.error))
            {
                result.result.Add("message", entityCharts.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");

                result.result.Add("charts", entityCharts.data);
                return Ok(result);
            }
        }


        //[RequiredFieldsFilter("ARMSessionId", "TransId", "ChartMetaData")]
        //[HttpPost("GetAnalyticsChartsData")]
        //public async Task<IActionResult> GetAnalyticsChartsData(AnalyticsCharts charts)
        //{
        //    SQLResult entityCharts;
        //    if (charts.Page == "Entity")
        //        entityCharts = await _entity.GetAnalyticsMultipleChartsData(charts);
        //    else
        //        entityCharts = await _entity.GetAnalyticsChartsData(charts);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityCharts.error))
        //    {
        //        result.result.Add("message", entityCharts.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");

        //        result.result.Add("charts", entityCharts.data);
        //        return Ok(result);
        //    }
        //}


        //[RequiredFieldsFilter("ARMSessionId","EntityName")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //[HttpPost("GetEntityChartsMetaData")]
        //public async Task<IActionResult> GetEntityChartsMetaData(EntityCharts entity)
        //{
        //    var entityCharts = await _entity.GetEntityChartsMetaData(entity);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityCharts.error))
        //    {
        //        result.result.Add("message", entityCharts.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("metadata", entityCharts.data);
        //        return Ok(result);
        //    }
        //}

        //[RequiredFieldsFilter("ARMSessionId", "TransId")]
        //[HttpPost("GetSubEntityListData")]
        //public async Task<IActionResult> GetSubEntityListData(EntityForm entity)
        //{
        //    var entityList = await _entity.GetSubEntityListData(entity);
        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityList.error))
        //    {
        //        result.result.Add("message", entityList.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("list", entityList.data);
        //        return Ok(result);
        //    }
        //}

        [RequiredFieldsFilter("ARMSessionId", "TransId")]
        [HttpPost("GetSubEntityMetaData")]
        public async Task<IActionResult> GetSubEntityMetaData(EntityForm entity)
        {

            var entityMetaData = await _entity.GetSubEntityMetaData(entity);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(entityMetaData.error))
            {
                result.result.Add("message", entityMetaData.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("metadata", entityMetaData.data);
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "TransId")]
        [HttpPost("GetEntityFormMetaData")]
        public async Task<IActionResult> GetEntityFormMetaData(EntityForm entity)
        {
            var entityMData = await _entity.GetEntityFormMetaData(entity);

            //TO DO
            //var subEntityMData = new SQLResult();
            //if (entity.SubEntityMetaData == true) { 

            //}

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(entityMData.error))
            {
                result.result.Add("message", entityMData.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("metadata", entityMData.data);
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "EntityName")]
        [HttpPost("GetSubEntityChartsData")]
        public async Task<IActionResult> GetSubEntityChartsData(EntityCharts entity)
        {
            var entityCharts = await _entity.GetSubEntityChartsData(entity);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(entityCharts.error))
            {
                result.result.Add("message", entityCharts.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("charts", entityCharts.data);
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("GetEntityList")]
        public async Task<IActionResult> GetEntityList(Entity entity)
        {
            var entityList = await _entity.GetEntityList(entity);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(entityList.error))
            {
                result.result.Add("message", entityList.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("list", entityList.data);
                return Ok(result);
            }
        }

        //[RequiredFieldsFilter("ARMSessionId","EntityName")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //[HttpPost("GetSubEntityChartsMetaData")]
        //public async Task<IActionResult> GetSubEntityChartsMetaData(EntityCharts entity)
        //{
        //    var entityCharts = await _entity.GetSubEntityChartsMetaData(entity);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(entityCharts.error))
        //    {
        //        result.result.Add("message", entityCharts.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("metadata", entityCharts.data);
        //        return Ok(result);
        //    }
        //}

        [RequiredFieldsFilter("ARMSessionId", "AppName", "UserName", "Page", "Properties")]
        [HttpPost("SetAnalyticsData")]
        public async Task<IActionResult> SetAnalyticsData(AnalyticsData analyticsData)
        {

            var apiResult = await _entity.SetAnalyticsData(analyticsData);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(apiResult.error))
            {
                result.result.Add("message", apiResult.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                return Ok(result);
            }
        }


        [RequiredFieldsFilter("ARMSessionId", "AppName", "UserName", "Page", "PropertiesList")]
        [HttpPost("GetAnalyticsData")]
        public async Task<IActionResult> GetAnalyticsData(AnalyticsData analyticsData)
        {

            var apiResult = await _entity.GetAnalyticsData(analyticsData);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(apiResult.error))
            {
                result.result.Add("message", apiResult.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", apiResult.data);
                return Ok(result);
            }
        }

        //[RequiredFieldsFilter("ARMSessionId", "AppName", "UserName", "Page", "PropertiesList")]
        //[HttpPost("GetAnalyticsEntityData")]
        //public async Task<IActionResult> GetAnalyticsEntityData(AnalyticsEntityInput analyticsInput)
        //{
        //    AnalyticsEntityOutput analyticResult;
        //    if (string.IsNullOrEmpty(analyticsInput.TransId))
        //        analyticResult = await _entity.GetAnalyticsPageLoadData(analyticsInput);
        //    else
        //        analyticResult = await _entity.GetAnalyticsEntityData(analyticsInput);

        //    ARMResult result = new ARMResult();
        //    result.result.Add("message", "SUCCESS");
        //    result.result.Add("data", analyticResult);
        //    return Ok(result);
        //}


        //[RequiredFieldsFilter("ARMSessionId", "TransId")]
        //[HttpPost("GetEntityListPageLoadData")]
        //public async Task<IActionResult> GetEntityListPageLoadData(Entity entity)
        //{
        //    var entityList = await _entity.GetEntityListPageLoadData(entity);
        //    ARMResult result = new ARMResult();

        //    if (string.IsNullOrEmpty(entityList.Error))
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("data", entityList);

        //        return Ok(result);
        //    }
        //    else {
        //        result.result.Add("messagetype", "Custom");
        //        result.result.Add("message", entityList.Error);
        //        result.result.Add("Trace", entityList.Trace ?? "");
        //        return BadRequest(result);
        //    }
        //}

        [RequiredFieldsFilter("ARMSessionId", "TransId", "RecordId", "UserName")]
        [HttpPost("GetEntityEditableFlag")]
        public async Task<IActionResult> GetEntityEditableFlag(Entity entity)
        {
            var editable = await _entity.GetEntityEditableFlag(entity);
            ARMResult result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("data", editable);
            return Ok(result);
        }

        //[RequiredFieldsFilter("ARMSessionId", "TransId", "FieldId")]
        //[HttpPost("GetEntityDropdownValues")]
        //public async Task<IActionResult> GetEntityDropdownValues(EntityFilter entityFilter)
        //{
        //    var data = await _entity.GetEntityDropdownValues(entityFilter);
        //    ARMResult result = new ARMResult();
        //    result.result.Add("message", "SUCCESS");
        //    result.result.Add("data", data);
        //    return Ok(result);
        //}

        [RequiredFieldsFilter("ARMSessionId", "Criteria", "TransId", "RecordId")]
        [HttpPost("GetEntityFormConnectedDataMetrics")]
        public async Task<IActionResult> GetEntityFormConnectedDataMetrics(EntityForm entityForm)
        {
            var data = await _entity.GetEntityFormConnectedDataMetrics(entityForm);

            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(data.error))
            {
                result.result.Add("message", data.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data.data);
                return Ok(result);
            }
        }

        //[RequiredFieldsFilter("ARMSessionId", "TransId", "RecordId")]
        //[HttpPost("GetEntityFormPageLoadData")]
        //public async Task<IActionResult> GetEntityFormPageLoadData(EntityForm entityForm)
        //{
        //    var data = await _entity.GetEntityFormPageLoadData(entityForm);

        //    ARMResult result = new ARMResult();
        //    if (!string.IsNullOrEmpty(data.Error))
        //    {
        //        result.result.Add("message", data.Error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("data", data);
        //        return Ok(result);
        //    }
        //}
    }
}
