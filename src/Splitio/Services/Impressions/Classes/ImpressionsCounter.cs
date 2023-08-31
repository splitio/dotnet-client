using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsCounter : TrackerComponent, IImpressionsCounter
    {
        private static readonly ISplitLogger Logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsCounter));
        private const int DefaultAmount = 1;

        private readonly IImpressionsSenderAdapter _senderAdapter;
        private readonly ConcurrentDictionary<KeyCache, int> _cache;

        public ImpressionsCounter(ComponentConfig config,
            IImpressionsSenderAdapter senderAdapter,
            ISplitTask task) : base(config, task)
        {
            _cache = new ConcurrentDictionary<KeyCache, int>();
            _senderAdapter = senderAdapter;
        }

        public void Inc(string splitName, long timeFrame)
        {
            var key = new KeyCache(splitName, timeFrame);

            _cache.AddOrUpdate(key, DefaultAmount, (keyCache, cacheAmount) => cacheAmount + DefaultAmount);

            if (_cache.Count >= _cacheMaxSize)
            {
                SendBulkDataAsync();
            }
        }

        protected override async Task SendBulkDataAsync()
        {
            try
            {
                var impressions = new ConcurrentDictionary<KeyCache, int>(_cache);

                _cache.Clear();

                if (impressions.Count <= 0) return;

                var values = impressions
                    .Select(x => new ImpressionsCountModel(x.Key, x.Value))
                    .ToList();

                if (values.Count <= _maxBulkSize)
                {
                    await _senderAdapter.RecordImpressionsCountAsync(values);
                    return;
                }

                while (values.Count > 0)
                {
                    var bulkToPost = Util.Helper.TakeFromList(values, _maxBulkSize);

                    await _senderAdapter.RecordImpressionsCountAsync(bulkToPost);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Exception caught sending impressions count.", e);
            }
        }
    }
}
