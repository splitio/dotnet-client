namespace Splitio.Enums
{
    public enum Task
    {
        SegmentsFetcher,
        FeatureFlagsFetcher,
        StreamingTokenRefresh,
        EventsSender,
        ImpressionsSender,
        MTKsSender,
        ImpressionsCountSender,
        CacheLongTermCleaning,
        TelemetryStats,
        TelemetryInit,
        SegmentsWorkerFetcher,
        SegmentsWorker,
        FeatureFlagsWorker,
        EventsSenderAPI,
        Track,
        SDKInitialization,
        SSEConnect,
        OnStreamingStatusTask
    }
}
