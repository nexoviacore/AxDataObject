using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
namespace ARMCommon.Helpers
{
    public class RedisCacheHelper : IRedisCacheHelper
    {
        private IConnectionMultiplexer _redis;
        private string _appName;
        private RedisSettings _redisSettings;

        public RedisCacheHelper()
        {                 
        }

        public IConnectionMultiplexer Redis
        {
            get { return _redis; }
            set { _redis = value; }
        }

        public RedisSettings RedisSettings
        {
            get { return _redisSettings; }
            set { _redisSettings = value; }
        }

        public string AppName
        {
            get { return _appName; }
            set { _appName = value; }
        }

        public async Task<IConnectionMultiplexer> OpenConnectionAsync()
        {
            try
            {
                if (_redis != null)
                {
                    await _redis.CloseAsync();
                    _redis.Dispose();
                }
                _redis = await ConnectionMultiplexer.ConnectAsync(GetRedisConfig());
                return _redis;
            }
            catch
            {
                return null;
            }           
        }
        public async Task<bool> CloseAsync()
        {
            try
            {
                if (_redis != null)
                {
                    await _redis.CloseAsync();
                    _redis.Dispose();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string StringGet(string key)
        {
            
            var db = _redis.GetDatabase();
            return db.StringGet(key);
        }

        public bool StringSet(string key, string value, int expiry = 0)
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
        public async Task<string> StringGetAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            return await db.StringGetAsync(key);
        }
        public async Task<bool> StringSetAsync(string key, string value, int expiry = 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            
            var db = _redis.GetDatabase();
            if (expiry > 0)
            {
                return await db.StringSetAsync(key, bytes, TimeSpan.FromSeconds(expiry));
            }
            else
            {
                return await db.StringSetAsync(key, bytes);
            }
        }

        public async Task<object> ObjectGetAsync(string key)
        {
            object result = null;
            
            var db = _redis.GetDatabase();
            var data = (byte[])await db.StringGetAsync(key);

#pragma warning disable SYSLIB0011
            if (data != null)
            {
                using (var stream = new MemoryStream(data))
                {
                    result = (object)new BinaryFormatter().Deserialize(stream);
                }
            }
#pragma warning restore SYSLIB0011

            return result;
        }

        public async Task<bool> StringSetBinaryAsync(string key, string value, int expiry = 0)
        {
            return await ObjectSetAsync(key, value, expiry);
        }

        public async Task<string> StringGetBinaryAsync(string key)
        {
            var result = await ObjectGetAsync(key);
            if (result == null)
                return null;
            else
                return result.ToString();

        }

        public async Task<bool> ObjectSetAsync(string key, object value, int expiry = 0)
        {
            byte[] bytes;

#pragma warning disable SYSLIB0011
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, value);
                bytes = stream.ToArray();
            }
#pragma warning restore SYSLIB0011

            
            var db = _redis.GetDatabase();
            if (expiry > 0)
            {
                return await db.StringSetAsync(key, bytes, TimeSpan.FromSeconds(expiry));
            }
            else
            {
                return await db.StringSetAsync(key, bytes);
            }
        }

        public bool KeyDelete(string key)
        {
            
            var db = _redis.GetDatabase();
            return db.KeyDelete(key);
        }

        public bool KeyExists(string key)
        {
            
            var db = _redis.GetDatabase();
            return db.KeyExists(key);
        }
        public async Task<bool> KeyExistsAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        public async Task<bool> KeyDeleteAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
        public long KeysDelete(RedisKey[] keys)
        {
            
            var db = _redis.GetDatabase();
            return db.KeyDelete(keys);
        }
        public async Task<long> KeysDeleteAsync(RedisKey[] keys)
        {
            
            var db = _redis.GetDatabase();
            var result = await db.KeyDeleteAsync(keys);
            return result;
        }
        public string HashGet(string key, string field)
        {
            
            var db = _redis.GetDatabase();
            return db.HashGet(key, field);
        }
        public async Task<string> HashGetAsync(string key, string field)
        {
            
            var db = _redis.GetDatabase();
            return await db.HashGetAsync(key, field);
        }


        public RedisValue[] HashGetEntries(string key, RedisValue[] fields)
        {
            
            var db = _redis.GetDatabase();
            return db.HashGet(key, fields);
        }
        public async Task<RedisValue[]> HashGetEntriesAsync(string key, RedisValue[] fields)
        {
            
            var db = _redis.GetDatabase();
            return await db.HashGetAsync(key, fields);
        }
        public HashEntry[] HashGetAll(string key)
        {
            
            var db = _redis.GetDatabase();
            return db.HashGetAll(key);
        }
        public async Task<HashEntry[]> HashGetAllAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            return await db.HashGetAllAsync(key);
        }

        public async Task<Dictionary<string, string>> HashGetAllDictAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            var allfields = await db.HashGetAllAsync(key);
            var dict = new Dictionary<string, string>();
            if (allfields != null)
            {
                foreach (var entry in allfields)
                {
                    dict.Add(entry.Name, entry.Value.ToString());
                }
            }
            return dict;
        }

        public bool HashSet(string key, string field, string value, int expiry = 0)
        {
            
            var db = _redis.GetDatabase();
            var result = db.HashSet(key, field, value);
            if (expiry > 0)
            {
                db.KeyExpire(key, TimeSpan.FromSeconds(expiry));
            }
            return result;
        }
        public async Task<bool> HashSetAsync(string key, string field, string value, int expiry = 0)
        {
            
            var db = _redis.GetDatabase();
            var result = await db.HashSetAsync(key, field, value);
            if (expiry > 0)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(expiry));
            }
            return result;
        }
        public bool HashSetEntries(string key, HashEntry[] hashEntries, int expiry = 0)
        {
            
            var db = _redis.GetDatabase();
            db.HashSetAsync(key, hashEntries);
            if (expiry > 0)
            {
                db.KeyExpire(key, TimeSpan.FromSeconds(expiry));
            }
            return true;
        }
        public async Task<bool> HashSetEntriesAsync(string key, HashEntry[] hashEntries, int expiry = 0)
        {
            
            var db = _redis.GetDatabase();
            await db.HashSetAsync(key, hashEntries);
            if (expiry > 0)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(expiry));
            }
            return true;
        }

        public RedisType KeyType(string key)
        {
            
            var db = _redis.GetDatabase();
            return db.KeyType(key);
        }
        public async Task<RedisType> KeyTypeAsync(string key)
        {
            
            var db = _redis.GetDatabase();
            return await db.KeyTypeAsync(key);
        }
        public RedisResult Execute(string command)
        {
            
            var db = _redis.GetDatabase();
            return db.Execute(command);
        }
        public async Task<RedisResult> ExecuteAsync(string command)
        {
            
            var db = _redis.GetDatabase();
            var result = await db.ExecuteAsync(command);
            return result;
        }

        public async Task<bool> HashDeleteAsync(string key, string field)
        {
            
            var db = _redis.GetDatabase();
            var result = await db.HashDeleteAsync(key, field);
            return result;
        }
        public async Task<object> ObjectHashGetAsync(string key, string field)
        {
            object result = null;
            
            var db = _redis.GetDatabase();
            var data = (byte[])await db.HashGetAsync(key, field);

#pragma warning disable SYSLIB0011
            if (data != null)
            {
                using (var stream = new MemoryStream(data))
                {
                    result = (object)new BinaryFormatter().Deserialize(stream);
                }
            }
#pragma warning restore SYSLIB0011

            return result;
        }
       
        public async Task<bool> ObjectHashSetAsync(string key, string field, object value, int expiry = 0)
        {
            byte[] bytes;

#pragma warning disable SYSLIB0011
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, value);
                bytes = stream.ToArray();
            }
#pragma warning restore SYSLIB0011

            
            var db = _redis.GetDatabase();
            var result = await db.HashSetAsync(key, field, bytes);
            
            if (result && expiry > 0)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(expiry));
            }

            return result;

        }

        public object ObjectHashGet(string key, string field)
        {
            object result = null;

            var db = _redis.GetDatabase();
            var data = (byte[])db.HashGet(key, field);

#pragma warning disable SYSLIB0011
            if (data != null)
            {
                using (var stream = new MemoryStream(data))
                {
                    result = (object)new BinaryFormatter().Deserialize(stream);
                }
            }
#pragma warning restore SYSLIB0011

            return result;
        }

        public bool ObjectHashSet(string key, string field, object value, int expiry = 0)
        {
            byte[] bytes;

#pragma warning disable SYSLIB0011
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, value);
                bytes = stream.ToArray();
            }
#pragma warning restore SYSLIB0011


            var db = _redis.GetDatabase();
            var result = db.HashSet(key, field, bytes);

            if (result && expiry > 0)
            {
                db.KeyExpire(key, TimeSpan.FromSeconds(expiry));
            }

            return result;

        }


        public async Task<bool> HashSetBinaryAsync(string key, string field, string value, int expiry = 0)
        {
            return await ObjectHashSetAsync(key, field, value, expiry);
        }

        public async Task<string> HashGetBinaryAsync(string key, string field)
        {
            var result = await ObjectHashGetAsync(key, field);
            if (result == null)
                return null;
            else
                return result.ToString();

        }

        public async Task<bool> DeleteKeysByPrefixAsync(string pattern)
        {
            
            var db = _redis.GetDatabase();
            var keysToDelete = new List<RedisKey>();

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var cursor = 0L;
            do
            {
                var scanResult = server.Execute("SCAN", cursor.ToString(), "MATCH", pattern + "*");
                var result = (RedisResult[])scanResult;

                cursor = (long)result[0];
                var keys = (RedisKey[])result[1];

                keysToDelete.AddRange(keys);
            } while (cursor != 0);

            if (keysToDelete.Any())
            {
                var deletedCount = await db.KeyDeleteAsync(keysToDelete.ToArray());
                return deletedCount > 0;
            }

            return false;
        }

        public async Task<Dictionary<string, object>> ObjectHashGetFieldsAsync(string key, string[] fields)
        {
            var result = new Dictionary<string, object>();

            var db = _redis.GetDatabase();

            RedisValue[] redisFields = fields.Select(f => (RedisValue)f).ToArray();

            var values = await db.HashGetAsync(key, redisFields);

#pragma warning disable SYSLIB0011
            for (int i = 0; i < redisFields.Length; i++)
            {
                if (!values[i].IsNull)
                {
                    var data = (byte[])values[i];

                    using (var stream = new MemoryStream(data))
                    {
                        var obj = (object)new BinaryFormatter().Deserialize(stream);
                        result[redisFields[i]] = obj;
                    }
                }
            }
#pragma warning restore SYSLIB0011

            return result;
        }

        public Dictionary<string, object> ObjectHashGetFields(string key, string[] fields)
        {
            var result = new Dictionary<string, object>();

            var db = _redis.GetDatabase();

            RedisValue[] redisFields = fields.Select(f => (RedisValue)f).ToArray();

            var values = db.HashGet(key, redisFields);

#pragma warning disable SYSLIB0011
            for (int i = 0; i < redisFields.Length; i++)
            {
                if (!values[i].IsNull)
                {
                    var data = (byte[])values[i];

                    using (var stream = new MemoryStream(data))
                    {
                        var obj = (object)new BinaryFormatter().Deserialize(stream);
                        result[redisFields[i]] = obj;
                    }
                }
            }
#pragma warning restore SYSLIB0011

            return result;
        }

        public bool ObjectHashSetFields(string key, Dictionary<string, object> data)
        {           
            foreach (var item in data)
            {
                ObjectHashSet(key, item.Key, JsonConvert.SerializeObject(item.Value));
            }
            return true;
        }

        public async Task<bool> ObjectHashSetFieldsAsync(string key, Dictionary<string, object> data)
        {
            foreach(var item in data)
            {
                await ObjectHashSetAsync(key, item.Key, JsonConvert.SerializeObject(item.Value));
            }
            return true;
        }

        public async Task<bool> SetKeyExpiryAsync(string key, int expiry)
        {
            var db = _redis.GetDatabase();
            if (expiry > 0)
            {
                return await db.KeyExpireAsync(key, TimeSpan.FromSeconds(expiry));
            }
            return true;
        }

        public bool SetKeyExpiry(string key, int expiry)
        {
            var db = _redis.GetDatabase();
            if (expiry > 0)
            {
                return db.KeyExpire(key, TimeSpan.FromSeconds(expiry));
            }
            return true;
        }

        public async Task<int> GetKeyExpiryAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();

                var expiry = await db.KeyTimeToLiveAsync(key);

                if (!expiry.HasValue)
                    return 0;

                var seconds = (int)Math.Floor(
                    expiry.Value.TotalSeconds);

                return seconds > 0 ? seconds : 0;
            }
            catch
            {
                return 0;
            }
        }

        public int GetKeyExpiry(string key)
        {
            try
            {
                var db = _redis.GetDatabase();

                var expiry = db.KeyTimeToLive(key);

                if (!expiry.HasValue)
                    return 0;

                var seconds = (int)Math.Floor(
                    expiry.Value.TotalSeconds);

                return seconds > 0 ? seconds : 0;
            }
            catch
            {
                return 0;
            }
        }


        public ConfigurationOptions GetRedisConfig()
        {            
            string redisHost = $"{_redisSettings.Host}:{_redisSettings.Port}";
            string redisPass = _redisSettings.DecryptedPwd;

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
