using Splitio.Commons.Dto;
using Splitio.Commons.Tasks;
using System.Threading.Tasks;

namespace Splitio.Commons.Cache.Interfaces
{
    public interface IEventsCache : IPeriodicTask
    {
        void Log(WrappedEvent wrappedEvent);
        Task LogAsync(WrappedEvent wrappedEvent);
    }
}
