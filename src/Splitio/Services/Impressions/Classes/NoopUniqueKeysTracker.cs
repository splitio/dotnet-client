using Splitio.Services.Impressions.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class NoopUniqueKeysTracker : IUniqueKeysTracker
    {
        public void Start()
        {
            // No-op
        }

        public Task StopAsync()
        {
            // No op.
            return Task.FromResult(0);
        }

        public bool Track(string key, string featureName)
        {
            // No-op
            return true;
        }
    }
}
