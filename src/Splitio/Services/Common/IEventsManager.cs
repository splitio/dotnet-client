using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventsManager
    {
        void NotifyInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata);
        void Register(SdkEvent sdkEvent, EventHandler<EventMetadata> handler);
        void Unregister(SdkEvent sdkEvent);
    }
}
