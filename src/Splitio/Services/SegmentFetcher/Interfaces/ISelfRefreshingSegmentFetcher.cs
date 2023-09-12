using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISelfRefreshingSegmentFetcher : IPeriodicTask
    {
        Task<bool> FetchAllAsync();
        Task FetchAsync(string segmentName, FetchOptions fetchOptions);
        Task FetchSegmentsIfNotExistsAsync(IList<string> names);
        void Clear();
    }
}
