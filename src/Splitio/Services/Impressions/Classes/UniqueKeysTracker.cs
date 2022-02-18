using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public class UniqueKeysTracker : IUniqueKeysTracker
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(UniqueKeysTracker));
        private static readonly int IntervalToClearLongTermCache = 3600000;

        private readonly IBloomFilter _bloomFilter;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;
        private readonly IImpressionsSdkApiClient _apiClient;
        private readonly ITasksManager _tasksManager;
        private readonly int _interval;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();
        private readonly object _taskLock = new object();        

        private bool _running = false;

        public UniqueKeysTracker(IBloomFilter bloomFilter,
            ConcurrentDictionary<string, HashSet<string>> cache,
            IImpressionsSdkApiClient apiClient,
            ITasksManager tasksManager,
            int intervalSeconds)
        {
            _bloomFilter = bloomFilter;
            _cache = cache;
            _apiClient = apiClient;
            _tasksManager = tasksManager;
            _interval = intervalSeconds;
        }

        #region Public Methods
        public void Start()
        {
            lock (_taskLock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.StartPeriodic(() => SendBulkMTKs(), _interval * 1000, _cancellationTokenSource, "MTKs sender.");
                _tasksManager.StartPeriodic(() => _bloomFilter.Clear(), IntervalToClearLongTermCache, _cancellationTokenSource, "Cache Long Term clear.");
            }
        }

        public void Stop()
        {
            lock (_taskLock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkMTKs();
            }
        }

        public void Track(string key, string featureName)
        {
            lock (_lock)
            {
                var filterKey = $"{featureName}:{key}";

                if (_bloomFilter.Contains(filterKey)) return;

                _bloomFilter.Add(filterKey);

                _cache.AddOrUpdate(featureName, new HashSet<string>() { key }, (_, hashSet) =>
                {
                    hashSet.Add(key);
                    return hashSet;
                });
            }
        }
        #endregion

        #region Private Methods
        private ConcurrentDictionary<string, HashSet<string>> PopAll()
        {
            lock (_lock)
            {
                var values = new ConcurrentDictionary<string, HashSet<string>>(_cache);

                _cache.Clear();

                return values;
            }
        }

        private void SendBulkMTKs()
        {
            var impressions = PopAll();

            if (impressions.Count > 0)
            {
                try
                {
                    //_apiClient.SendBulkImpressionsCount(impressions);
                }
                catch (Exception e)
                {
                    _logger.Error("Exception caught sending Unique Keys.", e);
                }
            }
        }
        #endregion
    }
}
