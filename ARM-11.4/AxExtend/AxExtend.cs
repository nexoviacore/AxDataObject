using AxExtend.Interface;
using ARMCommon;
using ARMCommon.Model;
using ARMCommon.Interface;
using ARMCommon.Helpers;
using ARMCommon.Filter;
using ARMCommon.ActionFilter;
using ARMCommon.Model;
using ARMCommon.Services;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ARMCommon.Helpers.RabbitMq;

namespace AxExtend
{
    public class AxExtend : IAxExtend 
    {
        private IConfiguration _config;
        private Utils _common;
        private IRedisCacheHelper _redisCache;
        private IAppSettingsHelper _appSettingsHelper;
        private IARMLogTraceService _traceService;
        public AxExtend(IConfiguration config, IHttpContextAccessor http) 
        {
            _config = config;
            var _appSettingsService = new AppSettingsService();
            _traceService = new ARMLogTraceService(http);
            _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _redisCache = new RedisCacheHelper();
            _common = new Utils(_config, _redisCache, _appSettingsHelper, _traceService);
        }

        public async Task<Dictionary<string, string>> GetDBConfigurations(string appName)
        {            
            return await _common.GetDBConfigurations(appName);
        }

        public async Task<bool> OpenDBConnectionAsync(string appName)
        {
            _common._appName = appName;
            var dbConn =  await _common.OpenDBConnectionAsync();
            if (_common._dbHelper != null)
                return true;
            else
                return false;
        }

        public async Task<IDbHelper> GetDB()
        {
            return _common._dbHelper;
        }

        public async Task CloseDBConnectionAsync()
        {
            await _common.CloseDBConnectionAsync();
        }

        public async Task<bool> OpenRedisConnectionAsync(string cacheKey = "")
        {
            if (cacheKey.Split("-").Count() == 1)
                cacheKey = "ARM-" + cacheKey;

           return await _common.OpenRedisConnectionAsync(cacheKey);
        }

        public async Task<IRedisCacheHelper> GetRedis()
        {
            return _common._redisCache;
        }

        public async Task<bool> CloseRedisConnectionAsync()
        {
            return await _common.CloseRedisConnectionAsync();
        }

        public async Task<bool> ValidateSession(string sessionId)
        {
            return await _common.ValidateSession(sessionId);
        }
        public async Task<Dictionary<string, object>> GetSessionData(string sessionId)
        {
            return await _common.GetSessionData(sessionId);
        }

        public async Task<IAppSettingsHelper> GetAppSettings()
        {
            return _appSettingsHelper;
        }

        public async Task<IRabbitMQProducer> GetRabbitMQProducer()
        {
            var rmq = new RabbitMQProducer(_appSettingsHelper);
            return rmq;
        }

        public async Task<IRabbitMQConsumer> GetRabbitMQConsumer()
        {
            var rmq = new RabbitMQConsumer(_appSettingsHelper);
            return rmq;
        }
    }

}

