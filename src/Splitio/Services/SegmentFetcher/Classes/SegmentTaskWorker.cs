using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentTaskWorker : IPeriodicTask
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SegmentTaskWorker));
        
        //Worker is always one task, so when it is signaled, after the
        //task stops its wait, this variable is auto-reseted
        private readonly AutoResetEvent _waitForExecution = new AutoResetEvent(false);
        private readonly int _numberOfParallelTasks;
        private readonly BlockingCollection<SelfRefreshingSegment> _segmentTaskQueue;
        private readonly IStatusManager _statusManager;
        private readonly ISplitTask _task;

        private int _counter;
        private CancellationTokenSource _cts;

        public SegmentTaskWorker(int numberOfParallelTasks,
            BlockingCollection<SelfRefreshingSegment> segmentTaskQueue,
            IStatusManager statusManager,
            ISplitTask task)
        {
            _numberOfParallelTasks = numberOfParallelTasks;            
            _segmentTaskQueue = segmentTaskQueue;
            _counter = 0;
            _statusManager = statusManager;
            _task = task;
            _task.SetAction(ExecuteTasks);
        }

        public void Start()
        {
            if (_task.IsRunning() || _statusManager.IsDestroyed()) return;

            _cts = new CancellationTokenSource();
            _task.Start();
        }

        public void Stop()
        {
            if (!_task.IsRunning()) return;

            _cts.Cancel();
            _cts.Dispose();
            _task.Stop();
        }

        private void IncrementCounter()
        {
            Interlocked.Increment(ref _counter);
        }

        private void DecrementCounter()
        {
            Interlocked.Decrement(ref _counter);
            _waitForExecution.Set();
        }

        private void ExecuteTasks()
        {
            try
            {
                if (_counter >= _numberOfParallelTasks)
                {
                    _waitForExecution.WaitOne();
                }

                //Wait indefinitely until a segment is queued
                if (_segmentTaskQueue.TryTake(out SelfRefreshingSegment segment, -1, _cts.Token))
                {
                    if (_log.IsDebugEnabled)
                    {
                        _log.Debug($"Segment dequeued: {segment.Name}");
                    }

                    if (_cts.IsCancellationRequested) return;

                    IncrementCounter();
                    var task = new Task(async () => await segment.FetchSegmentAsync(new FetchOptions
                    {
                        Token = _cts.Token
                    }), _cts.Token);
                    task.ContinueWith((x) => DecrementCounter(), _cts.Token);
                    task.Start();
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is OperationCanceledException) return;

                _log.Debug($"SegmentTaskWorker Exception", ex);
            }
        }
    }
}
