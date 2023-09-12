using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public abstract class SegmentChangeFetcher: ISegmentChangeFetcher
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SegmentChangeFetcher));

        protected abstract Task<SegmentChange> FetchFromBackendAsync(string name, long since, FetchOptions fetchOptions);

        public async Task<SegmentChange> FetchAsync(string name, long since, FetchOptions fetchOptions)
        {
            try
            {
                return await FetchFromBackendAsync(name, since, fetchOptions);
            }
            catch(Exception e)
            {
                _log.Error(string.Format("Exception caught executing fetch segment changes since={0}", since), e);
                return null;
            }
        }
    }   
}
