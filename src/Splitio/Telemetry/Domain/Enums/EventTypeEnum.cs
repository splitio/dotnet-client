namespace Splitio.Telemetry.Domain.Enums
{
    public enum EventTypeEnum
    {
        SSEConnectionEstablished = 0,
        OccupancyPri = 10,
        OccupancySec = 20,
        StreamingStatus = 30,
        ConnectionError = 40,
        TokenRefresh = 50,
        AblyError = 60,
        SyncMode = 70
    }

    public enum StreamingStatusEnum
    {
        Disabled,
        Enabled,
        Paused
    }

    public enum ConnectionErrorEnum
    {
        Requested,
        Non_Requested
    }

    public enum SyncModeEnum
    {
        Streaming,
        Polling
    }
}
