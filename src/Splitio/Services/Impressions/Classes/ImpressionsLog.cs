using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsLog : IImpressionsLog
    {
        private readonly ISplitLogger Logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsLog));

        private readonly IImpressionsSdkApiClient _apiClient;
        private readonly ISimpleProducerCache<KeyImpression> _impressionsCache;
        private readonly ISplitTask _task;

        public ImpressionsLog(IImpressionsSdkApiClient apiClient,
            ISimpleCache<KeyImpression> impressionsCache,
            ISplitTask task,
            int maximumNumberOfKeysToCache = -1)
        {
            _apiClient = apiClient;
            _impressionsCache = (impressionsCache as ISimpleProducerCache<KeyImpression>) ?? new InMemorySimpleCache<KeyImpression>(new BlockingQueue<KeyImpression>(maximumNumberOfKeysToCache));            
            _task = task;
            _task.SetFunction(SendBulkImpressionsAsync);
        }

        public void Start()
        {
            _task.Start();
        }

        public void Stop()
        {
            if (!_task.IsRunning())
                return;

            _task.Stop();
            SendBulkImpressionsAsync();
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.AddItems(impressions);
        }

        private async Task SendBulkImpressionsAsync()
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
                    await _apiClient.SendBulkImpressionsAsync(impressions);
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught updating impressions.", e);
                }
            }
        }
    }
}