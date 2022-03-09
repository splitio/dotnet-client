using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisSenderAdapter : IImpressionsSenderAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(RedisSenderAdapter));

        private readonly IImpressionsCache _impressionsCache;

        public RedisSenderAdapter(IImpressionsCache impressionsCache)
        {
            _impressionsCache = impressionsCache;
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

                _impressionsCache.RecordUniqueKeys(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording unique keys.", ex);
            }
        }

        public void RecordImpressionsCount(ConcurrentDictionary<KeyCache, int> impressionsCount)
        {
            try
            {
                var values = new Dictionary<string, int>();

                foreach (var item in impressionsCount)
                {
                    values.Add($"{item.Key.SplitName}::{item.Key.TimeFrame}", item.Value);
                }

                _impressionsCache.RecordImpressionsCount(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording impressions count.", ex);
            }
        }
    }
}
