using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISegmentSdkApiClient
    {
        Task<string> FetchSegmentChanges(string name, long since, FetchOptions fetchOptions);
    }
}
