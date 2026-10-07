using Splitio.Commons.Dto;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISegmentChangeFetcher
    {
        Task<SegmentChange> FetchAsync(string name, long change_number, FetchOptions fetchOptions);
    }
}
