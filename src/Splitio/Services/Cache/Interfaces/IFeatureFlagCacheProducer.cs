using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IFeatureFlagCacheProducer
    {
        void Update(List<ParsedSplit> toAdd, List<string> toRemove, long till);
        void SetChangeNumber(long changeNumber);
        void Clear();
        void Kill(long changeNumber, string splitName, string defaultTreatment);
    }
}
