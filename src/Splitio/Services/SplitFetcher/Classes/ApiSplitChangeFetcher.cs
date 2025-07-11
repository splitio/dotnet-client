using Splitio.Constants;
using Splitio.Domain;
using Splitio.Services.Shared.Classes;
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
                var featureFlags = JsonConvertWrapper.DeserializeObject<OldSplitChangesDto>(result.Content);

                return featureFlags.ToTargetingRulesDto(result.ClearCache);
            }

            var targetingRulesDto = JsonConvertWrapper.DeserializeObject<TargetingRulesDto>(result.Content);
            targetingRulesDto.ClearCache = result.ClearCache;
            
            return targetingRulesDto;
        }
    }
}
