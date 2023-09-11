using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IFeatureFlagCacheProducer
    {
        void AddSplit(string splitName, SplitBase split);
        bool RemoveSplit(string splitName);
        bool AddOrUpdate(string splitName, SplitBase split);
        void SetChangeNumber(long changeNumber);
        void Clear();
        void Kill(long changeNumber, string splitName, string defaultTreatment);

        #region Async
        Task AddSplitAsync(string splitName, SplitBase split);
        Task<bool> RemoveSplitAsync(string splitName);
        Task<bool> AddOrUpdateAsync(string splitName, SplitBase split);
        Task SetChangeNumberAsync(long changeNumber);
        Task ClearAsync();
        Task KillAsync(long changeNumber, string splitName, string defaultTreatment);
        #endregion
    }
}
