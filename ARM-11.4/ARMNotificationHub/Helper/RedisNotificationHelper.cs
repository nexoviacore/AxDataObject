using ARMCommon;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMNotificationHub.Models;
using MailKit.Search;
using StackExchange.Redis;
using System.Text.Json;
namespace ARMNotificationHub.Helper
{
    
    public class RedisNotificationHelper : IDisposable
    {
        private bool _disposed;
        public readonly IAppSettingsHelper _appSettingsHelper;
        public readonly IRedisCacheHelper _redisCache;

        public RedisNotificationHelper(IAppSettingsHelper appSettingsHelper, IRedisCacheHelper _redisCacheHelper)
        {
            _appSettingsHelper = appSettingsHelper;
            _redisCache = _redisCacheHelper;            
        }

        private static string DataKey(string app, string userId)
            => $"ARM-{app}-{userId}-Notifications";

        private static string OrderKey(string app, string userId)
            => $"ARM-{app}-{userId}-NotificationsOrder";

        public async Task<Notification> Add(Notification notification)
        {
            notification.CreatedOn = DateTime.Now;

            var dataKey = DataKey(notification.Project, notification.UserId);
            var orderKey = OrderKey(notification.Project, notification.UserId);

            _redisCache.AppName = notification.Project;
            _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(notification.Project);
            _redisCache.Redis = await _redisCache.OpenConnectionAsync();

            if (_redisCache.Redis.IsConnected)
            {
                var _db = _redisCache.Redis.GetDatabase();
                _db.HashSet(dataKey, notification.NotificationId, JsonSerializer.Serialize(notification));
                
                _db.ListRightPush(orderKey, notification.NotificationId);

                var maxNotifications = _appSettingsHelper.GetARMNotificationMaxLimit(notification.Project);
                var maxNotificationCount = Int32.TryParse(maxNotifications, out int count) ? count : 10;

                while (_db.ListLength(orderKey) > maxNotificationCount)
                {
                    var oldId = _db.ListLeftPop(orderKey);
                    if (!oldId.IsNullOrEmpty)
                        _db.HashDelete(dataKey, oldId);
                }

                var expiryTime = _appSettingsHelper.GetARMNotificationExpiry(notification.Project);
                var expiryTimeInHours = Int32.TryParse(expiryTime, out int hours) ? hours : 12; 

                await _db.KeyExpireAsync(dataKey, TimeSpan.FromHours(expiryTimeInHours));
                await _db.KeyExpireAsync(orderKey, TimeSpan.FromHours(expiryTimeInHours));

                await _redisCache.CloseAsync();
            }
            return notification;
        }

        public async Task<IEnumerable<Notification>> GetAll(string app, string userId)
        {
            await CleanupExpired(app, userId);

            var dataKey = DataKey(app, userId);

            _redisCache.AppName = app;
            _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(app);
            _redisCache.Redis = await _redisCache.OpenConnectionAsync();
            if (_redisCache.Redis.IsConnected)
            {
                var _db = _redisCache.Redis.GetDatabase();
                var result =  _db.HashGetAll(dataKey)
                .Select(x => JsonSerializer.Deserialize<Notification>(x.Value!)!)
                .OrderBy(x => x.CreatedOn);

                await _redisCache.CloseAsync();

                return result;
            }
            return null;
        }

        public async Task Delete(string app, string userId, string notificationId)
        {
            _redisCache.AppName = app;
            _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(app);
            _redisCache.Redis = await _redisCache.OpenConnectionAsync();
            if (_redisCache.Redis.IsConnected)
            {
                var _db = _redisCache.Redis.GetDatabase();
                _db.HashDelete(DataKey(app, userId), notificationId);
                _db.ListRemove(OrderKey(app, userId), notificationId);

                await _redisCache.CloseAsync();
            }
        }

        public async Task DeleteAll(string app, string userId)
        {
            _redisCache.AppName = app;
            _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(app);
            _redisCache.Redis = await _redisCache.OpenConnectionAsync();
            if (_redisCache.Redis.IsConnected)
            {
                var _db = _redisCache.Redis.GetDatabase();
                _db.KeyDelete(DataKey(app, userId));
                _db.KeyDelete(OrderKey(app, userId));
                await _redisCache.CloseAsync();
            }
        }

        private async Task CleanupExpired(string app, string userId)
        {
            var now = DateTime.Now;
            var dataKey = DataKey(app, userId);
            var orderKey = OrderKey(app, userId);
            _redisCache.AppName = app;
            _redisCache.RedisSettings = _appSettingsHelper.GetRedisSettings(app);
            _redisCache.Redis = await _redisCache.OpenConnectionAsync();
            if (_redisCache.Redis.IsConnected)
            {
                var _db = _redisCache.Redis.GetDatabase();
                var expiryTime = _appSettingsHelper.GetARMNotificationExpiry(app);
                var expiryTimeInHours = Int32.TryParse(expiryTime, out int hours) ? hours : 12;

                foreach (var id in _db.ListRange(orderKey))
                {
                    var payload = _db.HashGet(dataKey, id);
                    if (payload.IsNullOrEmpty)
                    {
                        _db.ListRemove(orderKey, id);
                        continue;
                    }

                    var msg = JsonSerializer.Deserialize<Notification>(payload!)!;
                    

                    if (now - msg.CreatedOn > TimeSpan.FromHours(expiryTimeInHours))
                    {
                        _db.HashDelete(dataKey, id);
                        _db.ListRemove(orderKey, id);
                    }
                }
                await _redisCache.CloseAsync();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            OnDispose();
            GC.SuppressFinalize(this);
        }

        private void OnDispose()
        {
            if (_redisCache != null && _redisCache.Redis != null && _redisCache.Redis.IsConnected)
            {
                _redisCache.Redis.Close();
            }            
        }
    }

}
