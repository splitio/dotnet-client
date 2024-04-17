using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public class SegmentsWorker : BaseWorker, ISegmentsWorker, IQueueObserver
    {
        private readonly ISynchronizer _synchronizer;
        private readonly SplitQueue<SegmentQueueDto> _queue;

        public SegmentsWorker(ISynchronizer synchronizer) : base("SegmentsWorker", WrapperAdapter.Instance().GetLogger(typeof(SegmentsWorker)))
        {
            _synchronizer = synchronizer;
            _queue = new SplitQueue<SegmentQueueDto>();
            _queue.AddObserver(this);
        }

        #region Public Methods
        public async Task AddToQueue(long changeNumber, string segmentName)
        {
            try
            {
                if (!_running)
                {
                    _log.Error("Segments Worker not running.");
                    return;
                }

                _log.Debug($"Add to queue: {segmentName} - {changeNumber}");
                await _queue.EnqueueAsync(new SegmentQueueDto { ChangeNumber = changeNumber, SegmentName = segmentName });
            }
            catch (Exception ex)
            {
                _log.Error($"AddToQueue: {ex.Message}");
            }
        }

        public async Task Notify()
        {
            try
            {
                if (!_queue.TryDequeue(out SegmentQueueDto segment)) return;

                _log.Debug($"Segment dequeue: {segment.SegmentName}");
                await _synchronizer.SynchronizeSegmentAsync(segment.SegmentName, segment.ChangeNumber);
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
