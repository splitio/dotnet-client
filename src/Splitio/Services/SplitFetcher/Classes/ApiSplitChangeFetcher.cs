using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.SplitFetcher.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class ApiSplitChangeFetcher : SplitChangeFetcher, ISplitChangeFetcher 
    {
        private readonly ISplitSdkApiClient _apiClient;

        public ApiSplitChangeFetcher(ISplitSdkApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        protected override async Task<SplitChangesResult> FetchFromBackendAsync(long since, FetchOptions fetchOptions)
        {
            var fetchResult = await _apiClient.FetchSplitChangesAsync(since, fetchOptions);

            var splitChangesResult = JsonConvert.DeserializeObject<SplitChangesResult>(fetchResult);
            return splitChangesResult;
        }
    }
}
