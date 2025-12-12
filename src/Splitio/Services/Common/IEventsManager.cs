using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventsManager<E, I>
    {
        void NotifyInternalEvent(I sdkInternalEvent, EventMetadata eventMetadata);
        void Register(E sdkEvent, EventHandler<EventMetadata> handler);
        void Unregister(E sdkEvent);
    }
}
