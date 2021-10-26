namespace Splitio.Telemetry.Common
{
    public interface ITelemetrySyncTask
    {
        void Start();
        void Stop();
        void RecordConfigInit();
    }
}
