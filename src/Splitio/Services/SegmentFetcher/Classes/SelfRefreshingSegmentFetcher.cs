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
using System.Threading;
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

        public SelfRefreshingSegmentFetcher(ISegmentChangeFetcher segmentChangeFetcher,
            ISegmentCache segmentsCache,
            BlockingCollection<SelfRefreshingSegment> segmentTaskQueue,
            ISplitTask addSegmentToQueueTask,
            IPeriodicTask segmentTaskWorker) : base(segmentsCache)
        {
            _segmentChangeFetcher = segmentChangeFetcher;
            _segments = new ConcurrentDictionary<string, SelfRefreshingSegment>();
            _segmentTaskQueue = segmentTaskQueue;
            _segmentTaskWorker = segmentTaskWorker;
            _addSegmentToQueueTask = addSegmentToQueueTask;
            _addSegmentToQueueTask.SetAction(AddSegmentsToQueue);
        }

        #region Public Methods
        public void Start()
        {
            _segmentTaskWorker.Start();
            _addSegmentToQueueTask.Start();
        }

        public async Task StopAsync()
        {
            await _segmentTaskWorker.StopAsync();
            await _addSegmentToQueueTask.StopAsync();
        }

        public async Task ClearAsync()
        {
            await _segmentTaskWorker.StopAsync();
            await _addSegmentToQueueTask.StopAsync();
            _segments.Clear();
            _segmentCache.Clear();
            _segmentTaskQueue.Dispose();
            _log.Debug("Segments cache disposed ...");
        }

        public override void InitializeSegment(string name)
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
        }

        public bool FetchAll()
        {
            if (_segments.Count == 0) return true;

            var fetchOptions = new FetchOptions();
            var tasks = new List<Task<bool>>();

            foreach (var segment in _segments.Values)
            {
                tasks.Add(segment.FetchSegment(fetchOptions));
            }

            Task.WaitAll(tasks.ToArray());

            return tasks.All(t => t.Result == true);
        }

        public async Task Fetch(string segmentName, FetchOptions fetchOptions)
        {
            try
            {
                InitializeSegment(segmentName);
                _segments.TryGetValue(segmentName, out SelfRefreshingSegment fetcher);
                await fetcher.FetchSegment(fetchOptions);
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
                var changeNumber = _segmentCache.GetChangeNumber(name);

                if (changeNumber == -1) await Fetch(name, new FetchOptions());
            }
        }
        #endregion

        #region Private Methods
        private void AddSegmentsToQueue()
        {
            Console.WriteLine($"\n##### {Thread.CurrentThread.ManagedThreadId} AddSegmentsToQueue Task");

            foreach (var segment in _segments.Values)
            {
                _segmentTaskQueue.TryAdd(segment);

                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"Segment queued: {segment.Name}");
                }
            }

            Console.WriteLine($"\n##### {Thread.CurrentThread.ManagedThreadId} finished AddSegmentsToQueue Task");
        }
        #endregion
    }
}
