using Newtonsoft.Json;
using Splitio.Constants;
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

        protected override async Task<TargetingRulesDto> FetchFromBackendAsync(FetchOptions fetchOptions)
        {
            var result = await _apiClient.FetchSplitChangesAsync(fetchOptions);
            if (!result.Success)
            {
                return null;
            }

            if (result.Spec.Equals(ApiVersions.Spec1_1))
            {
                var featureFlags = JsonConvert.DeserializeObject<OldSplitChangesDto>(result.Content);

                return featureFlags.ToTargetingRulesDto(result.ClearCache);
            }

            var targetingRulesDto = JsonConvert.DeserializeObject<TargetingRulesDto>(result.Content);
            targetingRulesDto.ClearCache = result.ClearCache;
            
            return targetingRulesDto;
        }
    }
}
