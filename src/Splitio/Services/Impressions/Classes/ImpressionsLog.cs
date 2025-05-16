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
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsLog));

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
            _task.OnStop(SendBulkImpressionsAsync);
        }

        public void Start()
        {
            _task.Start();
        }

        public async Task StopAsync()
        {
            await _task.StopAsync();
        }

        public int Log(IList<KeyImpression> impressions)
        {
            _log.Debug($"Adding impressions: {impressions.Count}");

            return _impressionsCache.AddItems(impressions);
        }

        public Task<int> LogAsync(IList<KeyImpression> impressions)
        {
            return Task.FromResult(Log(impressions));
        }

        private async Task SendBulkImpressionsAsync()
        {
            try
            {
                if (_impressionsCache.HasReachedMaxSize())
                {
                    _log.Warn("Split SDK impressions queue is full. Impressions may have been dropped. Consider increasing capacity.");
                }

                var impressions = _impressionsCache.FetchAllAndClear();

                _log.Debug($"Impressions to post: {impressions.Count}");

                if (impressions.Count <= 0) return;

                await _apiClient.SendBulkImpressionsAsync(impressions);                
            }
            catch (Exception ex)
            {
                _log.Debug($"Somenthing went wrong posting impressions.", ex);
            }
        }
    }
}