using Splitio.Domain;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Common
{
    public interface IEventsManager<E, I, M>
    {
        void NotifyInternalEvent(I sdkInternalEvent, M eventMetadata, List<E> eventsToNotify);
        void Register(E sdkEvent, EventHandler<M> handler);
        void Unregister(E sdkEvent);
    }
}
