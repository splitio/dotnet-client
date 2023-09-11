using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSegmentCache : RedisCacheBase, ISegmentCacheConsumer
    {
        protected const string SegmentKeyPrefix = "segment.";
        protected const string SegmentNameKeyPrefix = "segment.{segmentname}.";
        protected const string SegmentsKeyPrefix = "segments.";

        public RedisSegmentCache(IRedisAdapter redisAdapter, string userPrefix = null) : base(redisAdapter, userPrefix) { }

        #region Consumer
        public List<string> GetSegmentNames()
        {
            var key = $"{RedisKeyPrefix}{SegmentsKeyPrefix}registered";
            var result = _redisAdapter.SMembers(key);

            return result.Select(x => (string)x).ToList();
        }

        public List<string> GetSegmentKeys(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var keys = _redisAdapter.SMembers(key);

            if (keys == null)
                return new List<string>();

            return keys.Select(k => (string)k).ToList();
        }

        public bool IsInSegment(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return _redisAdapter.SIsMember(redisKey, key);
        }

        public long GetChangeNumber(string segmentName)
        {
            var key = RedisKeyPrefix + SegmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";
            var changeNumberString = _redisAdapter.Get(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public int SegmentsCount()
        {
            return 0; // No-op
        }

        public int SegmentKeysCount()
        {
            return 0; // No-op
        }

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

        #region Only for tests
        public long RegisterSegment(string segmentName)
        {
            return RegisterSegments(new List<string>() { segmentName });
        }

        public long RegisterSegments(List<string> segmentNames)
        {
            var key = $"{RedisKeyPrefix}{SegmentsKeyPrefix}registered";
            var segments = segmentNames.Select(x => (RedisValue)x).ToArray();

            return _redisAdapter.SAdd(key, segments);
        }
        #endregion
    }
}
