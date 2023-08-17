using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface IEventSourceClient
    {
        bool ConnectAsync(string url);
        Task DisconnectAsync();


        event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
