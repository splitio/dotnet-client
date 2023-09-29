using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSegmentCache : RedisCacheBase, ISegmentCacheConsumer
    {
        protected const string SegmentKeyPrefix = "segment.";
        protected const string SegmentNameKeyPrefix = "segment.{segmentname}.";
        protected const string SegmentsKeyPrefix = "segments.";

        private readonly IRedisAdapterConsumer _redisAdapterConsumer;

        public RedisSegmentCache(IRedisAdapterConsumer redisAdapter, string userPrefix = null) : base(userPrefix)
        {
            _redisAdapterConsumer = redisAdapter;
        }

        #region Consumer

        public bool IsInSegment(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return _redisAdapterConsumer.SIsMember(redisKey, key);
        }

        public async Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return await _redisAdapterConsumer.SIsMemberAsync(redisKey, key);
        }

        public long GetChangeNumber(string segmentName)
        {
            return 0; // No-op
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
    }
}
