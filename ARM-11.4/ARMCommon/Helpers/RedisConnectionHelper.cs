using AgileConnect.EncrDecr.cs;
using ARMCommon.Interface;
using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ARMCommon.Helpers
{
    public class RedisConnectionHelper : IRedisConnection
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;
        private readonly IConnectionMultiplexer _redisconn;
        public Dictionary<string, string> redisConfigDictionry = new Dictionary<string, string>();

        public RedisConnectionHelper(DataContext context, IRedisHelper redis, Utils common)
        {

            _redis = redis;
            _context = context;
            _common = common;

        }

        public async Task<string> GetRedisConfiguration(string appName)
        {
            string redisConnection = "";
            try
            {
                redisConnection = GetARMRedisConfiguration(appName);
                string key = $"{Constants.REDIS_PREFIX.ARMRedisConfiguration.ToString()}_{appName}";

                if (_redis.KeyExists(key))
                {

                    redisConfigDictionry.Add(appName, redisConnection);
                    await _redis.KeyDeleteAsync(key);

                }
                else
                {
                    if (redisConfigDictionry.ContainsKey(appName))
                    {
                        redisConfigDictionry[appName] = redisConnection;
                    }
                    else
                    {
                        redisConfigDictionry.Add(appName, redisConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return redisConnection;
        }

        public string GetARMRedisConfiguration(string appName)
        {
            var userGroup = _context.ARMApps.Find(appName);
            if (userGroup == null)
                throw new KeyNotFoundException("App not found");

            var Key = userGroup.PrivateKey;
            if (Key != null)
            {
                try
                {

                    var ARMAppRedisConfiguration = _common.GetRedisConnections(EncrDecr.Decrypt(userGroup.RedisIP, Key), EncrDecr.Decrypt(userGroup.RedisIP, Key));
                    return ARMAppRedisConfiguration;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }
            else
            {
                return Constants.RESULTS.NO_RECORDS.ToString();
            }

        }





    }
}
