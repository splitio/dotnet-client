using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCacheConsumer
    {
        bool IsInSegment(string segmentName, string key);
        long GetChangeNumber(string segmentName);
        int SegmentsCount();
        int SegmentKeysCount();

        Task<bool> IsInSegmentAsync(string segmentName, string key);
    }
}
