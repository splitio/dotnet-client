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

        public void AddToSegment(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var valuesToAdd = segmentKeys.Select(x => (RedisValue)x).ToArray();

            _ = _redisAdapter.SAddAsync(key, valuesToAdd).Result;
        }

        public void RemoveFromSegment(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var valuesToRemove = segmentKeys.Select(x => (RedisValue)x).ToArray();

            _ = _redisAdapter.SRemAsync(key, valuesToRemove).Result;
        }

        public bool IsInSegment(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";

            return _redisAdapter.SIsMemberAsync(redisKey, key).Result;
        }

        public void SetChangeNumber(string segmentName, long changeNumber)
        {
            var key = RedisKeyPrefix + segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";

            _ = _redisAdapter.SetAsync(key, changeNumber.ToString()).Result;
        }

        public long GetChangeNumber(string segmentName)
        {
            var key = RedisKeyPrefix + segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";
            var changeNumberString = _redisAdapter.GetAsync(key).Result;
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public List<string> GetSegmentNames()
        {
            var key = $"{RedisKeyPrefix}{segmentsKeyPrefix}registered";
            var result = _redisAdapter.SMembersAsync(key).Result;

            return result.Select(x => (string)x).ToList();
        }

        public List<string> GetSegmentKeys(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{segmentKeyPrefix}{segmentName}";
            var keys = _redisAdapter.SMembersAsync(key).Result;

            if (keys == null)
                return new List<string>();

            return keys.Select(k => (string)k).ToList();
        }

        public int SegmentsCount()
        {
            return 0; // No-op
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
