using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisUniqueKeysStorage : IRedisUniqueKeysStorage
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(RedisUniqueKeysStorage));

        private string UniqueKeysKey => "{prefix}.SPLITIO.uniquekeys"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private readonly IRedisAdapter _redisAdapter;
        private readonly string _userPrefix;

        public RedisUniqueKeysStorage(IRedisAdapter redisAdapter,
            string userPrefix)
        {
            _redisAdapter = redisAdapter;
            _userPrefix = userPrefix;
        }

        public void RecordUniqueKeys(List<string> uniqueKeys)
        {
            var uniques = uniqueKeys.Select(x => (RedisValue)x).ToArray();

            _redisAdapter.SAdd(UniqueKeysKey, uniques);
        }
    }
}
