using StackExchange.Redis;

namespace ARMCommon.Interface
{
    public interface IRedisHelper
    {
        string AppName { get; set; }
        string StringGet(string key);
        bool StringSet(string key, string value, int expiry = 0);
        Task<string> StringGetAsync(string key);
        Task<bool> StringSetAsync(string key, string value, int expiry = 0);
        Task<string> StringGetBinaryAsync(string key);
        Task<bool> StringSetBinaryAsync(string key, string value, int expiry = 0);
        Task<object> ObjectGetAsync(string key);
        Task<bool> ObjectSetAsync(string key, object value, int expiry = 0);

        bool KeyDelete(string key);
        abstract Task<bool> KeyExistsAsync(string key);
        bool KeyExists(string key);
        Task<bool> KeyDeleteAsync(string key);

        long KeysDelete(RedisKey[] keys);

        Task<long> KeysDeleteAsync(RedisKey[] keys);

        string HashGet(string key, string field);

        Task<string> HashGetAsync(string key, string field);

        RedisValue[] HashGetEntries(string key, RedisValue[] fields);
        Task<RedisValue[]> HashGetEntriesAsync(string key, RedisValue[] fields);

        HashEntry[] HashGetAll(string key);

        Task<HashEntry[]> HashGetAllAsync(string key);
        Task<Dictionary<string, string>> HashGetAllDictAsync(string key);

        bool HashSet(string key, string field, string value, int expiry = 0);

        Task<bool> HashSetAsync(string key, string field, string value, int expiry = 0);

        bool HashSetEntries(string key, HashEntry[] hashEntries, int expiry = 0);

        Task<bool> HashSetEntriesAsync(string key, HashEntry[] hashEntries, int expiry = 0);

        RedisType KeyType(string key);

        Task<RedisType> KeyTypeAsync(string key);

        RedisResult Execute(string command);

        Task<RedisResult> ExecuteAsync(string command);


        ConfigurationOptions GetRedisConfig();
        Task<bool> DeleteKeysByPrefixAsync(string pattern);

        Task<bool> HashDeleteAsync(string key, string field);
        Task<object> ObjectHashGetAsync(string key, string field);
        Task<bool> ObjectHashSetAsync(string key, string field, object value, int expiry = 0);
        Task<bool> HashSetBinaryAsync(string key, string field, string value, int expiry = 0);
        Task<string> HashGetBinaryAsync(string key, string field);
        Task<bool> SetKeyExpiryAsync(string key, int expiry = 0);
        bool SetKeyExpiry(string key, int expiry = 0);

    }
}
