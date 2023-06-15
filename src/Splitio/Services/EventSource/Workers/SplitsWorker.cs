using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio.Services.EventSource.Workers
{
    public class SplitsWorker : ISplitsWorker
    {
        private readonly ISplitLogger _log;
        private readonly ISynchronizer _synchronizer;
        private readonly ITasksManager _tasksManager;
        private readonly ISplitCache _featureFlagCache;
        private readonly ISplitParser _featureFlagParser;
        private readonly BlockingCollection<SplitChangeNotification> _queue;
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellationTokenSource;
        private bool _running;

        public SplitsWorker(ISynchronizer synchronizer,
            ITasksManager tasksManager,
            ISplitCache featureFlagCache,
            ISplitParser featureFlagParser,
            BlockingCollection<SplitChangeNotification>  queue)
        {
            _synchronizer = synchronizer;
            _tasksManager = tasksManager;
            _featureFlagCache = featureFlagCache;
            _featureFlagParser = featureFlagParser;
            _queue = queue;
            _log = WrapperAdapter.Instance().GetLogger(typeof(SplitsWorker));
        }

        #region Public Methods
        public void AddToQueue(SplitChangeNotification scn)
        {
            try
            {
                _log.Debug($"Add to queue: {scn.ChangeNumber}");
                _queue.TryAdd(scn);
            }
            catch (Exception ex)
            {
                _log.Error($"AddToQueue: {ex.Message}");
            }
        }

        public void Kill(SplitKillNotification skn)
        {
            try
            {
                if (skn.ChangeNumber > _featureFlagCache.GetChangeNumber())
                {
                    _log.Debug($"Kill Feature Flag: {skn.SplitName}, changeNumber: {skn.ChangeNumber} and defaultTreatment: {skn.DefaultTreatment}");
                    _featureFlagCache.Kill(skn.ChangeNumber, skn.SplitName, skn.DefaultTreatment);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error killing the following feature flag: {skn.SplitName}", ex);
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

        #region Private Methods
        private async void ExecuteAsync()
        {
            try
            {
                _log.Debug($"FeatureFlags Worker, Token: {_cancellationTokenSource.IsCancellationRequested}; Running: {_running}.");
                while (!_cancellationTokenSource.IsCancellationRequested && _running)
                {
                    // Wait indefinitely until a segment is queued
                    if (_queue.TryTake(out SplitChangeNotification scn, -1, _cancellationTokenSource.Token))
                    {
                        _log.Debug($"ChangeNumber dequeue: {scn.ChangeNumber}");

                        var success = ProcessSplitChangeNotification(scn);

                        if (!success)
                            await _synchronizer.SynchronizeSplits(scn.ChangeNumber);
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

        private bool ProcessSplitChangeNotification(SplitChangeNotification scn)
        {
            try
            {
                if (_featureFlagCache.GetChangeNumber() >= scn.ChangeNumber) return true;

                if (scn.FeatureFlag != null && _featureFlagCache.GetChangeNumber() == scn.PreviousChangeNumber)
                {
                    var ffParsed = _featureFlagParser.Parse(scn.FeatureFlag);

                    // if ffParsed is null it means that the status is ARCHIVED or different to ACTIVE.
                    if (ffParsed == null)
                    {
                        _featureFlagCache.RemoveSplit(scn.FeatureFlag.name);
                    }
                    else
                    {
                        _featureFlagCache.AddOrUpdate(scn.FeatureFlag.name, ffParsed);
                    }

                    _featureFlagCache.SetChangeNumber(scn.ChangeNumber);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong processing a Feature Flag notification", ex);
            }

            return false;
        }
        #endregion
    }
}
