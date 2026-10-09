using Splitio.Commons.Dto;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Interfaces
{
    public interface ISegmentSdkApiClient
    {
        Task<string> FetchSegmentChangesAsync(string name, long since, FetchOptions fetchOptions);
    }
}
