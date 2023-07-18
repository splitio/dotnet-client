using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISynchronizer
    {
        Task<bool> SyncAllAsync();
        Task SynchronizeSplits(long targetChangeNumber);
        Task SynchronizeSegment(string segmentName, long targetChangeNumber);
        void StartPeriodicFetching();
        void StopPeriodicFetching();
        void StartPeriodicDataRecording();
        void StopPeriodicDataRecording();
        void ClearFetchersCache();
    }
}
