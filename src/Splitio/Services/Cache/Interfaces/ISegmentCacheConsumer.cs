using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCacheConsumer
    {
        bool IsInSegment(string segmentName, string key);
        long GetChangeNumber(string segmentName);
        List<string> GetSegmentNames();
        List<string> GetSegmentKeys(string segmentName);
        int SegmentsCount();
        int SegmentKeysCount();

        Task<bool> IsInSegmentAsync(string segmentName, string key);
        Task<long> GetChangeNumberAsync(string segmentName);
        Task<List<string>> GetSegmentNamesAsync();
        Task<List<string>> GetSegmentKeysAsync(string segmentName);
        Task<int> SegmentsCountAsync();
        Task<int> SegmentKeysCountAsync();
    }
}
