using Splitio.Services.Shared.Interfaces;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsCounter : IPeriodicTask
    {
        void Inc(string splitName, long timeFrame);
    }
}
