using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading;

namespace Splitio.Services.Common
{
    public class EventDelivery<E, M> : IEventDelivery<E, M>
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventDelivery");

        public void Deliver(E sdkEvent, M eventMetadata, Action<M> handler)
        {
            try
            {
                object[] parameters = new object[] { handler, eventMetadata };
                ThreadPool.QueueUserWorkItem(RunCallbackAction, parameters);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _logger.Debug($"EventDelivery worker Execute exception", ex);
            }
        }

        private void RunCallbackAction(object state)
        {
            try
            {
                if (state is object[] parameters)
                {
                    Action<M> callbackAction = (Action<M>)parameters[0];
                    M eventMetadata = (M)parameters[1];
                    callbackAction(eventMetadata);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _logger.Debug($"Exception in callback", ex);
            }
        }
    }
}
