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
        private readonly ISplitLogger _log;
        private readonly ISynchronizer _synchronizer;
        private readonly ITasksManager _tasksManager;
        private readonly BlockingCollection<SegmentQueueDto> _queue;
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellationTokenSource;
        private bool _running;

        public SegmentsWorker(ISynchronizer synchronizer, ITasksManager tasksManager)
        {
            _synchronizer = synchronizer;
            _tasksManager = tasksManager;
            _log = WrapperAdapter.Instance().GetLogger(typeof(SegmentsWorker));
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
                        _log.Error("Segments Worker not running.");
                        return;
                    }

                    _running = false;

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    _log.Debug($"Stop: {ex.Message}");
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
                        await _synchronizer.SynchronizeSegment(segment.SegmentName, segment.ChangeNumber);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _log.Debug($"Segments worker stoped ...");
            }
            catch (Exception ex)
            {
                _log.Debug($"Segments worker Execute exception", ex);
            }
        }
        #endregion
    }
}
