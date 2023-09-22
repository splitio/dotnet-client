using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.Events.Interfaces
{
    public interface IEventsLog : IPeriodicTask
    {
        void Log(WrappedEvent wrappedEvent);
        Task LogAsync(WrappedEvent wrappedEvent);
    }
}
