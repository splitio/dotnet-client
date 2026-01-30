using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public class InternalEventsTask : BaseWorker, IQueueObserver, IInternalEventsTask
    {
        private readonly IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("InternalEventsTask");
        private readonly SplitQueue<SdkEventNotification> _queue;

        public InternalEventsTask(IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager,
            SplitQueue<SdkEventNotification> internalEventsQueue) : base("InternalEventsTask", WrapperAdapter.Instance().GetLogger(typeof(InternalEventsTask)))
        {
            _eventsManager = eventsManager;
            _queue = internalEventsQueue;
            _queue.AddObserver(this);
        }

        public async Task AddToQueue(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            try
            {
                if (!_running)
                {
                    _logger.Error("InternalEventTask Worker not running.");
                    return;
                }

                _logger.Debug($"InternalEventTask: Add to queue: {sdkInternalEvent}");
                await _queue.EnqueueAsync(new SdkEventNotification(sdkInternalEvent, eventMetadata));
            }
            catch (Exception ex)
            {
                _logger.Error($"InternalEventTask error AddToQueue: {ex.Message}");
            }
        }

        public async Task Notify()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!_queue.TryDequeue(out SdkEventNotification sdkEventDto)) return;

                    _logger.Debug($"InternalEventTask: SdkEvent dequeue: {sdkEventDto.SdkInternalEvent}");
                    _eventsManager.NotifyInternalEvent(sdkEventDto.SdkInternalEvent, sdkEventDto.EventMetadata);
                });
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _logger.Debug($"InternalEventTask worker Execute exception", ex);
            }
        }
    }
}
