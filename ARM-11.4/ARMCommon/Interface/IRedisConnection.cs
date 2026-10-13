namespace ARMCommon.Interface
{
    public interface IRedisConnection
    {
        abstract Task<string> GetRedisConfiguration(string appName);
        abstract string GetARMRedisConfiguration(string appName);
    }
}
