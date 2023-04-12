using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SelfRefreshingSegment : ISelfRefreshingSegment
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSegment));

        public string Name;
        private readonly ISegmentChangeFetcher _segmentChangeFetcher;
        private readonly ISegmentCache _segmentCache;

        public SelfRefreshingSegment(string name, ISegmentChangeFetcher segmentChangeFetcher, ISegmentCache segmentCache)
        {
            Name = name;

            _segmentChangeFetcher = segmentChangeFetcher;
            _segmentCache = segmentCache;
        }

        public async Task<bool> FetchSegmentAsync(FetchOptions fetchOptions)
        {
            var success = false;

            while (true)
            {
                var changeNumber = await _segmentCache.GetChangeNumberAsync(Name);

                try
                {
                    var response = await _segmentChangeFetcher.Fetch(Name, changeNumber, fetchOptions);

                    if (response == null)
                    {
                        break;
                    }

                    if (changeNumber >= response.till)
                    {
                        success = true;
                        break;
                    }

                    if (response.added.Count() > 0 || response.removed.Count() > 0)
                    {
                        await _segmentCache.AddToSegmentAsync(Name, response.added);
                        await _segmentCache.RemoveFromSegmentAsync(Name, response.removed);

                        if (_log.IsDebugEnabled)
                        {
                            if (response.added.Count() > 0)
                            {
                                _log.Debug($"Segment {Name} - Added : {string.Join(" - ", response.added)}");
                            }

                            if (response.removed.Count() > 0)
                            {
                                _log.Debug($"Segment {Name} - Removed : {string.Join(" - ", response.removed)}");
                            }
                        }
                    }

                    await _segmentCache.SetChangeNumberAsync(Name, response.till);
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught refreshing segment", e);
                }
                finally
                {
                    if (_log.IsDebugEnabled)
                    {
                        var cn = await _segmentCache.GetChangeNumberAsync(Name);
                        _log.Debug($"segment {Name} fetch before: {changeNumber}, after: {cn}");
                    }
                }
            }

            return success;
        }
    }
}
