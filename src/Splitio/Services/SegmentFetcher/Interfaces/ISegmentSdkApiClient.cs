using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISegmentSdkApiClient
    {
        Task<string> FetchSegmentChangesAsync(string name, long since, FetchOptions fetchOptions);
    }
}
