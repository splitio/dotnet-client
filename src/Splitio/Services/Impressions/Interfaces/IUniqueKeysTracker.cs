using Splitio.Services.Shared.Interfaces;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IUniqueKeysTracker : IPeriodicTask
    {
        bool Track(string key, string featureName);
    }
}
