using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Enums;
using Splitio.Enums.Extensions;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
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
        private readonly IEventTypeValidator _eventTypeValidator;
        private readonly IEventPropertiesValidator _eventPropertiesValidator;
        private readonly ITrafficTypeValidator _trafficTypeValidator;
        private readonly IFlagSetsValidator _flagSetsValidator;
        private readonly IFlagSetsFilter _flagSetsFilter;

        public ClientExtensionService(IBlockUntilReadyService blockUntilReadyService,
            IStatusManager statusManager,
            IKeyValidator keyValidator,
            ISplitNameValidator splitNameValidator,
            ITelemetryEvaluationProducer telemetryEvaluationProducer,
            IEventTypeValidator eventTypeValidator,
            IEventPropertiesValidator eventPropertiesValidator,
            ITrafficTypeValidator trafficTypeValidator,
            IFlagSetsValidator flagSetsValidator,
            IFlagSetsFilter flagSetsFilter)
        {
            _blockUntilReadyService = blockUntilReadyService;
            _statusManager = statusManager;
            _keyValidator = keyValidator;
            _splitNameValidator = splitNameValidator;
            _telemetryEvaluationProducer = telemetryEvaluationProducer;
            _eventTypeValidator = eventTypeValidator;
            _eventPropertiesValidator = eventPropertiesValidator;
            _trafficTypeValidator = trafficTypeValidator;
            _flagSetsValidator = flagSetsValidator;
            _flagSetsFilter = flagSetsFilter;
        }

        public bool TrackValidations(string key, string trafficType, string eventType, double? value, Dictionary<string, object> properties, out WrappedEvent wrappedEvent)
        {
            wrappedEvent = null;

            var keyResult = _keyValidator.IsValid(new Key(key, null), API.Track);
            var eventTypeResult = _eventTypeValidator.IsValid(eventType, nameof(eventType));
            var eventPropertiesResult = _eventPropertiesValidator.IsValid(properties);
            var trafficTypeResult = _trafficTypeValidator.IsValid(trafficType, API.Track);

            if (!keyResult || !trafficTypeResult.Success || !eventTypeResult || !eventPropertiesResult.Success)
                return false;

            wrappedEvent = new WrappedEvent
            {
                Size = eventPropertiesResult.EventSize,
                Event = new Event
                {
                    key = key,
                    trafficTypeName = trafficTypeResult.Value,
                    eventTypeId = eventType,
                    value = value,
                    timestamp = CurrentTimeHelper.CurrentTimeMillis(),
                    properties = (Dictionary<string, object>)eventPropertiesResult.Value
                }
            };

            return true;
        }

        public bool TreatmentValidations(API method, Key key, string featureFlagName, ISplitLogger logger, out string ffNameSanitized)
        {
            ffNameSanitized = null;

            var ffNames = TreatmentsValidations(method, key, new List<string> { featureFlagName }, logger, out List<TreatmentResult> controlTreatments);

            if (controlTreatments != null || !ffNames.Any()) return false;

            ffNameSanitized = ffNames.FirstOrDefault();

            return true;
        }

        public List<string> FlagSetsValidations(API method, Key key, List<string> flagSets, ISplitLogger logger)
        {
            if (!IsClientReady(method, logger, new List<string>()) || !_flagSetsValidator.AreValid(method.ToString(), flagSets, _flagSetsFilter, out var setsToReturn))
            {
                return new List<string>();
            }

            return setsToReturn.ToList();
        }

        public List<string> TreatmentsValidations(API method, Key key, List<string> features, ISplitLogger logger, out List<TreatmentResult> result)
        {
            result = null;

            if (!IsClientReady(method, logger, features) || !_keyValidator.IsValid(key, method))
            {
                result = ReturnControl(features);

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

        public List<TreatmentResult> ReturnControl(List<string> featureFlagNames)
        {
            var toReturn = new List<TreatmentResult>();

            foreach (var item in featureFlagNames)
            {
                toReturn.Add(new TreatmentResult(item, Labels.Exception, Constants.Gral.Control, false));
            }

            return toReturn;
        }

        private bool IsClientReady(API method, ISplitLogger logger, List<string> features)
        {
            if (_statusManager.IsDestroyed())
            {
                logger.Error("Client has already been destroyed - No calls possible");
                return false;
            }

            if (!_blockUntilReadyService.IsSdkReady())
            {
                logger.Warn($"{method}: the SDK is not ready, results may be incorrect for feature flag {string.Join(",", features)}. Make sure to wait for SDK readiness before using this method");
                return false;
            }

            return true;
        }
    }
}
