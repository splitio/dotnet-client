using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface ISplitCache
    {
        Task<ParsedSplit> GetSplitAsync(string splitName);
        Task<List<ParsedSplit>> GetAllSplitsAsync();
        Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames);
        Task<List<string>> GetSplitNamesAsync();
        Task KillAsync(long changeNumber, string splitName, string defaultTreatment);
        Task<int> SplitsCountAsync();
        Task<long> GetChangeNumberAsync();
        Task<bool> TrafficTypeExistsAsync(string trafficType);


        void AddSplit(string splitName, SplitBase split);
        bool RemoveSplit(string splitName);
        bool AddOrUpdate(string splitName, SplitBase split);
        void SetChangeNumber(long changeNumber);
        
        void Clear();
        
    }
}
