using Splitio.Services.Impressions.Interfaces;
using System.Collections.Concurrent;

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
    }
}
