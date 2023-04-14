using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;

namespace Splitio.Services.Events.Interfaces
{
    public interface IEventCache : ISimpleCache<WrappedEvent>
    {
        int Add(WrappedEvent wrappedEvent);
    }
}
