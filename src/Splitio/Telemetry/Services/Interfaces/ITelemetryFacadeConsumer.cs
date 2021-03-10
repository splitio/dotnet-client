namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ITelemetryFacadeConsumer :
        IFactoryTelemetryConsumer,
        IEvaluationTelemetryConsumer,
        IImpressionTelemetryConsumer,
        IEventTelemetryConsumer,
        ISynchronizationTelemetryConsumer,
        IHTTPTelemetryConsumer,
        ICacheTelemetryConsumer,
        IPushTelemetryConsumer,
        IStreamingTelemetryConsumer,
        IMiscTelemetryConsumer,
        ISDKInfoTelemetryConsumer
    {
    }
}
