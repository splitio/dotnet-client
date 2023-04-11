using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCache
    {
        Task AddToSegmentAsync(string segmentName, List<string> segmentKeys);
        Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys);
        Task<bool> IsInSegmentAsync(string segmentName, string key);
        Task SetChangeNumberAsync(string segmentName, long changeNumber);
        Task<long> GetChangeNumberAsync(string segmentName);
        Task<List<string>> GetSegmentNamesAsync();
        Task<List<string>> GetSegmentKeysAsync(string segmentName);
        Task<int> SegmentsCountAsync();
        void Clear();
        int SegmentKeysCount();
    }
}
