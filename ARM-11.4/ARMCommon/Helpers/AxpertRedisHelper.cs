using StackExchange.Redis;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace ARMCommon.Helpers
{
    public class AxpertRedisHelper
    {
        private readonly IConnectionMultiplexer _redis;
        public AxpertRedisHelper(string redisHost, string redisPass)
        {
            _redis = ConnectionMultiplexer.Connect(GetRedisConfig(redisHost, redisPass));
        }

        public void CloseConnection()
        {
            _redis.Close();
        }

        public bool StringSet(string key, string value, int expiry = 0, bool isByteArray = false)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            var db = _redis.GetDatabase();
            if (expiry > 0)
            {
                return db.StringSet(key, bytes, TimeSpan.FromSeconds(expiry));
            }
            else
            {
                return db.StringSet(key, bytes);
            }
        }

        public ConfigurationOptions GetRedisConfig(string redisHost, string redisPass)
        {
            HashSet<string> redisCommands = new HashSet<string>
                {
                    "CLUSTER",
                    "PING", "ECHO", "CLIENT",
                    "SUBSCRIBE", "UNSUBSCRIBE", "NULL",
                    "INFO", "CONFIG"
                };

            ConfigurationOptions config = new ConfigurationOptions
            {
                SyncTimeout = int.MaxValue,
                KeepAlive = 60,
                Password = redisPass,
                AbortOnConnectFail = true,
                AllowAdmin = true,
                CommandMap = CommandMap.Create(redisCommands, available: false)
            };

            config.EndPoints.Add(redisHost);

            return config;
        }

    }
}
