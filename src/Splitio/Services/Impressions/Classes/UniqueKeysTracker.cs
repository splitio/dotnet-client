using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public class UniqueKeysTracker : IUniqueKeysTracker
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(UniqueKeysTracker));
        private static readonly int IntervalToClearLongTermCache = 3600000;        

        private readonly IFilterAdapter _filterAdapter;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;
        private readonly IUniqueKeysSenderAdapter _senderAdapter;
        private readonly ITasksManager _tasksManager;
        private readonly int _interval;
        private readonly int _cacheMaxSize;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();
        private readonly object _taskLock = new object();

        private bool _running = false;

        public UniqueKeysTracker(TrackerConfig config,
            IFilterAdapter filterAdapter,
            IUniqueKeysSenderAdapter senderAdapter,
            ITasksManager tasksManager)
        {
            _interval = config.PeriodicTaskIntervalSeconds;
            _cache = config.Cache;
            _cacheMaxSize = config.CacheMaxSize;
            _filterAdapter = filterAdapter;
            _senderAdapter = senderAdapter;
            _tasksManager = tasksManager;
            
        }

        #region Public Methods
        public void Start()
        {
            lock (_taskLock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.StartPeriodic(() => SendBulkUniques(), _interval * 1000, _cancellationTokenSource, "MTKs sender.");
                _tasksManager.StartPeriodic(() => _filterAdapter.Clear(), IntervalToClearLongTermCache, _cancellationTokenSource, "Cache Long Term clear.");
            }
        }

        public void Stop()
        {
            lock (_taskLock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkUniques();
            }
        }

        public bool Track(string key, string featureName)
        {
            if (_filterAdapter.Contains(featureName, key)) return false;

            _filterAdapter.Add(featureName, key);

            _cache.AddOrUpdate(featureName, new HashSet<string>() { key }, (_, hashSet) =>
            {
                hashSet.Add(key);
                return hashSet;
            });

            if (_cache.Count >= _cacheMaxSize)
            {
                SendBulkUniques();
            }

            return true;
        }
        #endregion

        #region Private Methods
        private void SendBulkUniques()
        {
            lock (_lock)
            {
                var uniques = new ConcurrentDictionary<string, HashSet<string>>(_cache);

                _cache.Clear();

                if (!uniques.Any()) return;

                try
                {
                    _senderAdapter.RecordUniqueKeys(uniques);
                }
                catch (Exception e)
                {
                    _logger.Error("Exception caught sending Unique Keys.", e);
                }
            }
        }
        #endregion
    }

    public class TrackerConfig
    {
        public int PeriodicTaskIntervalSeconds { get; set; }
        public ConcurrentDictionary<string, HashSet<string>> Cache { get; set; }
        public int CacheMaxSize { get; set; }
    }
}
