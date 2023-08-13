using System;

namespace Splitio.Services.EventSource
{
    public interface IEventSourceClient
    {
        bool ConnectAsync(string url);
        void Disconnect();
        
        event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
