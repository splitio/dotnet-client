using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;

namespace Splitio.Services.Events.Interfaces
{
    public interface IEventsLog : IPeriodicTask
    {
        void Log(WrappedEvent wrappedEvent);
    }
}
