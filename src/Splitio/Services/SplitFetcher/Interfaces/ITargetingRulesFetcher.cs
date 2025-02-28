using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ITargetingRulesFetcher : IPeriodicTask
    {
        Task<FetchResult> FetchSplitsAsync(FetchOptions fetchOptions);
        void Clear();
    }

    public class FetchResult
    {
        public List<string> SegmentNames { get; set; }
        public bool Success { get; set; }
    }
}
