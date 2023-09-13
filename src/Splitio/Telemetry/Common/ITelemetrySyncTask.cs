using Splitio.Services.Shared.Interfaces;

namespace Splitio.Telemetry.Common
{
    public interface ITelemetrySyncTask : IPeriodicTask
    {
        void RecordConfigInit(long timeUntilSDKReady);
    }
}
