using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading;

namespace Splitio.Services.Common
{
    public class EventDelivery<E, M> : IEventDelivery<E, M>
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventDelivery");

        public void Deliver(E sdkEvent, M eventMetadata, Action<M> callbackAction)
        {
            try
            {
                Thread eventCallbackThread = new Thread(() => callbackAction(eventMetadata));
                eventCallbackThread.Start();
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _logger.Debug($"EventDelivery worker Execute exception", ex);
            }
        }
    }
}
