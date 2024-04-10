using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface IEventSourceClient
    {
        void Connect(string url);
        Task DisconnectAsync();

        event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
