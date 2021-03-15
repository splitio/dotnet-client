namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ITelemetryFacadeProducer : 
        IFactoryTelemetryProducer, 
        IEvaluationTelemetryProducer,
        IImpressionTelemetryProducer,
        IEventTelemetryProducer,
        ISynchronizationTelemetryProducer,
        IHTTPTelemetryProducer,
        IPushTelemetryProducer,
        IStreamingTelemetryProducer,
        IMiscTelemetryProducer,
        ISDKInfoTelemetryProducer
    {
    }
}
