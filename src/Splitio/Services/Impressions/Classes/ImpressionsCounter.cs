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
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsCounter));
        private const int DefaultAmount = 1;

        private readonly IImpressionsSenderAdapter _senderAdapter;
        private readonly ConcurrentDictionary<KeyCache, int> _cache;

        public ImpressionsCounter(ComponentConfig config,
            IImpressionsSenderAdapter senderAdapter,
            ISplitTask task,
            ISplitTask sendBulkDataTask) : base(config, task, sendBulkDataTask)
        {
            _cache = new ConcurrentDictionary<KeyCache, int>();
            _senderAdapter = senderAdapter;
        }

        public void Inc(string splitName, long timeFrame)
        {
            var key = new KeyCache(splitName, timeFrame);

            _log.Debug($"Impressions Count Inc: {splitName} - {timeFrame}");

            _cache.AddOrUpdate(key, DefaultAmount, (keyCache, cacheAmount) => cacheAmount + DefaultAmount);

            if (_cache.Count >= _cacheMaxSize)
            {
                _taskBulkData.Start();
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
                _log.Error("Exception caught sending impressions count.", e);
            }
        }
    }
}
