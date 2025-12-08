using Splitio.Domain;

namespace Splitio.Services.Common
{
    public interface IEventHandler
    {
        void Handle(SdkEvent sdkEvent, EventMetadata eventMetadata);
    }
}
