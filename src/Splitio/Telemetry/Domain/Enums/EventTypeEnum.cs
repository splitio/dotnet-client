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
}
