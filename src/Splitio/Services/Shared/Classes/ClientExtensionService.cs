using Splitio.Domain;
using Splitio.Enums;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Shared.Classes
{
    public class ClientExtensionService : IClientExtensionService
    {
        private readonly IBlockUntilReadyService _blockUntilReadyService;
        private readonly IStatusManager _statusManager;
        private readonly IKeyValidator _keyValidator;
        private readonly ISplitNameValidator _splitNameValidator;
        private readonly ITelemetryEvaluationProducer _telemetryEvaluationProducer;

        public ClientExtensionService(IBlockUntilReadyService blockUntilReadyService,
            IStatusManager statusManager,
            IKeyValidator keyValidator,
            ISplitNameValidator splitNameValidator,
            ITelemetryEvaluationProducer telemetryEvaluationProducer)
        {
            _blockUntilReadyService = blockUntilReadyService;
            _statusManager = statusManager;
            _keyValidator = keyValidator;
            _splitNameValidator = splitNameValidator;
            _telemetryEvaluationProducer = telemetryEvaluationProducer;
        }

        public bool TreatmentValidations(API method, Key key, string featureFlagName, ISplitLogger logger, out string ffNameSanitized)
        {
            ffNameSanitized = null;

            var ffNames = TreatmentsValidations(method, key, new List<string> { featureFlagName }, logger, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null || !ffNames.Any()) return false;

            ffNameSanitized = ffNames.FirstOrDefault();

            return true;
        }

        public List<string> TreatmentsValidations(API method, Key key, List<string> features, ISplitLogger logger, out Dictionary<string, TreatmentResult> result)
        {
            result = null;

            if (!IsClientReady(method, logger) || !_keyValidator.IsValid(key, method))
            {
                result = new Dictionary<string, TreatmentResult>();
                foreach (var feature in features)
                {
                    result.Add(feature, new TreatmentResult(string.Empty, Constants.Gral.Control, null));
                }

                return new List<string>();
            }

            return _splitNameValidator.SplitNamesAreValid(features, method);
        }

        public void RecordException(API method)
        {
            if (_telemetryEvaluationProducer == null) return;

            _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());
        }

        public void RecordLatency(API method, long latency)
        {
            if (_telemetryEvaluationProducer == null) return;

            _telemetryEvaluationProducer.RecordLatency(method.ConvertToMethodEnum(), Util.Metrics.Bucket(latency));
        }

        public async System.Threading.Tasks.Task RecordExceptionAsync(API method)
        {
            if (_telemetryEvaluationProducer == null) return;

            await _telemetryEvaluationProducer.RecordExceptionAsync(method.ConvertToMethodEnum());
        }

        public async System.Threading.Tasks.Task RecordLatencyAsync(API method, long latency)
        {
            if (_telemetryEvaluationProducer == null) return;

            await _telemetryEvaluationProducer.RecordLatencyAsync(method.ConvertToMethodEnum(), Util.Metrics.Bucket(latency));
        }

        private bool IsClientReady(API method, ISplitLogger logger)
        {
            if (_statusManager.IsDestroyed())
            {
                logger.Error("Client has already been destroyed - No calls possible");
                return false;
            }

            if (!_blockUntilReadyService.IsSdkReady())
            {
                logger.Error($"{method}: the SDK is not ready, the operation cannot be executed.");
                return false;
            }

            return true;
        }
    }
}
