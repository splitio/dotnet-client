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
        private static readonly ISplitLogger Log = WrapperAdapter.GetLogger(typeof(SplitChangeFetcher));

        protected abstract Task<SplitChangesResult> FetchFromBackend(long since, FetchOptions fetchOptions);

        public async Task<SplitChangesResult> Fetch(long since, FetchOptions fetchOptions)
        {
            try
            {
                return await FetchFromBackend(since, fetchOptions);
            }
            catch(Exception e)
            {
                Log.Error(string.Format("Exception caught executing Fetch since={0}", since), e);
                return null;
            }
        }
    }
}
