using ARM_APIs.Interface;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace ARM_APIs.Service
{
    public class ARMAppStatusV2 : IARMAppStatusV2

    {
        public async Task<string> TestAppDatabaseConnection(string appConnectionString, string appName)
        {
            try
            {
                if (appConnectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    using (var connection = new OracleConnection(appConnectionString))
                    {
                        await connection.OpenAsync();
                        await connection.CloseAsync();
                    }
                }
                else if (appConnectionString.Contains("Server", StringComparison.OrdinalIgnoreCase))
                {
                    using (var connection = new NpgsqlConnection(appConnectionString))
                    {
                        await connection.OpenAsync();
                        await connection.CloseAsync();
                    }
                }
                else
                {
                    throw new Exception("Unknown database type");
                }

                return "Connection Successful!";
            }
            catch (Exception ex)
            {
                return $"Connection failed!: {ex.Message}";
            }
        }


        public async Task<string> TestRedisConnection(string redisIP, string redisPassword)
        {
            try
            {
                var options = new ConfigurationOptions
                {
                    EndPoints = { redisIP },
                    Password = redisPassword
                };

                var connection = ConnectionMultiplexer.Connect(options);
                var database = connection.GetDatabase();

                database.StringSet("testkey", "testvalue");

                if (database.StringGet("testkey") == "testvalue")
                {
                    return $"Redis Connection successful!";

                }
                else
                {
                    return $"Redis Connection Failed!";

                }
            }
            catch (Exception ex)
            {
                return $"Invalid Connection Details";
            }
        }

        public async Task<string> TestAxpertRedisConnection(string axpertredisIP, string axpertredisPassword)
        {
            try
            {
                var options = new ConfigurationOptions
                {
                    EndPoints = { axpertredisIP },
                    Password = axpertredisPassword
                };

                var connection = ConnectionMultiplexer.Connect(options);
                var database = connection.GetDatabase();

                database.StringSet("testkey", "testvalue");

                if (database.StringGet("testkey") == "testvalue")
                {
                    return $"AxpertRedis Connection successful!";

                }
                else
                {
                    return $"AxpertRedis Connection Failed!";
                    //return Json""(new { success = false, message = "Connection failed.";
                }
            }
            catch (Exception ex)
            {
                return $"Invalid Connection Details";
            }
        }

        public async Task<string> TestRabbitmqConnection(string rabbitmqIP)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(rabbitmqIP)
            };

            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    return $"Connection successful!";
                }
            }
            catch (Exception ex)
            {
                return $"Connection failed: {ex.Message}";
            }
        }

        public async Task<string> TestDatabaseConnectionString(string connectionString)
        {
            try
            {
                var options = new DbContextOptionsBuilder().UseNpgsql(connectionString).Options;

                using (var dbContext = new DbContext(options))
                {
                    await dbContext.Database.OpenConnectionAsync();
                    return "Connection successful!";
                }
            }
            catch (Exception ex)
            {
                return $"Connection failed: {ex.Message}";
            }
        }


    }
}
