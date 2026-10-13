using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxStructUtils
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    public class AxAddFormFieldController : ControllerBase
    {
        private readonly IAxAddFormFieldService _addFormField;
        public AxAddFormFieldController(IAxAddFormFieldService addFormField)
        {
            _addFormField = addFormField;
        }

        [RequiredFieldsFilter("ARMSessionId", "StructName", "StructType")]
        [HttpPost("AxAddForm")]
        public async Task<IActionResult> AxAddForm(AxAddFormField formData)
        {
            var result = await _addFormField.AxAddForm(formData);
            if (result is not null)
            {
                return new JsonResult(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "StructName", "StructType")]
        [HttpPost("AxAddField")]
        public async Task<IActionResult> AxAddField(AxAddFormField fieldData)
        {
            var result = await _addFormField.AxAddField(fieldData);
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
