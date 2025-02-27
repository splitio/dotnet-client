using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public abstract class SplitChangeFetcher : ISplitChangeFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitChangeFetcher));

        protected abstract Task<SplitChangesResult> FetchFromBackendAsync(FetchOptions fetchOptions);

        public async Task<SplitChangesResult> FetchAsync(FetchOptions fo)
        {
            try
            {
                return await FetchFromBackendAsync(fo);
            }
            catch(Exception e)
            {
                _log.Error($"Exception caught executing Fetch since={fo.FeatureFlagsSince} and rbSince={fo.RuleBasedSegmentsSince}", e);
                return null;
            }
        }
    }
}
