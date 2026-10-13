using ARMNotificationHub.Models;
using ARMNotificationHub.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ARMNotificationHub.Helper;
using System.Text;

namespace ARMNotificationHub.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]   
    public class NotificationController : ControllerBase
    {


        private readonly RedisNotificationHelper _redisNotificationHelper;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationController(
            RedisNotificationHelper redisNotificationHelper,
            IHubContext<NotificationHub> hub)
        {
            _redisNotificationHelper = redisNotificationHelper;
            _hub = hub;
        }

        [HttpGet("ARMAppStatus")]
        public ActionResult ARMAppStatus()
        {
            return Ok("Application is running successfully");
        }

        [HttpPost("SendSignalR")]
        public async Task<IActionResult> SendSignalR([FromBody] Notification request)
        {
            request.NotificationId = Guid.NewGuid().ToString();
            var notification = await _redisNotificationHelper.Add(
                request);

            var group = $"USR:{request.Project}:{request.UserId}";

            await _hub.Clients.Group(group)
                .SendAsync("ReceiveNotification", notification.NotificationId + "♠*♠" + notification.Message);

            return Ok("SUCCESS");
        }

        [HttpPost("GetPendingNotificationsForUser")]
        public async Task<IActionResult> GetPendingNotificationsForUser([FromBody] Notification request)
        {
            var notifications = await _redisNotificationHelper.GetAll(request.Project, request.UserId);

            List<string> notificationsList = new List<string>();
            foreach (var notification in notifications)
            {
                notificationsList.Add(notification.NotificationId + "♠*♠" + notification.Message);
            }

            return Ok(notificationsList);
        }

        [HttpPost("DeleteNotification")]
        public async Task<IActionResult> DeleteNotification([FromBody] Notification request)
        {
            await _redisNotificationHelper.Delete(request.Project, request.UserId, request.NotificationId);
            return Ok("Deleted notification");
        }

        [HttpPost("DeleteAllNotifications")]
        public async Task<IActionResult> DeleteAllNotifications([FromBody] Notification request)
        {
            await _redisNotificationHelper.DeleteAll(request.Project, request.UserId);
            return Ok("Deleted All notifications");
        }
    }
}
