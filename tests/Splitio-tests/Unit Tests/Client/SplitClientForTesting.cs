using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Evaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.Client
{
    public class SplitClientForTesting : SplitClient
    {
        public SplitClientForTesting(IFeatureFlagCacheConsumer featureFlagCacheConsumer,
            IEventsLog eventsLog,
            IImpressionsLog impressionsLog,
            IBlockUntilReadyService blockUntilReadyService,
            IEvaluator evaluator,
            IImpressionsManager impressionsManager,
            ISyncManager syncManager,
            ITelemetryEvaluationProducer telemetryEvaluationProducer,
            IFallbackTreatmentCalculator fallbackTreatmentCalculator)
            : base("SplitClientForTesting", Mode.Standalone)
        {
            _fallbackTreatmentCalculator = fallbackTreatmentCalculator;
            _eventsLog = eventsLog;
            _impressionsLog = impressionsLog;
            _blockUntilReadyService = blockUntilReadyService;
            _trafficTypeValidator = new TrafficTypeValidator(featureFlagCacheConsumer, _blockUntilReadyService);
            _evaluator = evaluator;
            _impressionsManager = impressionsManager;
            _syncManager = syncManager;
            _telemetryEvaluationProducer = telemetryEvaluationProducer;

            BuildClientExtension();
        }
    }
}
