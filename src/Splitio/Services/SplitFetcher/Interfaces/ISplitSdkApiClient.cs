using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitSdkApiClient
    {
        Task<string> FetchSplitChangesAsync(long since, FetchOptions fetchOptions);
    }
}
