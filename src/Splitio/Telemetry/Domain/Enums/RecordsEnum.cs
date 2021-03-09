namespace Splitio.Telemetry.Domain.Enums
{
    public enum ImpressionsDataRecordsEnum
    {
        ImpressionsQueued,
        ImpressionsDropped,
        ImpressionsDeduped
    }

    public enum EventsDataRecordsEnum
    {
        EventsQueued,
        EventsDropped
    }

    public enum LastSynchronizationRecordsEnum
    {
        Splits,
        Segments,
        Impressions,
        Events,
        Token,
        Telemetry
    }

    public enum SdkRecordsEnum
    {
        Session
    }

    public enum FactoryRecordsEnum
    {
        TimeUntilReady
    }
}
