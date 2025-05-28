using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IRuleBasedSegmentCacheConsumer : ICacheConsumer
    {
        RuleBasedSegment Get(string name);
        bool Contains(List<string> names);

        #region Async
        Task<RuleBasedSegment> GetAsync(string name);
        #endregion
    }
}
