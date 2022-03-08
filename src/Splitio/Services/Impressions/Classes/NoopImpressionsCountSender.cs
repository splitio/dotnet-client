using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Services.Impressions.Classes
{
    public class NoopImpressionsCountSender : IImpressionsCountSender
    {
        public void Start()
        {
            // No op.
        }

        public void Stop()
        {
            // No op.
        }
    }
}
