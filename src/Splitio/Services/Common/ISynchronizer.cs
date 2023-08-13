using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISynchronizer
    {
        Task<bool> SyncAll(CancellationTokenSource cancellationTokenSource, bool asynchronous = true);
        Task SynchronizeSplits(long targetChangeNumber);
        Task SynchronizeSegment(string segmentName, long targetChangeNumber);
        void StartPeriodicFetching();
        void StopPeriodicFetching();
        void StartPeriodicDataRecording();
        void StopPeriodicDataRecording();
        void ClearFetchersCache();
    }
}
