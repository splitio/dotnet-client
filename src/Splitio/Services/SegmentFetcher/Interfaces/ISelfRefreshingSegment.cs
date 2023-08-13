using Splitio.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISelfRefreshingSegment
    {
        Task<bool> FetchSegment(FetchOptions fetchOptions);
    }
}
