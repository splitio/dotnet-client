using System;

namespace Splitio.Commons.SdkEvents
{
    public interface IEventsManager<E, I, M>
    {
        void NotifyInternalEvent(I sdkInternalEvent, M eventMetadata);
        void Register(E sdkEvent, Action<M> handler);
        void Unregister(E sdkEvent);
        bool EventAlreadyTriggered(E sdkEvent);
    }
}
