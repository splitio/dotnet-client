﻿using System;

namespace Splitio.Services.EventSource
{
    public interface IEventSourceClient
    {
        bool Connect(string url);
        void Disconnect();

        event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
