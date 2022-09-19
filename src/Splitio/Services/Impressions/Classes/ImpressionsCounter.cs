using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Linq;

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
            ITasksManager tasksManager) : base(config, tasksManager)
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
                SendBulkData();
            }
        }

        protected override void StartTask()
        {
            _tasksManager.StartPeriodic(() => SendBulkData(), _taskInterval * 1000, _cancellationTokenSource, "Main Impressions Count Sender.");
        }

        protected override void SendBulkData()
        {
            lock (_lock)
            {
                var impressions = new ConcurrentDictionary<KeyCache, int>(_cache);
                _cache.Clear();

                if (impressions.Count <= 0) return;

                try
                {
                    var values = impressions
                        .Select(x => new ImpressionsCountModel(x.Key, x.Value))
                        .ToList();

                    if (values.Count <= _maxBulkSize)
                    {
                        _senderAdapter.RecordImpressionsCount(values);
                        return;
                    }

                    while (values.Count > 0)
                    {
                        var bulkToPost = Util.Helper.TakeFromList(values, _maxBulkSize);

                        _senderAdapter.RecordImpressionsCount(bulkToPost);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught sending impressions count.", e);
                }
            }
        }
    }
}
