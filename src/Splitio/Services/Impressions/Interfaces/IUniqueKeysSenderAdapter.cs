using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IUniqueKeysSenderAdapter
    {
        void RecordUniqueKeys(ConcurrentDictionary<string, HashSet<string>> uniques);
    }
}
