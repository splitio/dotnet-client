using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentFetcher : ISegmentFetcher
    {
        protected readonly ISegmentCache _segmentCache;

        public SegmentFetcher(ISegmentCache segmentCache)
        {
            _segmentCache = segmentCache;
        }

        public virtual Task InitializeSegmentAsync(string name)
        {
            return Task.FromResult(0); // No-op
        }
    }
}
