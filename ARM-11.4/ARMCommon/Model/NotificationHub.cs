using ARMCommon.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ARMCommon.Model
{
    public class NotificationHub : Hub
    {
        private readonly IRedisHelper _redis;
        private readonly ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();

        public NotificationHub(IRedisHelper redis)
        {
            _redis = redis;
        }
        public async Task SendNotificationToUser(string userId, string message)
        {

            Console.WriteLine("Sending message '{0}' to userId: {1}", message, userId);
            await Clients.Client(userId).SendAsync("ReceiveNotification", message);
        }

        public string GetConnectionId() => Context.ConnectionId;


        public async Task PassConnectionId(string userId, string connectionId)
        {
            _redis.StringSet(userId, connectionId);

        }

        public override Task OnConnectedAsync()
        {
            _connections.TryAdd(Context.ConnectionId, Context.User.Identity.Name);
            var activeConnections = _connections.Count;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _connections.TryRemove(Context.ConnectionId, out _);
            var activeConnections = _connections.Count;
            return base.OnDisconnectedAsync(exception);
        }




    }

}
