using System;

namespace Splitio.Commons.SdkEvents
{
    public interface IEventDelivery<E, M>
    {
        void Deliver(E sdkEvent, M eventMetadata, Action<M> handler);
    }
}
