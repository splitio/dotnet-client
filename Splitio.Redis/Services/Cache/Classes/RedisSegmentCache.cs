﻿using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
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

        private readonly IRedisAdapterConsumer _redisAdapterConsumer;

        public RedisSegmentCache(IRedisAdapterConsumer redisAdapter, string userPrefix = null) : base(userPrefix)
        {
            _redisAdapterConsumer = redisAdapter;
        }

        #region Consumer
        public List<string> GetSegmentNames()
        {
            var key = $"{RedisKeyPrefix}{SegmentsKeyPrefix}registered";
            var result = _redisAdapterConsumer.SMembers(key);

            return result.Select(x => (string)x).ToList();
        }

        public List<string> GetSegmentKeys(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var keys = _redisAdapterConsumer.SMembers(key);

            if (keys == null)
                return new List<string>();

            return keys.Select(k => (string)k).ToList();
        }

        public bool IsInSegment(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return _redisAdapterConsumer.SIsMember(redisKey, key);
        }

        public long GetChangeNumber(string segmentName)
        {
            var key = RedisKeyPrefix + SegmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till";
            var changeNumberString = _redisAdapterConsumer.Get(key);
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
            var changeNumberString = await _redisAdapterConsumer.GetAsync(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public async Task<List<string>> GetSegmentKeysAsync(string segmentName)
        {
            var key = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";
            var keys = await _redisAdapterConsumer.SMembersAsync(key);

            if (keys == null) return new List<string>();

            return keys
                .Select(k => (string)k)
                .ToList();
        }

        public async Task<List<string>> GetSegmentNamesAsync()
        {
            var key = $"{RedisKeyPrefix}{SegmentsKeyPrefix}registered";
            var result = await _redisAdapterConsumer.SMembersAsync(key);

            return result
                .Select(x => (string)x)
                .ToList();
        }

        public async Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            var redisKey = $"{RedisKeyPrefix}{SegmentKeyPrefix}{segmentName}";

            return await _redisAdapterConsumer.SIsMemberAsync(redisKey, key);
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
