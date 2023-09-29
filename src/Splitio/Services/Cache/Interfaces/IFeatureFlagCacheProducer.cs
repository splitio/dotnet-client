using Splitio.Domain;

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
    }
}
