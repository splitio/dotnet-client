using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SelfRefreshingSegmentFetcher : SegmentFetcher, ISelfRefreshingSegmentFetcher
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSegmentFetcher));

        private readonly ISegmentChangeFetcher _segmentChangeFetcher;
        private readonly IStatusManager _statusManager;
        private readonly IWrapperAdapter _wrappedAdapter;
        private readonly ISegmentTaskQueue _segmentTaskQueue;
        private readonly ITasksManager _tasksManager;
        private readonly ConcurrentDictionary<string, SelfRefreshingSegment> _segments;
        private readonly SegmentTaskWorker _worker;
        private readonly int _interval;
        private readonly object _lock = new object();

        private CancellationTokenSource _cancelTokenSource;
        private bool _running;

        public SelfRefreshingSegmentFetcher(ISegmentChangeFetcher segmentChangeFetcher, 
            IStatusManager statusManager, 
            int interval, 
            ISegmentCache segmentsCache, 
            int numberOfParallelSegments,
            ISegmentTaskQueue segmentTaskQueue,
            ITasksManager tasksManager,
            IWrapperAdapter wrapperAdapter) : base(segmentsCache)
        {
            _segmentChangeFetcher = segmentChangeFetcher;
            _segments = new ConcurrentDictionary<string, SelfRefreshingSegment>();
            _worker = new SegmentTaskWorker(numberOfParallelSegments, segmentTaskQueue);
            _interval = interval;
            _statusManager = statusManager;
            _wrappedAdapter = wrapperAdapter;
            _segmentTaskQueue = segmentTaskQueue;
            _tasksManager = tasksManager;
        }

        #region Public Methods
        public void Start()
        {
            lock (_lock)
            {
                if (_running || _statusManager.IsDestroyed()) return;

                _running = true;
                _cancelTokenSource = new CancellationTokenSource();

                _tasksManager.Start(() =>
                {
                    //Delay first execution until expected time has passed
                    var intervalInMilliseconds = _interval * 1000;
                    _wrappedAdapter.TaskDelay(intervalInMilliseconds).Wait();

                    if (_running)
                    {
                        _tasksManager.Start(() => _worker.ExecuteTasks(_cancelTokenSource.Token), _cancelTokenSource, "Segments Fetcher Worker.");
                        _tasksManager.StartPeriodic(() => AddSegmentsToQueue(), intervalInMilliseconds, _cancelTokenSource, "Segmennnnts Fetcher Add to Queue.");
                    }
                }, _cancelTokenSource, "Main Segments Fetcher.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancelTokenSource.Cancel();
                _cancelTokenSource.Dispose();
            }
        }

        public void Clear()
        {
            _segmentTaskQueue.Dispose();
            _segments.Clear();
            _segmentCache.Clear();
        }

        public override Task InitializeSegmentAsync(string name)
        {
            _segments.TryGetValue(name, out SelfRefreshingSegment segment);

            if (segment == null)
            {
                segment = new SelfRefreshingSegment(name, _segmentChangeFetcher, _segmentCache);

                if (_segments.TryAdd(name, segment))
                {
                    _segmentTaskQueue.Add(segment);

                    if (_log.IsDebugEnabled)
                    {
                        _log.Debug($"Segment queued: {segment.Name}");
                    }
                }                
            }

            return Task.FromResult(0);
        }

        public bool FetchAll()
        {
            if (_segments.Count == 0) return true;

            var fetchOptions = new FetchOptions();
            var tasks = new List<Task<bool>>();

            foreach (var segment in _segments.Values)
            {
                tasks.Add(segment.FetchSegmentAsync(fetchOptions));
            }

            Task.WaitAll(tasks.ToArray());

            return tasks.All(t => t.Result == true);
        }

        public async Task Fetch(string segmentName, FetchOptions fetchOptions)
        {
            try
            {
                await InitializeSegmentAsync(segmentName);
                _segments.TryGetValue(segmentName, out SelfRefreshingSegment fetcher);
                await fetcher.FetchSegmentAsync(fetchOptions);
            }
            catch (Exception ex)
            {
                _log.Error($"Segment {segmentName} is not initialized. {ex.Message}");
            }
        }


        public async Task FetchSegmentsIfNotExists(IList<string> names)
        {
            if (names.Count == 0) return;

            var uniqueNames = names.Distinct().ToList();

            foreach (var name in uniqueNames)
            {
                var changeNumber = await _segmentCache.GetChangeNumberAsync(name);

                if (changeNumber == -1) await Fetch(name, new FetchOptions());
            }
        }
        #endregion

        #region Private Methods
        private void AddSegmentsToQueue()
        {
            foreach (var segment in _segments.Values)
            {
                _segmentTaskQueue.Add(segment);

                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"Segment queued: {segment.Name}");
                }
            }
        }
        #endregion
    }
}
