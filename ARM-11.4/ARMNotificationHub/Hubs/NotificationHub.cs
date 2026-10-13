using ARMNotificationHub.Helper;
using Microsoft.AspNetCore.SignalR;

namespace ARMNotificationHub.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly RedisNotificationHelper _redisNotificationHelper;

        public NotificationHub(RedisNotificationHelper redisNotificationHelper)
        {
            _redisNotificationHelper = redisNotificationHelper;
        }

        public override async Task OnConnectedAsync()
        {
            var ctx = Context.GetHttpContext()!;
            var userId = ctx.Request.Query["userId"];
            var app = ctx.Request.Query["project"];

            var group = Group(app!, userId!);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            var notifications = await _redisNotificationHelper.GetAll(app, userId);
            foreach (var notification in notifications)
            {
                await Clients.Caller.SendAsync("ReceiveNotification", notification.NotificationId + "♠*♠" + notification.Message);
            }
            await base.OnConnectedAsync();
        }
       
        private static string Group(string app, string userId)
            => $"USR:{app}:{userId}";
     
    }

}
