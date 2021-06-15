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

        protected override async Task<SplitChangesResult> FetchFromBackend(long since, bool cacheControlHeaders = false)
        {
            var fetchResult = await _apiClient.FetchSplitChanges(since, cacheControlHeaders);

            var splitChangesResult = JsonConvert.DeserializeObject<SplitChangesResult>(fetchResult);
            return splitChangesResult;
        }
    }
}
