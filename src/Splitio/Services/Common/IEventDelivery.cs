using Splitio.Domain;

namespace Splitio.Services.Common
{
    public interface IEventDelivery
    {
        void Deliver(SdkEvent sdkEvent, EventMetadata eventMetadata);
    }
}
