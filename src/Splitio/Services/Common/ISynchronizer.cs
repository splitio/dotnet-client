using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISynchronizer
    {
        Task<bool> SyncAllAsync();
        Task SynchronizeSplitsAsync(long targetChangeNumber);
        Task SynchronizeSegmentAsync(string segmentName, long targetChangeNumber);
        void StartPeriodicFetching();
        void StopPeriodicFetching();
        void StartPeriodicDataRecording();
        void StopPeriodicDataRecording();
        void ClearFetchersCache();
    }
}
