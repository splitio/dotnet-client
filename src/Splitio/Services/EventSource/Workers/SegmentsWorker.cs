using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public class SegmentsWorker : BaseWorker, ISegmentsWorker
    {
        private readonly ISynchronizer _synchronizer;
        private readonly BlockingCollection<SegmentQueueDto> _queue;

        public SegmentsWorker(ISynchronizer synchronizer, ISplitTask task) : base("SegmentsWorker", WrapperAdapter.Instance().GetLogger(typeof(SegmentsWorker)), task)
        {
            _synchronizer = synchronizer;
            _queue = new BlockingCollection<SegmentQueueDto>(new ConcurrentQueue<SegmentQueueDto>());
        }

        #region Public Methods
        public void AddToQueue(long changeNumber, string segmentName)
        {
            try
            {
                if (!_task.IsRunning())
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
        #endregion

        #region Protected Methods
        protected override async Task ExecuteAsync()
        {
            try
            {
                if (_queue.TryTake(out SegmentQueueDto segment, -1, _cts.Token))
                {
                    _log.Debug($"Segment dequeue: {segment.SegmentName}");
                    await _synchronizer.SynchronizeSegmentAsync(segment.SegmentName, segment.ChangeNumber);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _log.Debug($"Segments worker Execute exception", ex);
            }
        }
        #endregion
    }
}
