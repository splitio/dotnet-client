using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public class EventDelivery : IEventDelivery
    {
        EventsManagerConfig _config;
        EventsManager _eventsManager;


        public void Deliver(SdkEvent sdkEvent, EventMetadata eventMetadata)
        { }

    }
}
