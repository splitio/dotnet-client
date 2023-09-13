using Splitio.Services.Impressions.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class NoopImpressionsCounter : IImpressionsCounter
    {
        public void Inc(string splitName, long timeFrame)
        {
            // No op.
        }

        public ConcurrentDictionary<KeyCache, int> PopAll()
        {
            // No op.
            return new ConcurrentDictionary<KeyCache, int>();
        }

        public void Start()
        {
            // No op.
        }

        public Task StopAsync()
        {
            // No op.
            return Task.FromResult(0);
        }
    }
}
