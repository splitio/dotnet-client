using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsLog : IImpressionsLog
    {
        protected static readonly ISplitLogger Logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsLog));

        private readonly IImpressionsSdkApiClient _apiClient;
        private readonly ISimpleProducerCache<KeyImpression> _impressionsCache;
        private readonly ISplitTask _task;
        private readonly object _lock = new object();

        public ImpressionsLog(IImpressionsSdkApiClient apiClient,
            ISimpleCache<KeyImpression> impressionsCache,
            ISplitTask task,
            int maximumNumberOfKeysToCache = -1)
        {
            _apiClient = apiClient;
            _impressionsCache = (impressionsCache as ISimpleProducerCache<KeyImpression>) ?? new InMemorySimpleCache<KeyImpression>(new BlockingQueue<KeyImpression>(maximumNumberOfKeysToCache));            
            _task = task;
            _task.SetEventHandler((object sender, ElapsedEventArgs e) => SendBulkImpressions());
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_task.IsRunning()) return;

                _task.Start();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_task.IsRunning())
                    return;

                _task.Stop();
                SendBulkImpressions();
            }
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.AddItems(impressions);
        }

        private void SendBulkImpressions()
        {
            if (_impressionsCache.HasReachedMaxSize())
            {
                Logger.Warn("Split SDK impressions queue is full. Impressions may have been dropped. Consider increasing capacity.");
            }

            var impressions = _impressionsCache.FetchAllAndClear();

            if (impressions.Count > 0)
            {
                try
                {
                    _apiClient.SendBulkImpressions(impressions);
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught updating impressions.", e);
                }
            }
        }
    }
}