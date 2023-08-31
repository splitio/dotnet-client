using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisSenderAdapter : IImpressionsSenderAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(RedisSenderAdapter));

        private readonly IImpressionsCache _impressionsCache;

        public RedisSenderAdapter(IImpressionsCache impressionsCache)
        {
            _impressionsCache = impressionsCache;
        }

        public Task RecordUniqueKeysAsync(List<Mtks> uniques)
        {
            try
            {
                _impressionsCache.RecordUniqueKeys(uniques);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording unique keys.", ex);
            }

            return Task.FromResult(0);
        }

        public Task RecordImpressionsCountAsync(List<ImpressionsCountModel> impressionsCount)
        {
            try
            {
                var values = new Dictionary<string, int>();

                foreach (var item in impressionsCount)
                {
                    values.Add($"{item.SplitName}::{item.TimeFrame}", item.Count);
                }

                _impressionsCache.RecordImpressionsCount(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording impressions count.", ex);
            }

            return Task.FromResult(0);
        }
    }
}
