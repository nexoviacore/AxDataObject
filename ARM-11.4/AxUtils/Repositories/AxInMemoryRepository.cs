using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using AxUtils.Interface;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Runtime.Serialization.Formatters.Binary;

namespace AxUtils.Repositories
{
    public class AxInMemoryRepository : IAxInMemoryRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        public AppSettingsService _appSettingsService;
        public AppSettingsHelper _appSettingsHelper;
        public AxInMemoryRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
            _appSettingsService = new AppSettingsService();
            _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appSettingsHelper = _appSettingsHelper;
        }
        public async Task<AxInMemDataResult> AxReadInMemory(AxInMemReadData menuData)
        {
            try
            {
                string keyPattern = menuData.InMemoryKey + "-♣";
                var config = _appSettingsHelper.GetRedisSettings(menuData.AccessCode);
                string redisConnectionString = $"{config.Host}:{config.Port},password={config.DecryptedPwd}";
                using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                IDatabase db = redis.GetDatabase();
                if (keyPattern.Contains("*"))
                {
                    var server = redis.GetServer(redis.GetEndPoints().First());
                    var result = new Dictionary<string, object>();
                    foreach (RedisKey redisKey in server.Keys(pattern: keyPattern))
                    {
                        RedisType keyType = await db.KeyTypeAsync(redisKey);
                        switch (keyType)
                        {
                            case RedisType.String:
                                {
                                    RedisValue value = await db.StringGetAsync(redisKey);
                                    if (menuData.IsBinary == "true")
                                    {
                                        try
                                        {
                                            byte[] bytes = value;
                                            object obj = DeserializeBinary(bytes);
                                            result[redisKey.ToString()] = obj is string str ? str : JsonConvert.SerializeObject(obj);
                                        }
                                        catch
                                        {
                                            result[redisKey.ToString()] = Convert.ToBase64String((byte[])value);
                                        }
                                    }
                                    else
                                    {
                                        result[redisKey.ToString()] = value.ToString();
                                    }
                                    break;
                                }
                            case RedisType.Hash:
                                {
                                    if (!string.IsNullOrWhiteSpace(menuData.InMemorySubKey))
                                    {
                                        RedisValue value = await db.HashGetAsync(redisKey, menuData.InMemorySubKey);
                                        if (menuData.IsBinary == "true")
                                        {
                                            try
                                            {
                                                byte[] bytes = value;
                                                object obj = DeserializeBinary(bytes);
                                                result[redisKey.ToString()] = obj is string str ? str : JsonConvert.SerializeObject(obj);
                                            }
                                            catch
                                            {
                                                result[redisKey.ToString()] = Convert.ToBase64String((byte[])value);
                                            }
                                        }
                                        else
                                        {
                                            result[redisKey.ToString()] = value.ToString();
                                        }
                                    }
                                    else
                                    {
                                        HashEntry[] entries = await db.HashGetAllAsync(redisKey);
                                        var dict = new Dictionary<string, object>();
                                        foreach (HashEntry entry in entries)
                                        {
                                            if (menuData.IsBinary == "true")
                                            {
                                                try
                                                {
                                                    byte[] bytes = entry.Value;
                                                    object obj = DeserializeBinary(bytes);
                                                    dict[entry.Name.ToString()] = obj;
                                                }
                                                catch
                                                {
                                                    dict[entry.Name.ToString()] = Convert.ToBase64String((byte[])entry.Value);
                                                }
                                            }
                                            else
                                            {
                                                dict[entry.Name.ToString()] = entry.Value.ToString();
                                            }
                                        }
                                        result[redisKey.ToString()] = dict;
                                    }
                                    break;
                                }
                            default:
                                result[redisKey.ToString()] = $"Unsupported Redis type: {keyType}";
                                break;
                        }
                    }

                    return new AxInMemDataResult
                    {
                        Success = true,
                        Message = $"{result.Count} key(s) found",
                        result = JsonConvert.SerializeObject(result, Formatting.Indented)
                    };
                }

                // ============================
                // Single Key Read
                // ============================

                RedisKey key = keyPattern;

                if (!await db.KeyExistsAsync(key))
                {
                    return new AxInMemDataResult
                    {
                        Success = false,
                        Message = $"Key '{menuData.InMemoryKey}' not found"
                    };
                }

                RedisType singleKeyType = await db.KeyTypeAsync(key);

                switch (singleKeyType)
                {
                    case RedisType.String:
                        {
                            RedisValue value = await db.StringGetAsync(key);
                            string result;

                            if (menuData.IsBinary == "true")
                            {
                                try
                                {
                                    byte[] bytes = value;
                                    object obj = DeserializeBinary(bytes);

                                    result = obj is string str
                                        ? str
                                        : JsonConvert.SerializeObject(obj);
                                }
                                catch
                                {
                                    result = Convert.ToBase64String((byte[])value);
                                }
                            }
                            else
                            {
                                result = value.ToString();
                            }

                            return new AxInMemDataResult
                            {
                                Success = true,
                                Message = "String key",
                                result = result
                            };
                        }

                    case RedisType.Hash:
                        {
                            if (!string.IsNullOrWhiteSpace(menuData.InMemorySubKey))
                            {
                                RedisValue value = await db.HashGetAsync(key, menuData.InMemorySubKey);
                                string result;

                                if (menuData.IsBinary == "true")
                                {
                                    try
                                    {
                                        byte[] bytes = value;
                                        object obj = DeserializeBinary(bytes);

                                        result = obj is string str
                                            ? str
                                            : JsonConvert.SerializeObject(obj);
                                    }
                                    catch
                                    {
                                        result = Convert.ToBase64String((byte[])value);
                                    }
                                }
                                else
                                {
                                    result = value.ToString();
                                }

                                return new AxInMemDataResult
                                {
                                    Success = true,
                                    Message = "Hash field",
                                    result = result
                                };
                            }

                            HashEntry[] entries = await db.HashGetAllAsync(key);

                            var dict = new Dictionary<string, object>();

                            foreach (HashEntry entry in entries)
                            {
                                if (menuData.IsBinary == "true")
                                {
                                    try
                                    {
                                        byte[] bytes = entry.Value;
                                        object obj = DeserializeBinary(bytes);
                                        dict[entry.Name.ToString()] = obj;
                                    }
                                    catch
                                    {
                                        dict[entry.Name.ToString()] = Convert.ToBase64String((byte[])entry.Value);
                                    }
                                }
                                else
                                {
                                    dict[entry.Name.ToString()] = entry.Value.ToString();
                                }
                            }

                            return new AxInMemDataResult
                            {
                                Success = true,
                                Message = "Hash key",
                                result = JsonConvert.SerializeObject(dict, Formatting.Indented)
                            };
                        }

                    default:
                        return new AxInMemDataResult
                        {
                            Success = false,
                            Message = $"Unsupported Redis type: {singleKeyType}"
                        };
                }
            }
            catch (Exception ex)
            {
                return new AxInMemDataResult
                {
                    Success = false,
                    Message = ex.ToString()
                };
            }
        }
        public async Task<AxInMemDataResult> AxWriteInMemory(AxInMemWriteData menuData)
        {
            try
            {
                int expiryMins = 10;
                if (!string.IsNullOrEmpty(menuData.KeyExpiryInMins))
                    expiryMins = int.Parse(menuData.KeyExpiryInMins);
                TimeSpan expiry = TimeSpan.FromMinutes(expiryMins);
                menuData.InMemoryKey += "-♣";

                var config = _appSettingsHelper.GetRedisSettings(menuData.AccessCode);

                string redisConnectionString = $"{config.Host}:{config.Port},password={config.DecryptedPwd}";

                using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                IDatabase db = redis.GetDatabase();

                bool keyExists = await db.KeyExistsAsync(menuData.InMemoryKey);

                // HASH WRITE
                if (!string.IsNullOrWhiteSpace(menuData.InMemorySubKey))
                {
                    if (keyExists)
                    {
                        RedisType existingType = await db.KeyTypeAsync(menuData.InMemoryKey);

                        if (existingType != RedisType.Hash)
                        {
                            return new AxInMemDataResult
                            {
                                Success = false,
                                Message = $"Key '{menuData.InMemoryKey}' already exists as '{existingType}'. Cannot write hash field."
                            };
                        }
                    }

                    RedisValue value;

                    if (menuData.IsBinary == "true")
                    {
                        byte[] bytes = SerializeBinary(menuData.InMemoryValue ?? string.Empty);

                        value = bytes;
                    }
                    else
                    {
                        value = menuData.InMemoryValue ?? string.Empty;
                    }

                    bool isNewField = await db.HashSetAsync(menuData.InMemoryKey, menuData.InMemorySubKey, value);
                    await db.KeyExpireAsync(menuData.InMemoryKey, expiry);
                    return new AxInMemDataResult
                    {
                        Success = true,
                        Message = isNewField ? "Hash field created successfully" : "Hash field updated successfully"
                    };
                }

                // STRING WRITE
                if (keyExists)
                {
                    RedisType existingType = await db.KeyTypeAsync(menuData.InMemoryKey);

                    if (existingType != RedisType.String)
                    {
                        return new AxInMemDataResult
                        {
                            Success = false,
                            Message = $"Key '{menuData.InMemoryKey}' already exists as '{existingType}'. Cannot write string value."
                        };
                    }
                }

                bool result;

                if (menuData.IsBinary == "true")
                {
                    byte[] bytes = SerializeBinary(menuData.InMemoryValue ?? string.Empty);
                    result = await db.StringSetAsync(menuData.InMemoryKey, bytes, expiry);
                }
                else
                {
                    result = await db.StringSetAsync(menuData.InMemoryKey, menuData.InMemoryValue ?? string.Empty, expiry);
                }

                return new AxInMemDataResult
                {
                    Success = result,
                    Message = result ? "String value written successfully" : "Failed to write string value"
                };
            }
            catch (Exception ex)
            {
                return new AxInMemDataResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        private static object DeserializeBinary(byte[] bytes)
        {
#pragma warning disable SYSLIB0011
            using var ms = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            return formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011
        }
        private static byte[] SerializeBinary(object obj)
        {
#pragma warning disable SYSLIB0011
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
#pragma warning restore SYSLIB0011
        }
        public async Task<AxInMemDeleteDataResult> AxDeleteInMemory(AxInMemDeleteData menuData)
        {
            try
            {
                var config = _appSettingsHelper.GetRedisSettings(menuData.AccessCode);
                string redisConnectionString = $"{config.Host}:{config.Port},password={config.DecryptedPwd}";
                using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                IDatabase db = redis.GetDatabase();
                string keyPattern = menuData.InMemoryKey + "-♣";
                if (keyPattern.Contains("*"))
                {
                    var server = redis.GetServer(redis.GetEndPoints().First());
                    RedisKey[] keys = server.Keys(pattern: keyPattern).ToArray();
                    if (keys.Length == 0)
                    {
                        return new AxInMemDeleteDataResult
                        {
                            Success = false,
                            Message = $"No keys found matching '{menuData.InMemoryKey}'."
                        };
                    }
                    long deletedCount = await db.KeyDeleteAsync(keys);
                    return new AxInMemDeleteDataResult
                    {
                        Success = true,
                        Message = $"{deletedCount} key(s) deleted successfully."
                    };
                }
                RedisKey key = keyPattern;
                bool exists = await db.KeyExistsAsync(key);
                if (!exists)
                {
                    return new AxInMemDeleteDataResult
                    {
                        Success = false,
                        Message = $"Key '{menuData.InMemoryKey}' not found."
                    };
                }
                bool deleted = await db.KeyDeleteAsync(key);
                return new AxInMemDeleteDataResult
                {
                    Success = deleted,
                    Message = deleted
                        ? $"Key '{menuData.InMemoryKey}' deleted successfully."
                        : $"Failed to delete key '{menuData.InMemoryKey}'."
                };
            }
            catch (Exception ex)
            {
                return new AxInMemDeleteDataResult
                {
                    Success = false,
                    Message = ex.ToString()
                };
            }
        }
    }
}
