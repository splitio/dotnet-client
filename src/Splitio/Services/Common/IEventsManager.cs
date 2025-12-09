using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventsManager
    {
        bool EventAlreadyTriggered(SdkEvent sdkEvent);
        void Register(SdkEvent sdkEvent, Action<EventMetadata> callbackAction);
        void Unregister(SdkEvent sdkEvent);
        void Destroy();
    }
}
