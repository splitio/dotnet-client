using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISplitCache
    {
        bool TrafficTypeExists(string trafficType);
        long GetChangeNumber();
        List<ParsedSplit> GetAllSplits();
        List<string> GetSplitNames();
        ParsedSplit GetSplit(string splitName);
        List<ParsedSplit> FetchMany(List<string> splitNames);
        void Kill(long changeNumber, string splitName, string defaultTreatment);
        int SplitsCount();
        void AddSplit(string splitName, SplitBase split);
        bool RemoveSplit(string splitName);
        bool AddOrUpdate(string splitName, SplitBase split);
        void SetChangeNumber(long changeNumber);
        void Clear();
        
    }
}
