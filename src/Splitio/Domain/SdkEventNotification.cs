using Splitio.Domain;

namespace Splitio.Services.EventSource.Workers
{
    public class SdkEventNotification
    {
        public SdkInternalEvent SdkInternalEvent { get; set; }
        public EventMetadata EventMetadata { get; set; }

        public SdkEventNotification(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            SdkInternalEvent = sdkInternalEvent;
            EventMetadata = eventMetadata;
        }
    }
}
