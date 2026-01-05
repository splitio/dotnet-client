using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.Common
{
    public class EventDelivery<E, M> : IEventDelivery<E, M>
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventDelivery");

        public virtual void Deliver(E sdkEvent, M eventMetadata, Action<M> handler)
        {
            if (handler != null)
            {
                try
                {
                    handler.Invoke(eventMetadata);
                }
                catch (Exception e)
                {
                    _logger.Error($"EventDelivery: Failed to run event {sdkEvent} handler {e.Message}", e);
                }
            }
        }
    }
}
