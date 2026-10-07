using Splitio.Domain;
using Splitio.Commons.Dto;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Threading.Tasks;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Shared.Logger;

namespace Splitio.Services.SplitFetcher.Classes
{
    public abstract class SplitChangeFetcher : ISplitChangeFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitChangeFetcher));

        protected abstract Task<TargetingRulesDto> FetchFromBackendAsync(FetchOptions fetchOptions);

        public async Task<TargetingRulesDto> FetchAsync(FetchOptions fetchOptions)
        {
            try
            {
                return await FetchFromBackendAsync(fetchOptions);
            }
            catch(Exception e)
            {
                _log.Error($"Exception caught executing Fetch since={fetchOptions.FeatureFlagsSince} and rbSince={fetchOptions.RuleBasedSegmentsSince}", e);
                return null;
            }
        }
    }
}
