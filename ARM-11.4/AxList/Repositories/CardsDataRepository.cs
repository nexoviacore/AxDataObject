using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Linq;

namespace AxList.Repositories
{
    public class CardsDataRepository : ICardsDataRepository
    {
        //private readonly IRedisHelper _redis;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public CardsDataRepository(Utils common, IARMLogTraceService traceService)
        {
            //_redis = redis;
            _common = common;
            _traceService = traceService;
        }

        public async Task<SQLResult> GetCardsList(CardsData cards)
        {
           var cardsList = await GetCardsListFromCache(cards);
            if (cardsList == null)
            {
                cardsList = await GetCardsListFromDB(cards);
                if (cardsList != null && cardsList.data != null && string.IsNullOrEmpty(cardsList.error))
                {
                    var jsonData = JsonConvert.SerializeObject(cardsList);
                    await SetCardsListToCache(cards, jsonData);
                }
            }
            return cardsList;
        }

        private async Task<SQLResult> GetCardsListFromCache(CardsData cards)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateCardsListCacheKey(cards.SchemaName, cards.Roles, cards.HomePageCards);
                var cachedCardsList = await _common._redisCache.StringGetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedCardsList))
                {
                    return JsonConvert.DeserializeObject<SQLResult>(cachedCardsList);
                }
                else
                {
                    return null;
                }
            }
            else
                return null;
        }

        private async Task<bool> SetCardsListToCache(CardsData cards, string value)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateCardsListCacheKey(cards.SchemaName, cards.Roles, cards.HomePageCards);
                return await _common._redisCache.StringSetAsync(cacheKey, value);
            }
            else
                return false;
        }

        private async Task<SQLResult> GetCardsListFromDB(CardsData cards)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();
            var dbType = _common._dbHelper?.GetDBType();
            string sql = "";

            if (cards.HomePageCards == false)
            {
                sql = Constants_SQL.GET_DASHBOARDCARDSLIST.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                {
                    sql = Constants_SQL.GET_DASHBOARDCARDSLIST_ORACLE.ToString();
                }
            }
            else
            {
                sql = Constants_SQL.GET_HOMEPAGECARDSLIST.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                {
                    sql = Constants_SQL.GET_HOMEPAGECARDSLIST_ORACLE.ToString();
                }
            }

            string roles = string.Join(",", cards.Roles.Split(',').Select(r => $"'{r}'"));
            sql = sql.Replace("$ROLES$", roles);

            return await _common._dbHelper.ExecuteSQLAsync(sql);
        }

        private string GenerateCardsListCacheKey(string schemaName, string roles, bool homePage)
        {
            var sortedRoles = string.Join(",", roles.Split(',', StringSplitOptions.RemoveEmptyEntries).OrderBy(r => r));
            if(homePage)
                return $"{schemaName}-ARM-AxCardsList-HomePage-{sortedRoles}";
            else
                return $"{schemaName}-ARM-AxCardsList-Dashboard-{sortedRoles}";
        }
    }
}
