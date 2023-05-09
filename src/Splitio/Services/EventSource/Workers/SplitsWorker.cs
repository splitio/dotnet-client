using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio.Services.EventSource.Workers
{
    public class SplitsWorker : ISplitsWorker
    {
        private readonly ISplitLogger _log;
        private readonly ISplitCache _featureFlagCache;
        private readonly ISynchronizer _synchronizer;
        private readonly ITasksManager _tasksManager;
        private readonly BlockingCollection<long> _queue;
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellationTokenSource;
        private bool _running;

        public SplitsWorker(ISplitCache featureFlagCache,
            ISynchronizer synchronizer,
            ITasksManager tasksManager)
        {
            _featureFlagCache = featureFlagCache;
            _synchronizer = synchronizer;
            _tasksManager = tasksManager;
            _log = WrapperAdapter.Instance().GetLogger(typeof(SplitsWorker));
            _queue = new BlockingCollection<long>(new ConcurrentQueue<long>());
        }

        #region Public Methods
        public void AddToQueue(long changeNumber)
        {
            try
            {
                if (!_running)
                {
                    _log.Debug("FeatureFlags Worker not running.");
                    return;
                }

                _log.Debug($"Add to queue: {changeNumber}");
                _queue.TryAdd(changeNumber);                
            }
            catch (Exception ex)
            {
                _log.Error($"AddToQueue: {ex.Message}");
            }
        }

        public void KillSplit(long changeNumber, string splitName, string defaultTreatment)
        {
            try
            {
                if (!_running)
                {
                    _log.Debug("FeatureFlags Worker not running.");
                    return;
                }

                if (changeNumber > _featureFlagCache.GetChangeNumber())
                {
                    _log.Debug($"Kill Feature Flag: {splitName}, changeNumber: {changeNumber} and defaultTreatment: {defaultTreatment}");
                    _featureFlagCache.Kill(changeNumber, splitName, defaultTreatment);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error killing the following feature flag: {splitName}", ex);
            }
        }

        public void Start()
        {
            lock (_lock)
            {
                try
                {
                    if (_running)
                    {
                        _log.Debug("FeatureFlags Worker already running.");
                        return;
                    }

                    _log.Debug("FeatureFlags Wroker starting ...");
                    _cancellationTokenSource = new CancellationTokenSource();
                    _running = true;
                    _tasksManager.Start(() => ExecuteAsync(), _cancellationTokenSource, "FeatureFlags Worker.");                    
                }
                catch (Exception ex)
                {
                    _log.Debug($"Start: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                try
                {
                    if (!_running)
                    {
                        _log.Debug("FeatureFlags Worker not running.");
                        return;
                    }

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();

                    _log.Debug("FeatureFlags Worker stopped ...");
                    _running = false;
                }
                catch (Exception ex)
                {
                    _log.Debug($"Stop: {ex.Message}");
                }
            }
        }
        #endregion

        #region Private Mthods
        private async void ExecuteAsync()
        {
            try
            {
                _log.Debug($"FeatureFlags Worker, Token: {_cancellationTokenSource.IsCancellationRequested}; Running: {_running}.");
                while (!_cancellationTokenSource.IsCancellationRequested && _running)
                {
                    // Wait indefinitely until a segment is queued
                    if (_queue.TryTake(out long changeNumber, -1, _cancellationTokenSource.Token))
                    {
                        _log.Debug($"ChangeNumber dequeue: {changeNumber}");

                        await _synchronizer.SynchronizeSplits(changeNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug($"Execute: {ex.Message}");
            }
            finally
            {
                _log.Debug("FeatureFlags Worker execution finished.");
            }
        }
        #endregion
    }
}
