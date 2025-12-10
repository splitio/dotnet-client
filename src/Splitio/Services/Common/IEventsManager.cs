using Splitio.Domain;
using System;

namespace Splitio.Services.Common
{
    public interface IEventsManager
    {
        void OnSdkInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata);
        void OnSdkEvent(SdkEvent sdkEvent, EventMetadata eventMetadata);
    }
}
