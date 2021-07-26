﻿using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio.Services.EventSource.Workers
{
    public class SegmentsWorker : ISegmentsWorker
    {
        private readonly static int MaxRetriesAllowed = 10;

        private readonly ISplitLogger _log;
        private readonly ISegmentCache _segmentCache;
        private readonly ISynchronizer _synchronizer;
        private readonly ITasksManager _tasksManager;
        private readonly BlockingCollection<SegmentQueueDto> _queue;
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellationTokenSource;
        private bool _running;

        public SegmentsWorker(ISegmentCache segmentCache,
            ISynchronizer synchronizer,
            ITasksManager tasksManager,
            ISplitLogger log = null)
        {
            _segmentCache = segmentCache;
            _synchronizer = synchronizer;
            _tasksManager = tasksManager;
            _log = log ?? WrapperAdapter.GetLogger(typeof(SegmentsWorker));
            _queue = new BlockingCollection<SegmentQueueDto>(new ConcurrentQueue<SegmentQueueDto>());
        }

        #region Public Methods
        public void AddToQueue(long changeNumber, string segmentName)
        {
            try
            {
                if (!_running)
                {
                    _log.Error("Segments Worker not running.");
                    return;
                }

                _log.Debug($"Add to queue: {segmentName} - {changeNumber}");
                _queue.TryAdd(new SegmentQueueDto { ChangeNumber = changeNumber, SegmentName = segmentName });
            }
            catch (Exception ex)
            {
                _log.Error($"AddToQueue: {ex.Message}");
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
                        _log.Error("Segments Worker already running.");
                        return;
                    }

                    _log.Debug($"Segments worker starting ...");
                    _cancellationTokenSource = new CancellationTokenSource();
                    _running = true;
                    _tasksManager.Start(() => Execute(), _cancellationTokenSource, "Segments Workers.");
                }
                catch (Exception ex)
                {
                    _log.Error($"Start: {ex.Message}");
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
                        _log.Error("Segments Worker not running.");
                        return;
                    }

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();

                    _log.Debug($"Segments worker stoped ...");
                    _running = false;
                }
                catch (Exception ex)
                {
                    _log.Error($"Stop: {ex.Message}");
                }
            }
        }
        #endregion

        #region Private Methods
        public async void Execute()
        {
            try
            {
                _log.Debug($"Segments Worker, Token: {_cancellationTokenSource.IsCancellationRequested}; Running: {_running}.");
                while (!_cancellationTokenSource.IsCancellationRequested && _running)
                {
                    // Wait indefinitely until a segment is queued
                    if (_queue.TryTake(out SegmentQueueDto segment, -1, _cancellationTokenSource.Token))
                    {
                        _log.Debug($"Segment dequeue: {segment.SegmentName}");

                        var attempt = 0;

                        while (segment.ChangeNumber > _segmentCache.GetChangeNumber(segment.SegmentName) && (attempt < MaxRetriesAllowed))
                        {
                            await _synchronizer.SynchronizeSegment(segment.SegmentName);
                            attempt++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Execute: {ex.Message}");
            }
            finally
            {
                _log.Debug("Segments Workers excecute finished.");
            }
        }
        #endregion
    }
}
