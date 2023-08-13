using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
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
        private readonly ITasksManager _tasksManager;
        private readonly IStatusManager _statusManager;

        private int _counter;
        private bool _running;
        private CancellationTokenSource _cancellationTokenSource;

        public SegmentTaskWorker(int numberOfParallelTasks,
            BlockingCollection<SelfRefreshingSegment> segmentTaskQueue,
            ITasksManager tasksManager,
            IStatusManager statusManager)
        {
            _numberOfParallelTasks = numberOfParallelTasks;            
            _segmentTaskQueue = segmentTaskQueue;
            _counter = 0;
            _tasksManager = tasksManager;
            _statusManager = statusManager;
        }

        public void Start()
        {
            if (_running || _statusManager.IsDestroyed()) return;

            _running = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _tasksManager.Start(() => ExecuteTasks(), _cancellationTokenSource, "SegmentsWorker");
        }

        public void Stop()
        {
            if (!_running) return;

            _running = false;
            _cancellationTokenSource.Cancel();
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
            Console.WriteLine($"##### {Thread.CurrentThread.ManagedThreadId} SegmentTaskWorker Task");

            while (_running && !_cancellationTokenSource.IsCancellationRequested)
            {
                if (_counter < _numberOfParallelTasks)
                {
                    try
                    {
                        //Wait indefinitely until a segment is queued
                        if (_segmentTaskQueue.TryTake(out SelfRefreshingSegment segment, -1, _cancellationTokenSource.Token))
                        {
                            Console.WriteLine($"########## {Thread.CurrentThread.ManagedThreadId} SegmentTaskWorker dequeued: {segment.Name}");

                            if (_log.IsDebugEnabled)
                            {
                                _log.Debug($"Segment dequeued: {segment.Name}");
                            }

                            if (!_cancellationTokenSource.IsCancellationRequested)
                            {
                                IncrementCounter();
                                var task = new Task(async () => await segment.FetchSegment(new FetchOptions
                                {
                                    Token = _cancellationTokenSource.Token
                                }), _cancellationTokenSource.Token);
                                task.ContinueWith((x) => DecrementCounter(), _cancellationTokenSource.Token);
                                task.ContinueWith((x) => Console.WriteLine($" {Thread.CurrentThread.ManagedThreadId} FetchSegment TASK FINISHED"), _cancellationTokenSource.Token);
                                task.Start();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is ObjectDisposedException || ex is OperationCanceledException) return;

                        _log.Debug($"SegmentTaskWorker Exception", ex);
                    }
                }
                else
                {
                    _waitForExecution.WaitOne();
                }
            }

            Console.WriteLine($"\n\n##### {Thread.CurrentThread.ManagedThreadId} FINISHED SegmentTaskWorker Task\n\n");
        }
    }
}
