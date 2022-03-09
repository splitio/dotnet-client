using Splitio.Redis.Services.Impressions.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisUniqueKeysSenderAdapter : IImpressionsSenderAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(RedisUniqueKeysSenderAdapter));

        private readonly IRedisUniqueKeysStorage _redisUniqueKeysStorage;

        public RedisUniqueKeysSenderAdapter(IRedisUniqueKeysStorage redisUniqueKeysStorage)
        {
            _redisUniqueKeysStorage = redisUniqueKeysStorage;
        }

        public void RecordImpressionsCount(ConcurrentDictionary<KeyCache, int> impressionsCount)
        {
            // TODO: I will implement this method in the next PR.
            throw new NotImplementedException();
        }

        public void RecordUniqueKeys(ConcurrentDictionary<string, HashSet<string>> uniques)
        {
            try
            {
                var values = new List<string>();

                var featureNames = uniques.Keys;

                foreach (var unique in uniques)
                {
                    foreach (var key in unique.Value)
                    {
                        values.Add($"{unique.Key}::{key}");
                    }
                }

                _redisUniqueKeysStorage.RecordUniqueKeys(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording unique keys.", ex);
            }
        }
    }
}
