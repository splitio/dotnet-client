using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class ApiSegmentChangeFetcher : SegmentChangeFetcher, ISegmentChangeFetcher
    {
        private readonly ISegmentSdkApiClient _apiClient;

        public ApiSegmentChangeFetcher(ISegmentSdkApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        protected override async Task<SegmentChange> FetchFromBackendAsync(string name, long since, FetchOptions fetchOptions)
        {
            var fetchResult = await _apiClient.FetchSegmentChangesAsync(name, since, fetchOptions);

            return JsonConvert.DeserializeObject<SegmentChange>(fetchResult);
        }
    }
}
