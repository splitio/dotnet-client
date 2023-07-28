using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSegmentCache : RedisSegmentCacheAsync, ISegmentCache
    {
        public RedisSegmentCache(IRedisAdapter redisAdapter, string userPrefix = null) : base(redisAdapter, userPrefix) { }

        #region Producer
        public void AddToSegment(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var valuesToAdd = segmentKeys.Select(x => (RedisValue)x).ToArray();

            _redisAdapter.SAdd(key, valuesToAdd);
        }

        public void RemoveFromSegment(string segmentName, List<string> segmentKeys)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var valuesToRemove = segmentKeys.Select(x => (RedisValue)x).ToArray();

            _redisAdapter.SRem(key, valuesToRemove);
        }

        public void SetChangeNumber(string segmentName, long changeNumber)
        {
            var key = RedisKeyPrefix + SegmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";

            _redisAdapter.Set(key, changeNumber.ToString());
        }

        public void Clear()
        {
            return;
        }
        #endregion

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
