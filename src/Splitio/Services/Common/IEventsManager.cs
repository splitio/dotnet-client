using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface IEventsManager
    {
        bool EventAlreadyTriggered(SdkEvent sdkEvent);
        void Register(SdkEvent sdkEvent, IEventHandler eventHandler);
        void Unregister(SdkEvent sdkEvent);
        void Destroy();
    }
}
