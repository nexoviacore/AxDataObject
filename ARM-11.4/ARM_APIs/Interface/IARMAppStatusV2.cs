namespace ARM_APIs.Interface
{
    public interface IARMAppStatusV2
    {
        Task<string> TestAppDatabaseConnection(string appConnectionString, string appName);
        Task<string> TestRedisConnection(string redisIP, string redisPassword);
        Task<string> TestAxpertRedisConnection(string axpertredisIP, string axpertredisPassword);
        Task<string> TestRabbitmqConnection(string rabbitmqIP);
        Task<string> TestDatabaseConnectionString(string connectionString);
    }
}
