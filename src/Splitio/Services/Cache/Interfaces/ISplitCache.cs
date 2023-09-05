using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISplitCache
    {
        // Producer
        void Update(List<ParsedSplit> toAdd, List<ParsedSplit> toRemove, long till);
        void SetChangeNumber(long changeNumber);
        void Kill(long changeNumber, string splitName, string defaultTreatment);
        void Clear();

        // Consumer
        long GetChangeNumber();
        ParsedSplit GetSplit(string splitName);
        List<ParsedSplit> GetAllSplits();
        bool TrafficTypeExists(string trafficType);
        List<ParsedSplit> FetchMany(List<string> splitNames);
        List<string> GetSplitNames();
        int SplitsCount();
        Dictionary<string, HashSet<string>> GetNamesByFlagSets(List<string> flagSets);
    }
}
