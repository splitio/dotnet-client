using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitSdkApiClient
    {
        Task<ApiFetchResult> FetchSplitChangesAsync(FetchOptions fetchOptions);
    }

    public class ApiFetchResult
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public string Spec { get; set; }
        public bool ClearCache { get; set; }
    }
}
