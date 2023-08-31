using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface IEventSourceClient
    {
        bool Connect(string url);
        Task DisconnectAsync();

        event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
