using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsLog : IImpressionsLog
    {
        protected static readonly ISplitLogger Logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsLog));

        private readonly IImpressionsSdkApiClient _apiClient;
        private readonly IImpressionCache _impressionsCache;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITasksManager _tasksManager;
        private readonly int _interval;
        private readonly object _lock = new object();

        private bool _running;

        public ImpressionsLog(IImpressionsSdkApiClient apiClient,
            int interval,
            IImpressionCache impressionsCache,
            ITasksManager tasksManager)
        {
            _apiClient = apiClient;
            _impressionsCache = impressionsCache;
            _interval = interval;
            _cancellationTokenSource = new CancellationTokenSource();
            _tasksManager = tasksManager;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.StartPeriodic(async () => await SendBulkImpressions(), _interval * 1000, _cancellationTokenSource, "Main Impressions Log.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                var _ = SendBulkImpressions();
            }
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.AddItems(impressions);
        }

        private async Task SendBulkImpressions()
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