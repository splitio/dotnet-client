using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IFeatureFlagCacheConsumer
    {
        long GetChangeNumber();
        ParsedSplit GetSplit(string splitName);
        List<ParsedSplit> GetAllSplits();
        bool TrafficTypeExists(string trafficType);
        List<ParsedSplit> FetchMany(List<string> splitNames);
        List<string> GetSplitNames();
        int SplitsCount();

        #region Async
        Task<ParsedSplit> GetSplitAsync(string splitName);
        Task<List<ParsedSplit>> GetAllSplitsAsync();
        Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames);
        Task<List<string>> GetSplitNamesAsync();
        #endregion
    }
}
