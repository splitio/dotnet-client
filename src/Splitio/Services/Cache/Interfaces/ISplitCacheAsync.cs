using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public  interface ISplitCacheAsync
    {
        Task AddSplitAsync(string splitName, SplitBase split);
        Task<bool> RemoveSplitAsync(string splitName);
        Task<bool> AddOrUpdateAsync(string splitName, SplitBase split);
        Task SetChangeNumberAsync(long changeNumber);
        Task<long> GetChangeNumberAsync();
        Task<ParsedSplit> GetSplitAsync(string splitName);
        Task<List<ParsedSplit>> GetAllSplitsAsync();
        Task ClearAsync();
        Task<bool> TrafficTypeExistsAsync(string trafficType);
        Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames);
        Task KillAsync(long changeNumber, string splitName, string defaultTreatment);
        Task<List<string>> GetSplitNamesAsync();
        Task<int> SplitsCountAsync();
    }
}
