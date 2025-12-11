using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventDelivery
    {
        void Deliver(SdkEvent sdkEvent, EventMetadata eventMetadata, EventHandler<EventMetadata> handler);
    }
}
