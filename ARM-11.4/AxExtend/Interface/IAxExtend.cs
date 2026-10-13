using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxExtend.Interface
{
    public interface IAxExtend
    {
        Task<Dictionary<string, string>> GetDBConfigurations(string appName);
        Task<IDbHelper> GetDB();
        Task<bool> OpenDBConnectionAsync(string appName);
        Task CloseDBConnectionAsync();
        Task<bool> OpenRedisConnectionAsync(string cacheKey = "");
        Task<IRedisCacheHelper> GetRedis();
        Task<bool> CloseRedisConnectionAsync();
        Task<bool> ValidateSession(string sessionId);
        Task<Dictionary<string, object>> GetSessionData(string sessionId);
        Task<IRabbitMQProducer> GetRabbitMQProducer();
        Task<IRabbitMQConsumer> GetRabbitMQConsumer();
        Task<IAppSettingsHelper> GetAppSettings();
    }
}
