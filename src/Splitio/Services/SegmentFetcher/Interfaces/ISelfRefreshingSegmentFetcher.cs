using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISelfRefreshingSegmentFetcher
    {
        void Start();
        void Stop();
        Task FetchAll();
        Task Fetch(string segmentName, FetchOptions fetchOptions);
        Task FetchSegmentsIfNotExists(IList<string> names);
        void Clear();
    }
}
