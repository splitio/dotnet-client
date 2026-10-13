using Splitio.Commons.Domain;
using System.Collections.Generic;

namespace Splitio.Commons.Cache.Interfaces
{
    public interface IRuleBasedSegmentCacheProducer
    {
        void SetChangeNumber(long changeNumber);
        void Update(List<RuleBasedSegment> toAdd, List<string> toRemove, long till);
        void Clear();
    }
}
