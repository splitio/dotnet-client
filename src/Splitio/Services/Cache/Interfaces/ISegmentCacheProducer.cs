using System.Collections.Generic;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCacheProducer
    {
        void AddToSegment(string segmentName, List<string> segmentKeys);
        void RemoveFromSegment(string segmentName, List<string> segmentKeys);
        void SetChangeNumber(string segmentName, long changeNumber);
        void Clear();
    }
}
