using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
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

        public bool TreatmentValidations(Enums.API method, Key key, string featureFlagName, ISplitLogger logger, out string ffNameSanitized)
        {
            ffNameSanitized = null;

            var ffNames = TreatmentsValidations(method, key, new List<string> { featureFlagName }, logger, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null || !ffNames.Any()) return false;

            ffNameSanitized = ffNames.FirstOrDefault();

            return true;
        }

        public List<string> TreatmentsValidations(Enums.API method, Key key, List<string> features, ISplitLogger logger, out Dictionary<string, TreatmentResult> result)
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

        public void RecordException(Enums.API method)
        {
            if (_telemetryEvaluationProducer == null) return;

            switch (method)
            {
                case Enums.API.GetTreatment:
                case Enums.API.GetTreatmentAsync:
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Treatment);
                    break;
                case Enums.API.GetTreatments:
                case Enums.API.GetTreatmentsAsync:
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Treatments);
                    break;
                case Enums.API.GetTreatmentWithConfig:
                case Enums.API.GetTreatmentWithConfigAsync:
                    _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentWithConfig);
                    break;
                case Enums.API.GetTreatmentsWithConfig:
                case Enums.API.GetTreatmentsWithConfigAsync:
                    _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentsWithConfig);
                    break;
                case Enums.API.Track:
                case Enums.API.TrackAsync:
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Track);
                    break;
            }
        }

        public void RecordLatency(Enums.API method, long latency)
        {
            if (_telemetryEvaluationProducer == null) return;

            int bucket = Util.Metrics.Bucket(latency);

            switch (method)
            {
                case Enums.API.GetTreatment:
                case Enums.API.GetTreatmentAsync:
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatment, bucket);
                    break;
                case Enums.API.GetTreatments:
                case Enums.API.GetTreatmentsAsync:
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatments, bucket);
                    break;
                case Enums.API.GetTreatmentWithConfig:
                case Enums.API.GetTreatmentWithConfigAsync:
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.TreatmentWithConfig, bucket);
                    break;
                case Enums.API.GetTreatmentsWithConfig:
                case Enums.API.GetTreatmentsWithConfigAsync:
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.TreatmentsWithConfig, bucket);
                    break;
                case Enums.API.Track:
                case Enums.API.TrackAsync:
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Track, bucket);
                    break;
            }
        }

        private bool IsClientReady(Enums.API method, ISplitLogger logger)
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
