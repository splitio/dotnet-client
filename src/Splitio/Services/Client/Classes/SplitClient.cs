using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public abstract class SplitClient : ISplitClient
    {
        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitClient));

        protected const string Control = "control";

        protected readonly IKeyValidator _keyValidator;
        protected readonly ISplitNameValidator _splitNameValidator;
        protected readonly IEventTypeValidator _eventTypeValidator;
        protected readonly IEventPropertiesValidator _eventPropertiesValidator;
        protected readonly IWrapperAdapter _wrapperAdapter;
        protected readonly IConfigService _configService;

        protected bool LabelsEnabled;
        protected string ApiKey;

        protected ISplitManager _manager;
        protected IEventsLog _eventsLog;
        protected ITrafficTypeValidator _trafficTypeValidator;
        protected IBlockUntilReadyService _blockUntilReadyService;
        protected IFactoryInstantiationsService _factoryInstantiationsService;
        protected ISplitParser _splitParser;
        protected IEvaluator _evaluator;
        protected ITelemetryEvaluationProducer _telemetryEvaluationProducer;
        protected ITelemetryInitProducer _telemetryInitProducer;
        protected ITasksManager _tasksManager;
        protected IStatusManager _statusManager;

        protected IImpressionsLog _impressionsLog;
        protected IUniqueKeysTracker _uniqueKeysTracker;
        protected IImpressionListener _customerImpressionListener;
        protected IImpressionsManager _impressionsManager;
        protected IImpressionsSenderAdapter _impressionsSenderAdapter;
        protected IImpressionsCounter _impressionsCounter;
        protected IImpressionsObserver _impressionsObserver;

        public SplitClient()
        {
            _wrapperAdapter = WrapperAdapter.Instance();
            _keyValidator = new KeyValidator();
            _splitNameValidator = new SplitNameValidator();
            _eventTypeValidator = new EventTypeValidator();
            _eventPropertiesValidator = new EventPropertiesValidator();
            _factoryInstantiationsService = FactoryInstantiationsService.Instance();
            _configService = new ConfigService(_wrapperAdapter);
            _tasksManager = new TasksManager(_wrapperAdapter);
            _statusManager = new InMemoryReadinessGatesCache();
        }

        #region Public Async Methods
        public async Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            var result = await GetTreatmentAsync(nameof(GetTreatmentAsync), new Key(key, null), feature, attributes);

            return result.Treatment;
        }

        public async Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(nameof(GetTreatmentsAsync), new Key(key, null), features, attributes);

            return results
                .ToDictionary(r => r.Key, r => r.Value.Treatment);
        }

        public async Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentAsync(nameof(GetTreatmentWithConfigAsync), new Key(key, null), feature, attributes);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(nameof(GetTreatmentsWithConfigAsync), new Key(key, null), features, attributes);

            return results
                .ToDictionary(r => r.Key, r => new SplitResult
                {
                    Treatment = r.Value.Treatment,
                    Config = r.Value.Config
                });
        }
        #endregion

        #region Public Sync Methods
        public virtual string GetTreatment(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var result = GetTreatmentSync(nameof(GetTreatment), key, feature, attributes);

            return result.Treatment;
        }

        public virtual string GetTreatment(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return GetTreatment(new Key(key, null), feature, attributes);
        }

        public SplitResult GetTreatmentWithConfig(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentWithConfig(new Key(key, null), feature, attributes);
        }

        public SplitResult GetTreatmentWithConfig(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentSync(nameof(GetTreatmentWithConfig), key, feature, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfig(new Key(key, null), features, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(nameof(GetTreatmentsWithConfig), key, features, attributes);

            return results
                .ToDictionary(r => r.Key, r => new SplitResult
                {
                    Treatment = r.Value.Treatment,
                    Config = r.Value.Config
                });
        }

        public Dictionary<string, string> GetTreatments(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatments(new Key(key, null), features, attributes);
        }

        public Dictionary<string, string> GetTreatments(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(nameof(GetTreatments), key, features, attributes);

            return results
                .ToDictionary(r => r.Key, r => r.Value.Treatment);
        }

        public virtual bool Track(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            if (_statusManager.IsDestroyed()) return false;

            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var keyResult = _keyValidator.IsValid(new Key(key, null), nameof(Track));
                var eventTypeResult = _eventTypeValidator.IsValid(eventType, nameof(eventType));
                var eventPropertiesResult = _eventPropertiesValidator.IsValid(properties);

                var trafficTypeResult = _blockUntilReadyService.IsSdkReady()
                    ? _trafficTypeValidator.IsValid(trafficType, nameof(trafficType))
                    : new ValidatorResult { Success = true, Value = trafficType };

                if (!keyResult || !trafficTypeResult.Success || !eventTypeResult || !eventPropertiesResult.Success)
                    return false;

                try
                {
                    var eventToLog = new Event
                    {
                        key = key,
                        trafficTypeName = trafficTypeResult.Value,
                        eventTypeId = eventType,
                        value = value,
                        timestamp = CurrentTimeHelper.CurrentTimeMillis(),
                        properties = (Dictionary<string, object>)eventPropertiesResult.Value
                    };

                    _tasksManager.Start(() =>
                    {
                        _eventsLog.Log(new WrappedEvent
                        {
                            Event = eventToLog,
                            Size = eventPropertiesResult.EventSize
                        });
                    }, new CancellationTokenSource(), "Track");

                    RecordLatency(nameof(Track), clock.ElapsedMilliseconds);

                    return true;
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught trying to track an event", e);
                    RecordException(nameof(Track));
                    return false;
                }
            }
        }

        public bool IsDestroyed()
        {
            return _statusManager.IsDestroyed();
        }

        public virtual void Destroy()
        {
            if (!_statusManager.IsDestroyed())
            {
                _factoryInstantiationsService.Decrease(ApiKey);
                _statusManager.SetDestroy();
            }
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            _blockUntilReadyService.BlockUntilReady(blockMilisecondsUntilReady);
        }

        public ISplitManager GetSplitManager()
        {
            return _manager;
        }
        #endregion

        #region Protected Methods
        protected void BuildUniqueKeysTracker(BaseConfig config)
        {
            if (config.ImpressionsMode != ImpressionsMode.None)
            {
                _uniqueKeysTracker = new NoopUniqueKeysTracker();
                return;
            }

            var bloomFilter = new BloomFilter(config.BfExpectedElements, config.BfErrorRate);
            var filterAdapter = new FilterAdapter(bloomFilter);
            var trackerCache = new ConcurrentDictionary<string, HashSet<string>>();
            var trackerConfig = new ComponentConfig(config.UniqueKeysRefreshRate, config.UniqueKeysCacheMaxSize, config.UniqueKeysBulkSize);

            _uniqueKeysTracker = new UniqueKeysTracker(trackerConfig, filterAdapter, trackerCache, _impressionsSenderAdapter, _tasksManager);
        }

        protected void BuildImpressionsCounter(BaseConfig config)
        {
            if (config.ImpressionsMode == ImpressionsMode.Debug)
            {
                _impressionsCounter = new NoopImpressionsCounter();
                return;
            }

            var trackerConfig = new ComponentConfig(config.ImpressionsCounterRefreshRate, config.ImpressionsCounterCacheMaxSize, config.ImpressionsCountBulkSize);

            _impressionsCounter = new ImpressionsCounter(trackerConfig, _impressionsSenderAdapter, _tasksManager);
        }
        #endregion

        #region Private Methods
        private SplitResult GetTreatmentSync(string method, Key key, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            featureFlagName = ProcessGetTreatmentValidation(method, key, featureFlagName, out TreatmentResult controlTreatment);

            if (controlTreatment != null) return new SplitResult(controlTreatment.Treatment, controlTreatment.Config);

            var evaluatorResult = _evaluator.EvaluateFeature(key, featureFlagName, attributes);

            RecordImpressionAndTelemetry(evaluatorResult, key, featureFlagName, method);

            return new SplitResult(evaluatorResult.Treatment, evaluatorResult.Config);
        }

        private Dictionary<string, TreatmentResult> GetTreatmentsSync(string method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            features = ProcessGetTreatmentsValidation(method, key, features, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var results = _evaluator.EvaluateFeatures(key, features, attributes);

            return ParseTreatmentsAndRecordImpressionsAndTelemetry(method, results, key);
        }

        private async Task<SplitResult> GetTreatmentAsync(string method, Key key, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            featureFlagName = ProcessGetTreatmentValidation(method, key, featureFlagName, out TreatmentResult controlTreatment);

            if (controlTreatment != null) return new SplitResult(controlTreatment.Treatment, controlTreatment.Config);

            var evaluatorResult = await _evaluator.EvaluateFeatureAsync(key, featureFlagName, attributes);

            RecordImpressionAndTelemetry(evaluatorResult, key, featureFlagName, method);

            return new SplitResult(evaluatorResult.Treatment, evaluatorResult.Config);
        }

        private async Task<Dictionary<string, TreatmentResult>> GetTreatmentsAsync(string method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            features = ProcessGetTreatmentsValidation(method, key, features, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var results = await _evaluator.EvaluateFeaturesAsync(key, features, attributes);

            return ParseTreatmentsAndRecordImpressionsAndTelemetry(method, results, key);
        }

        private bool IsClientReady(string methodName)
        {
            if (!_blockUntilReadyService.IsSdkReady())
            {
                _log.Error($"{methodName}: the SDK is not ready, the operation cannot be executed.");
                return false;
            }

            if (_statusManager.IsDestroyed())
            {
                _log.Error("Client has already been destroyed - No calls possible");
                return false;
            }

            return true;
        }

        private Dictionary<string, TreatmentResult> ParseTreatmentsAndRecordImpressionsAndTelemetry(string method, MultipleEvaluatorResult results, Key key)
        {
            var impressionsQueue = new List<KeyImpression>();
            var treatmentsForFeatures = new Dictionary<string, TreatmentResult>();

            RecordTelemetry(results.Exception, method, results.ElapsedMilliseconds);

            foreach (var treatmentResult in results.TreatmentResults)
            {
                treatmentsForFeatures.Add(treatmentResult.Key, treatmentResult.Value);

                var impression = BuildImpression(treatmentResult.Value, key, treatmentResult.Key);

                if (impression != null) impressionsQueue.Add(impression);
            }

            if (impressionsQueue.Any()) _impressionsManager.Track(impressionsQueue);

            return treatmentsForFeatures;
        }

        private string ProcessGetTreatmentValidation(string method, Key key, string featureFlagName, out TreatmentResult result)
        {
            result = null;

            if (!IsClientReady(method) || !_keyValidator.IsValid(key, method))
            {
                result = new TreatmentResult(string.Empty, Control, null);
                return string.Empty;
            }

            var splitNameResult = _splitNameValidator.SplitNameIsValid(featureFlagName, method);

            if (!splitNameResult.Success)
            {
                result = new TreatmentResult(string.Empty, Control, null);
                return string.Empty;
            }

            return splitNameResult.Value;
        }

        private List<string> ProcessGetTreatmentsValidation(string method, Key key, List<string> features, out Dictionary<string, TreatmentResult> result)
        {
            result = null;

            if (!IsClientReady(method) || !_keyValidator.IsValid(key, method))
            {
                result = new Dictionary<string, TreatmentResult>();
                foreach (var feature in features)
                {
                    result.Add(feature, new TreatmentResult(string.Empty, Control, null));
                }

                return new List<string>();
            }

            return _splitNameValidator.SplitNamesAreValid(features, method);
        }

        private void RecordImpressionAndTelemetry(TreatmentResult evaluationResult, Key key, string feature, string method)
        {
            RecordTelemetry(evaluationResult.Exception, method, evaluationResult.ElapsedMilliseconds);

            var impression = BuildImpression(evaluationResult, key, feature);

            if (impression != null) _impressionsManager.Track(new List<KeyImpression> { impression });
        }

        private void RecordTelemetry(bool exception, string method, long latency)
        {
            if (exception) RecordException(method);

            RecordLatency(method, latency);
        }

        private void RecordLatency(string method, long latency)
        {
            if (_telemetryEvaluationProducer == null) return;

            switch (method)
            {
                case nameof(GetTreatment):
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatment, Util.Metrics.Bucket(latency));
                    break;
                case nameof(GetTreatments):
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatments, Util.Metrics.Bucket(latency));
                    break;
                case nameof(GetTreatmentWithConfig):
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.TreatmentWithConfig, Util.Metrics.Bucket(latency));
                    break;
                case nameof(GetTreatmentsWithConfig):
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.TreatmentsWithConfig, Util.Metrics.Bucket(latency));
                    break;
                case nameof(Track):
                    _telemetryEvaluationProducer.RecordLatency(MethodEnum.Track, Util.Metrics.Bucket(latency));
                    break;
            }
        }

        private void RecordException(string method)
        {
            if (_telemetryEvaluationProducer == null) return;

            switch (method)
            {
                case nameof(GetTreatment):
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Treatment);
                    break;
                case nameof(GetTreatments):
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Treatments);
                    break;
                case nameof(GetTreatmentWithConfig):
                    _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentWithConfig);
                    break;
                case nameof(GetTreatmentsWithConfig):
                    _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentsWithConfig);
                    break;
                case nameof(Track):
                    _telemetryEvaluationProducer.RecordException(MethodEnum.Track);
                    break;
            }
        }

        private KeyImpression BuildImpression(TreatmentResult treatmentResult, Key key, string featureName)
        {
            if (Labels.SplitNotFound.Equals(treatmentResult.Label)) return null;

            return _impressionsManager.BuildImpression(key.matchingKey, featureName, treatmentResult.Treatment, CurrentTimeHelper.CurrentTimeMillis(), treatmentResult.ChangeNumber, LabelsEnabled ? treatmentResult.Label : null, key.bucketingKeyHadValue ? key.bucketingKey : null);
        }
        #endregion
    }
}