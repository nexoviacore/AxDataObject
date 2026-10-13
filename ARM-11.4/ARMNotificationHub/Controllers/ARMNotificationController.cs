using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ARMNotificationHub
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMNotificationController : Controller
    {
        private readonly IARMNotificationService _notificationService;


        public ARMNotificationController(IARMNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        //[RequiredFieldsFilter("firebaseId", "ImeiNo", "ARMSessionId")]
        [HttpPost("ARMMobileNotification")]
        public async Task<IActionResult> ARMMobileNotification(MobileNotification notification)
        {
            var result = await _notificationService.ProcessARMMobileNotification(notification);

            if (result.ToString() == Constants.RESULTS.ERROR.ToString())
            {
                return BadRequest("INVALIDDETAILS");
            }
            else if (result.ToString() == Constants.RESULTS.RECORDINSERTED.ToString())
            {
                return Ok("RECORDINSERTED");
            }
            else if (result.ToString() == Constants.RESULTS.RECORDUPDATED.ToString())
            {
                return Ok("RECORDUPDATED");
            }
            else
            {
                return BadRequest("Unexpected Error");
            }
        }


        ////[RequiredFieldsFilter("Project", "Message", "UserId")]
        //[HttpPost("SendSignalR")]
        //public async Task<IActionResult> SendSignalR(SendSignalRRequest request)
        //{
        //    string connectionId = _redis.StringGet($"ARM-{request.Project}-{request.UserId}");
        //    if (!string.IsNullOrEmpty(connectionId))
        //    {
        //        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", request.Message);
        //        return Ok("SUCCESS");

        //    }
        //    else
        //    {
        //        return BadRequest("INAVALIDUSERID");
        //    }
        //}



        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMDisableMobileNotificationForUser")]
        public async Task<IActionResult> ARMDisableMobileNotificationForUser(MobileNotification notification)
        {
            var result = await _notificationService.DisableMobileNotificationForUserAsync(notification.ARMSessionId);
            if (result)
            {
                return Ok("NOTIFICATIONDISABLED");
            }
            else
            {
                return BadRequest("INVALIDSESSION");
            }
        }



        //[HttpPost]
        //public async Task<IActionResult> Post(ARMNotify model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }
        //    var result = await _notificationService.SendEmailNotification(model);
        //    return result;

        //}
    }


}


