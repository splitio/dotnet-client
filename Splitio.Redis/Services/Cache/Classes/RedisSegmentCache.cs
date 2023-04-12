using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSegmentCache : RedisCacheBase, ISegmentCache
    {
        private const string segmentKeyPrefix = "segment.";
        private const string segmentNameKeyPrefix = "segment.{segmentname}.";
        private const string segmentsKeyPrefix = "segments.";

        public RedisSegmentCache(IRedisAdapter redisAdapter,
            string userPrefix = null) : base(redisAdapter, userPrefix)
        { }

        public async Task AddToSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var valuesToAdd = segmentKeys.Select(x => (RedisValue)x).ToArray();

            await _redisAdapter.SAddAsync(key, valuesToAdd);
        }

        public async Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var valuesToRemove = segmentKeys.Select(x => (RedisValue)x).ToArray();

            await _redisAdapter.SRemAsync(key, valuesToRemove);
        }

        public async Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";

            return await _redisAdapter.SIsMemberAsync(redisKey, key);
        }

        public async Task SetChangeNumberAsync(string segmentName, long changeNumber)
        {
            var key = RedisKeyPrefix + segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";

            await _redisAdapter.SetAsync(key, changeNumber.ToString());
        }

        public async Task<long> GetChangeNumberAsync(string segmentName)
        {
            var key = RedisKeyPrefix + segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";
            var changeNumberString = await _redisAdapter.GetAsync(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public async Task<List<string>> GetSegmentNamesAsync()
        {
            var key = $"{RedisKeyPrefix}{segmentsKeyPrefix}registered";
            var result = await _redisAdapter.SMembersAsync(key);

            return result.Select(x => (string)x).ToList();
        }

        public async Task<List<string>> GetSegmentKeysAsync(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var keys = await _redisAdapter.SMembersAsync(key);

            if (keys == null)
                return new List<string>();

            return keys.Select(k => (string)k).ToList();
        }

        public Task<int> SegmentsCountAsync()
        {
            return Task.FromResult(0); // No-op
        }

        public int SegmentKeysCount()
        {
            return 0; // No-op
        }

        public void Flush()
        {
            _redisAdapter.Flush();
        }

        public void Clear()
        {
            return;
        }

        public async Task<long> RegisterSegmentsAsync(List<string> segmentNames)
        {
            var key = $"{RedisKeyPrefix}{segmentsKeyPrefix}registered";
            var segments = segmentNames.Select(x => (RedisValue)x).ToArray();

            return await _redisAdapter.SAddAsync(key, segments);
        }
    }
}
