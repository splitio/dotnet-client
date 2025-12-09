using Splitio.Domain;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class EventDelivery : BaseWorker, IEventDelivery, IQueueObserver
    {
        EventsManager _eventsManager;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventDelivery");
        private readonly SplitQueue<QueuedSdkEventDto> _queue;

        public EventDelivery(EventsManager eventsManager) : base("EventDelivery", WrapperAdapter.Instance().GetLogger(typeof(EventDelivery)))
        {
            _eventsManager = eventsManager;
            _queue = new SplitQueue<QueuedSdkEventDto>();
            _queue.AddObserver(this);
        }

        public void Deliver(SdkEvent sdkEvent, EventMetadata eventMetadata)
        {
            Task.Run(() => AddToQueue(sdkEvent, eventMetadata));
        }

        public async Task AddToQueue(SdkEvent sdkEvent, EventMetadata eventMetadata)
        {
            try
            {
                if (!_running)
                {
                    _logger.Error("EventDelivery Worker not running.");
                    return;
                }

                _logger.Debug($"EventDelivery: Add to queue: {sdkEvent}");
                await _queue.EnqueueAsync(new QueuedSdkEventDto { SdkEvent = sdkEvent, EventMetadata = eventMetadata });
            }
            catch (Exception ex)
            {
                _logger.Error($"EventDelivery error AddToQueue: {ex.Message}");
            }
        }

        public async Task Notify()
        {
            try
            {
                if (!_queue.TryDequeue(out QueuedSdkEventDto sdkEventDto)) return;

                _logger.Debug($"EventDelivery: SdkEvent dequeue: {sdkEventDto.SdkEvent}");
                Action<EventMetadata> callbackAction = _eventsManager.GetCallbackAction(sdkEventDto.SdkEvent);
                if (callbackAction != null)
                {
                    _eventsManager.SetSdkEventTriggered(sdkEventDto.SdkEvent);
                    _logger.Debug($"EventDelivery: executing callback notification for Event {sdkEventDto.SdkEvent}");
                    callbackAction(sdkEventDto.EventMetadata);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _logger.Debug($"EventDelivery worker Execute exception", ex);
            }
        }
    }
}
