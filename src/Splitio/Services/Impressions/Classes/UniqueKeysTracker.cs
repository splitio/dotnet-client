using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
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

        private readonly IFilterAdapter _filterAdapter;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;
        private readonly ITelemetryAPI _telemetryApi;
        private readonly ITasksManager _tasksManager;
        private readonly int _interval;
        private readonly int _cacheMaxSize;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();
        private readonly object _taskLock = new object();

        private bool _running = false;

        public UniqueKeysTracker(IFilterAdapter filterAdapter,
            ConcurrentDictionary<string, HashSet<string>> cache,
            int cacheMaxSize,
            ITelemetryAPI telemetryApi,
            ITasksManager tasksManager,
            int intervalSeconds)
        {
            _filterAdapter = filterAdapter;
            _cache = cache;
            _cacheMaxSize = cacheMaxSize;
            _telemetryApi = telemetryApi;
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
                SendBulkMTKs();
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
                _tasksManager.Start(() => SendBulkMTKs(), new CancellationTokenSource(), "Uniques cache reached max size.");
            }

            return true;
        }
        #endregion

        #region Private Methods
        private void SendBulkMTKs()
        {
            lock (_lock)
            {
                var uniques = PopAll();

                if (uniques.Count > 0)
                {
                    try
                    {
                        //_telemetryApi.RecordUniqueKeys(new UniqueKeys(uniques));
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Exception caught sending Unique Keys.", e);
                    }
                }
            }
        }

        private ConcurrentDictionary<string, HashSet<string>> PopAll()
        {
            var values = new ConcurrentDictionary<string, HashSet<string>>(_cache);

            _cache.Clear();

            return values;
        }
        #endregion
    }
}
