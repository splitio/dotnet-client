using Splitio.Commons.Dto;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISelfRefreshingSegment
    {
        Task<bool> FetchSegmentAsync(FetchOptions fetchOptions);
    }
}
