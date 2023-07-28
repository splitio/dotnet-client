using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSegmentCacheAsync : RedisCacheBase, ISegmentCacheAsync
    {
        protected const string SegmentKeyPrefix = "segment.";
        protected const string SegmentNameKeyPrefix = "segment.{segmentname}.";
        protected const string SegmentsKeyPrefix = "segments.";

        public RedisSegmentCacheAsync(IRedisAdapter redisAdapter, string userPrefix = null) : base(redisAdapter, userPrefix) { }

        #region Producer
        public async Task AddToSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var valuesToAdd = segmentKeys
                .Select(x => (RedisValue)x)
                .ToArray();

            await _redisAdapter.SAddAsync(key, valuesToAdd);
        }

        public async Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var valuesToRemove = segmentKeys
                .Select(x => (RedisValue)x)
                .ToArray();

            await _redisAdapter.SRemAsync(key, valuesToRemove);
        }

        public async Task SetChangeNumberAsync(string segmentName, long changeNumber)
        {
            var key = RedisKeyPrefix + SegmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";

            await _redisAdapter.SetAsync(key, changeNumber.ToString());
        }

        public Task ClearAsync()
        {
            return Task.FromResult(0);
        }
        #endregion

        #region Consumer
        public async Task<long> GetChangeNumberAsync(string segmentName)
        {
            var key = RedisKeyPrefix + SegmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";
            var changeNumberString = await _redisAdapter.GetAsync(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public async Task<List<string>> GetSegmentKeysAsync(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var keys = await _redisAdapter.SMembersAsync(key);

            if (keys == null) return new List<string>();

            return keys
                .Select(k => (string)k)
                .ToList();
        }

        public async Task<List<string>> GetSegmentNamesAsync()
        {
            var key = $"{RedisKeyPrefix}{SegmentsKeyPrefix}registered";
            var result = await _redisAdapter.SMembersAsync(key);

            return result
                .Select(x => (string)x)
                .ToList();
        }

        public async Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return await _redisAdapter.SIsMemberAsync(redisKey, key);
        }

        public Task<int> SegmentKeysCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> SegmentsCountAsync()
        {
            return Task.FromResult(0);
        }
        #endregion
    }
}
