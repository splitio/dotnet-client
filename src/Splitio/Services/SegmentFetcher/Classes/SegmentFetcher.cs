using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentFetcher : ISegmentFetcher
    {
        protected readonly ISegmentCache _segmentCache;

        public SegmentFetcher(ISegmentCache segmentCache)
        {
            _segmentCache = segmentCache;
        }

        public virtual void InitializeSegment(string name) { }
    }
}
