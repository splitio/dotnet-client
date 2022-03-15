﻿using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Impressions.Classes
{
    public class UniqueKeysTracker : TrackerComponent, IUniqueKeysTracker
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(UniqueKeysTracker));
        private static readonly int IntervalToClearLongTermCache = 3600000;        

        private readonly IFilterAdapter _filterAdapter;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;
        private readonly IImpressionsSenderAdapter _senderAdapter;

        public UniqueKeysTracker(ComponentConfig config,
            IFilterAdapter filterAdapter,
            ConcurrentDictionary<string, HashSet<string>> cache,
            IImpressionsSenderAdapter senderAdapter,
            ITasksManager tasksManager) : base(config, tasksManager)
        {
            _filterAdapter = filterAdapter;
            _cache = cache;
            _senderAdapter = senderAdapter;
        }

        #region Public Methods
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
                SendBulkData();
            }

            return true;
        }
        #endregion

        #region Protected Methods
        protected override void StartTask()
        {
            _tasksManager.StartPeriodic(() => SendBulkData(), _taskInterval * 1000, _cancellationTokenSource, "MTKs sender.");
            _tasksManager.StartPeriodic(() => _filterAdapter.Clear(), IntervalToClearLongTermCache, _cancellationTokenSource, "Cache Long Term clear.");
        }

        protected override void SendBulkData()
        {
            lock (_lock)
            {
                var uniques = new ConcurrentDictionary<string, HashSet<string>>(_cache);

                _cache.Clear();

                if (!uniques.Any()) return;

                try
                {
                    var values = uniques
                        .Select(v => new Mtks(v.Key, v.Value))
                        .ToList();

                    if (values.Count <= _maxBulkSize)
                    {
                        _senderAdapter.RecordUniqueKeys(values);
                        return;
                    }

                    while (values.Count > 0)
                    {
                        var bulkToPost = Util.Helper.TakeFromList(values, _maxBulkSize);

                        _senderAdapter.RecordUniqueKeys(bulkToPost);
                    }
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
