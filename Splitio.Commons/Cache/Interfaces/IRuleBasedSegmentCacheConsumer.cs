using Splitio.Commons.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Commons.Cache.Interfaces
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
