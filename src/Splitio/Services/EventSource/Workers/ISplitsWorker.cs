using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public interface ISplitsWorker : IWorker
    {
        Task AddToQueue(SplitChangeNotification scn);
        void Kill(SplitKillNotification skn);
    }
}
