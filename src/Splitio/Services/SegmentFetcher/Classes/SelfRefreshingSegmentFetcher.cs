using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SelfRefreshingSegmentFetcher : SegmentFetcher, ISelfRefreshingSegmentFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSegmentFetcher));

        private readonly ISegmentChangeFetcher _segmentChangeFetcher;
        private readonly BlockingCollection<SelfRefreshingSegment> _segmentTaskQueue;
        private readonly ISplitTask _addSegmentToQueueTask;
        private readonly ConcurrentDictionary<string, SelfRefreshingSegment> _segments;
        private readonly IPeriodicTask _segmentTaskWorker;
        private readonly IStatusManager _statusManager;

        public SelfRefreshingSegmentFetcher(ISegmentChangeFetcher segmentChangeFetcher,
            ISegmentCache segmentsCache,
            BlockingCollection<SelfRefreshingSegment> segmentTaskQueue,
            ISplitTask addSegmentToQueueTask,
            IPeriodicTask segmentTaskWorker,
            IStatusManager statusManager) : base(segmentsCache)
        {
            _segmentChangeFetcher = segmentChangeFetcher;
            _segments = new ConcurrentDictionary<string, SelfRefreshingSegment>();
            _segmentTaskQueue = segmentTaskQueue;
            _segmentTaskWorker = segmentTaskWorker;
            _statusManager = statusManager;
            _addSegmentToQueueTask = addSegmentToQueueTask;
            _addSegmentToQueueTask.SetAction(AddSegmentsToQueue);
        }

        #region Public Methods
        public void Start()
        {
            _segmentTaskWorker.Start();
            _addSegmentToQueueTask.Start();
        }

        public void Stop()
        {
            _segmentTaskWorker.Stop();
            _addSegmentToQueueTask.Stop();
        }

        public void Clear()
        {
            _segmentTaskWorker.Stop();
            _addSegmentToQueueTask.Stop();
            _segments.Clear();
            _segmentCache.Clear();
            _segmentTaskQueue.Dispose();
            _log.Debug("Segments cache disposed ...");
        }

        public override void InitializeSegment(string name)
        {
            if (_statusManager.IsDestroyed() || _segments.TryGetValue(name, out _)) return;

            var segment = new SelfRefreshingSegment(name, _segmentChangeFetcher, _segmentCache, _statusManager);

            if (!_segments.TryAdd(name, segment)) return;
            
            _segmentTaskQueue.Add(segment);

            if (_log.IsDebugEnabled)
            {
                _log.Debug($"Segment queued: {segment.Name}");
            }
        }

        public async Task<bool> FetchAllAsync()
        {
            if (_segments.IsEmpty) return true;

            var fetchOptions = new FetchOptions();
            var tasks = new List<Task<bool>>();

            foreach (var segment in _segments.Values)
            {
                tasks.Add(segment.FetchSegmentAsync(fetchOptions));
            }

            var result = await Task.WhenAll(tasks.ToArray());

            return !result.Contains(false);
        }

        public async Task FetchAsync(string segmentName, FetchOptions fetchOptions)
        {
            try
            {
                InitializeSegment(segmentName);
                _segments.TryGetValue(segmentName, out SelfRefreshingSegment fetcher);
                await fetcher.FetchSegmentAsync(fetchOptions);
            }
            catch (Exception ex)
            {
                _log.Error($"Segment {segmentName} is not initialized. {ex.Message}");
            }
        }


        public async Task FetchSegmentsIfNotExistsAsync(IList<string> names)
        {
            if (names.Count == 0) return;

            var uniqueNames = names.Distinct().ToList();

            foreach (var name in uniqueNames)
            {
                var changeNumber = _segmentCache.GetChangeNumber(name);

                if (changeNumber == -1) await FetchAsync(name, new FetchOptions());
            }
        }
        #endregion

        #region Private Methods
        private void AddSegmentsToQueue()
        {
            foreach (var segment in _segments.Values)
            {
                _segmentTaskQueue.TryAdd(segment);

                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"Segment queued: {segment.Name}");
                }
            }
        }
        #endregion
    }
}
