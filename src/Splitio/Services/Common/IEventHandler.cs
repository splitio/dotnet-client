
namespace Splitio.Services.Common
{
    public interface IEventHandler
    {
        void SubscribeInternalEvents();
        void ClearInternalEventsSubscription();
    }
}
