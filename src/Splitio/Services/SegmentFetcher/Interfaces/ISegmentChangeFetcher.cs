using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISegmentChangeFetcher
    {
        // TODO: Rename to FetchAsync
        Task<SegmentChange> Fetch(string name, long change_number, FetchOptions fetchOptions);
    }
}
