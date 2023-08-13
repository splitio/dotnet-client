using Splitio.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitFetcher
    {
        void Start();
        void Stop();
        Task<FetchResult> FetchSplits(FetchOptions fetchOptions);
        void Clear();
    }

    public class FetchResult
    {
        public List<string> SegmentNames { get; set; }
        public bool Success { get; set; }
    }
}
