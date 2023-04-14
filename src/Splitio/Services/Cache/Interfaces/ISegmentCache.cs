using System.Collections.Generic;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISegmentCache
    {
        void AddToSegment(string segmentName, List<string> segmentKeys);
        void RemoveFromSegment(string segmentName, List<string> segmentKeys);
        List<string> GetSegmentKeys(string segmentName);
        void SetChangeNumber(string segmentName, long changeNumber);
        long GetChangeNumber(string segmentName);
        List<string> GetSegmentNames();
        int SegmentsCount();
        bool IsInSegment(string segmentName, string key);
        void Clear();
        int SegmentKeysCount();
    }
}
