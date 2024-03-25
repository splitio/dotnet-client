using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface INotificationProcessor
    {
        Task Proccess(IncomingNotification notification);
    }
}
