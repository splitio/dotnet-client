using Splitio.Commons.Telemetry.Storages;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Commons.Impressions.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Commons.Cache.Interfaces;
using Splitio.Commons.Engine.Evaluator;

namespace Splitio_Tests.Unit_Tests.Client
{
    public class SplitClientForTesting : SplitClient
    {
        public SplitClientForTesting(IFeatureFlagCacheConsumer featureFlagCacheConsumer,
            IEventsCache eventsLog,
            IImpressionsCache impressionsLog,
            IBlockUntilReadyService blockUntilReadyService,
            IEvaluator evaluator,
            IImpressionsManager impressionsManager,
            ISyncManager syncManager,
            ITelemetryEvaluationProducer telemetryEvaluationProducer,
            IFallbackTreatmentCalculator fallbackTreatmentCalculator)
            : base("SplitClientForTesting")
        {
            BuildEventsManager();
            BuildStatusAndTaskManager();
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
