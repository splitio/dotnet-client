using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCacheProducer
    {
        void AddToSegment(string segmentName, List<string> segmentKeys);
        void RemoveFromSegment(string segmentName, List<string> segmentKeys);
        void SetChangeNumber(string segmentName, long changeNumber);
        void Clear();

        Task AddToSegmentAsync(string segmentName, List<string> segmentKeys);
        Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys);
        Task SetChangeNumberAsync(string segmentName, long changeNumber);
        Task ClearAsync();
    }
}
