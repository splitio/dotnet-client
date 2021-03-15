namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ICacheTelemetryConsumer
    {
        long GetSplitsCount();
        long GetSegmentsCount();
        long GetSegmentKeysCount();
    }
}
