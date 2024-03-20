using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface INotificationManagerKeeper
    {
        Task HandleSseStatus(SSEClientStatusMessage newStatus);
        Task HandleIncomingEvent(IncomingNotification notification);
    }
}
