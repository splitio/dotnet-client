using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public interface ISplitsWorker : IWorker
    {
        Task AddToQueue(InstantUpdateNotification notification);
        void Kill(SplitKillNotification skn);
    }
}
