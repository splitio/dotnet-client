using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventDelivery<E, M>
    {
        void Deliver(E sdkEvent, M eventMetadata, Action<M> handler);
    }
}
