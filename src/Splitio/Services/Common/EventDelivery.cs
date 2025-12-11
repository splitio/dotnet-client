using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.Common
{
    public class EventDelivery : IEventDelivery
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");

        public virtual void Deliver(SdkEvent sdkEvent, EventMetadata eventMetadata, EventHandler<EventMetadata> handler)
        {
            if (handler != null)
            {
                _logger.Debug($"EventManager: Triggering handle for Sdk Event {sdkEvent}");
                try
                {
                    handler(this, eventMetadata);
                }
                catch (Exception e)
                {
                    _logger.Error($"EventManager: Failed to run event {sdkEvent} handler {e.Message}", e);
                }
            }
        }
    }
}
