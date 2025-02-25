using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IRuleBasedSegmentCacheConsumer
    {
        long GetChangeNumber();
        RuleBasedSegment Get(string name);

        #region Async
        Task<long> GetChangeNumberAsync();
        Task<RuleBasedSegment> GetAsync(string name);
        #endregion
    }
}
