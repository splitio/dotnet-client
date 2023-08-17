using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitFetcher : IPeriodicTask
    {
        Task<FetchResult> FetchSplits(FetchOptions fetchOptions);
        Task ClearAsync();
    }

    public class FetchResult
    {
        public List<string> SegmentNames { get; set; }
        public bool Success { get; set; }
    }
}
