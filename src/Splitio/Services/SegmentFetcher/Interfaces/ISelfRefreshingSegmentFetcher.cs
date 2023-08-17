using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISelfRefreshingSegmentFetcher : IPeriodicTask
    {
        bool FetchAll();
        Task Fetch(string segmentName, FetchOptions fetchOptions);
        Task FetchSegmentsIfNotExists(IList<string> names);
        Task ClearAsync();
    }
}
