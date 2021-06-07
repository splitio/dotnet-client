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
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(SegmentChangeFetcher));

        protected abstract Task<SegmentChange> FetchFromBackend(string name, long since, bool cacheControlHeaders = false);

        public async Task<SegmentChange> Fetch(string name, long since, bool cacheControlHeaders = false)
        {
            try
            {
                return await FetchFromBackend(name, since, cacheControlHeaders);
            }
            catch(Exception e)
            {
                _log.Error(string.Format("Exception caught executing fetch segment changes since={0}", since), e);
                return null;
            }
        }
    }   
}
