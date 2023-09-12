using System;

namespace Splitio.Services.EventSource
{
    public interface INotificationManagerKeeper
    {
        void HandleSseStatus(SSEClientStatusMessage newStatus);
        void HandleIncomingEvent(IncomingNotification notification);
    }
}
