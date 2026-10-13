using ARMCommon.ActionFilter;
using ARM_APIs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARMCommon.Model;
using ARM_APIs.Interface;
using System.Reflection.Metadata;
using ARMCommon.Helpers;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMUserGroupsController : Controller
    {
        private readonly IARMUserGroups _userGroupsService;

        public ARMUserGroupsController(IARMUserGroups userGroupsService)
        {
            _userGroupsService = userGroupsService;
        }

        [AllowAnonymous]
        [RequiredFieldsFilter("appname")]
        [HttpPost("ARMUserGroups")]

        public async Task<IActionResult> ARMUserGroups(ARMGetUserGroup aRMGetUserGroup)
        {
            var result = await _userGroupsService.GetARMUserGroups(aRMGetUserGroup.appname);

            if (result.ToString() == Constants.RESULTS.ERROR.ToString())
            {
                return BadRequest("USERGROUPDOESNTEXIST");
            }
            else
            {
                return Ok(result);
            }
        }
    };
}



