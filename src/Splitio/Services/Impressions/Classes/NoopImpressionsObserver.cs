using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Services.Impressions.Classes
{
    public class NoopImpressionsObserver : IImpressionsObserver
    {
        public long? TestAndSet(KeyImpression impression)
        {
            // No op.
            return null;
        }
    }
}
