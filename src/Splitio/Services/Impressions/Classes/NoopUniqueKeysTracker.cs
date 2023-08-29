using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Services.Impressions.Classes
{
    public class NoopUniqueKeysTracker : IUniqueKeysTracker
    {
        public void Start()
        {
            // No-op
        }

        public void Stop()
        {
            // No-op
        }

        public bool Track(string key, string featureName)
        {
            // No-op
            return true;
        }
    }
}
