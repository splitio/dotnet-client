using Splitio.Services.Impressions.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsSenderAdapter
    {
        void RecordUniqueKeys(ConcurrentDictionary<string, HashSet<string>> uniques);
        void RecordImpressionsCount(ConcurrentDictionary<KeyCache, int> impressionsCount);
    }
}
