using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface INotificationProcessor
    {
        Task ProccessAsync(IncomingNotification notification);
    }
}
