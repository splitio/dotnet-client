using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Common;
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
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public abstract class SplitClient : ISplitClient
    {
        protected readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitClient));

        protected readonly IKeyValidator _keyValidator;
        protected readonly ISplitNameValidator _splitNameValidator;
        protected readonly IEventTypeValidator _eventTypeValidator;
        protected readonly IEventPropertiesValidator _eventPropertiesValidator;
        protected readonly IWrapperAdapter _wrapperAdapter;
        protected readonly IConfigService _configService;

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
        protected ISyncManager _syncManager;
        protected IImpressionsLog _impressionsLog;
        protected IUniqueKeysTracker _uniqueKeysTracker;
        protected IImpressionListener _customerImpressionListener;
        protected IImpressionsManager _impressionsManager;
        protected IImpressionsSenderAdapter _impressionsSenderAdapter;
        protected IImpressionsCounter _impressionsCounter;
        protected IImpressionsObserver _impressionsObserver;
        protected IClientExtensionService _clientExtensionService;

        public SplitClient()
        {
            _wrapperAdapter = WrapperAdapter.Instance();
            _keyValidator = new KeyValidator();
            _splitNameValidator = new SplitNameValidator();
            _eventTypeValidator = new EventTypeValidator();
            _eventPropertiesValidator = new EventPropertiesValidator();
            _factoryInstantiationsService = FactoryInstantiationsService.Instance();
            _configService = new ConfigService(_wrapperAdapter);
            _statusManager = new InMemoryReadinessGatesCache();
            _tasksManager = new TasksManager(_statusManager);
        }

        #region Public Async Methods
        public async Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            var result = await GetTreatmentAsync(Enums.API.GetTreatmentAsync, new Key(key, null), feature, attributes);

            return result.Treatment;
        }

        public async Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsAsync, new Key(key, null), features, attributes);

            return results
                .ToDictionary(r => r.Key, r => r.Value.Treatment);
        }

        public async Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentAsync(Enums.API.GetTreatmentWithConfigAsync, new Key(key, null), feature, attributes);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsWithConfigAsync, new Key(key, null), features, attributes);

            return results
                .ToDictionary(r => r.Key, r => new SplitResult
                {
                    Treatment = r.Value.Treatment,
                    Config = r.Value.Config
                });
        }

        public virtual async Task DestroyAsync()
        {
            if (_statusManager.IsDestroyed()) return;

            _log.Info(Constants.Messages.InitDestroy);

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            await _syncManager.ShutdownAsync();

            _log.Info(Constants.Messages.Destroyed);
        }
        #endregion

        #region Public Sync Methods
        public virtual string GetTreatment(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var result = GetTreatmentSync(Enums.API.GetTreatment, key, feature, attributes);

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
            return GetTreatmentSync(Enums.API.GetTreatmentWithConfig, key, feature, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfig(new Key(key, null), features, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(Enums.API.GetTreatmentsWithConfig, key, features, attributes);

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
            var results = GetTreatmentsSync(Enums.API.GetTreatments, key, features, attributes);

            return results
                .ToDictionary(r => r.Key, r => r.Value.Treatment);
        }

        public virtual bool Track(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            if (_statusManager.IsDestroyed()) return false;

            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var keyResult = _keyValidator.IsValid(new Key(key, null), Enums.API.Track);
                var eventTypeResult = _eventTypeValidator.IsValid(eventType, nameof(eventType));
                var eventPropertiesResult = _eventPropertiesValidator.IsValid(properties);
                var trafficTypeResult = _trafficTypeValidator.IsValid(trafficType, Enums.API.Track);

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

                    _eventsLog.Log(new WrappedEvent
                    {
                        Event = eventToLog,
                        Size = eventPropertiesResult.EventSize
                    });

                    _clientExtensionService.RecordLatency(Enums.API.Track, clock.ElapsedMilliseconds);

                    return true;
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught trying to track an event", e);
                    _clientExtensionService.RecordException(Enums.API.Track);
                    return false;
                }
            }
        }

        public bool IsDestroyed()
        {
            return _statusManager.IsDestroyed();
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            _blockUntilReadyService.BlockUntilReady(blockMilisecondsUntilReady);
        }

        public ISplitManager GetSplitManager()
        {
            return _manager;
        }

        public virtual void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _log.Info(Constants.Messages.InitDestroy);

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            _syncManager.Shutdown();

            _log.Info(Constants.Messages.Destroyed);
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
            var trackerConfig = new ComponentConfig(config.UniqueKeysCacheMaxSize, config.UniqueKeysBulkSize);

            var mtksTask = _tasksManager.NewPeriodicTask(Enums.Task.MTKsSender, config.UniqueKeysRefreshRate * 1000);
            var cacheLongTermCleaningTask = _tasksManager.NewPeriodicTask(Enums.Task.CacheLongTermCleaning, Constants.Gral.IntervalToClearLongTermCache);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Enums.Task.MtkSendBulkData);

            _uniqueKeysTracker = new UniqueKeysTracker(trackerConfig, filterAdapter, trackerCache, _impressionsSenderAdapter, mtksTask, cacheLongTermCleaningTask, sendBulkDataTask);
        }

        protected void BuildImpressionsCounter(BaseConfig config)
        {
            if (config.ImpressionsMode == ImpressionsMode.Debug)
            {
                _impressionsCounter = new NoopImpressionsCounter();
                return;
            }

            var trackerConfig = new ComponentConfig(config.ImpressionsCounterCacheMaxSize, config.ImpressionsCountBulkSize);
            var task = _tasksManager.NewPeriodicTask(Enums.Task.ImpressionsCountSender, config.ImpressionsCounterRefreshRate * 1000);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Enums.Task.ImpressionCounterSendBulkData);

            _impressionsCounter = new ImpressionsCounter(trackerConfig, _impressionsSenderAdapter, task, sendBulkDataTask);
        }

        protected void BuildClientExtension()
        {
            _clientExtensionService = new ClientExtensionService(_blockUntilReadyService, _statusManager, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer);
        }
        #endregion

        #region Private Async Methods
        private async Task<SplitResult> GetTreatmentAsync(Enums.API method, Key key, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            if (!_clientExtensionService.TreatmentValidations(method, key, featureFlagName, _log, out var ffNameSanitized))
                return new SplitResult(Constants.Gral.Control, null);

            var evaluatorResult = await _evaluator.EvaluateFeatureAsync(key, ffNameSanitized, attributes);
            
            if (evaluatorResult.Exception) _clientExtensionService.RecordException(method);

            _clientExtensionService.RecordLatency(method, evaluatorResult.ElapsedMilliseconds);

            var impression = _impressionsManager.Build(evaluatorResult, key, ffNameSanitized);
            
            if (impression != null) await _impressionsManager.TrackAsync(new List<KeyImpression> { impression });

            return new SplitResult(evaluatorResult.Treatment, evaluatorResult.Config);
        }

        private async Task<Dictionary<string, TreatmentResult>> GetTreatmentsAsync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var results = await _evaluator.EvaluateFeaturesAsync(key, features, attributes);

            if (results.Exception) _clientExtensionService.RecordException(method);

            _clientExtensionService.RecordLatency(method, results.ElapsedMilliseconds);

            var toReturn = ParseTreatmentsAndRecordTelemetry(method, results, key, out var impressions);

            if (impressions.Any()) await _impressionsManager.TrackAsync(impressions);

            return toReturn;
        }
        #endregion

        #region Private Methods
        private SplitResult GetTreatmentSync(Enums.API method, Key key, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            if (!_clientExtensionService.TreatmentValidations(method, key, featureFlagName, _log, out var ffNameSanitized))
                return new SplitResult(Constants.Gral.Control, null);

            var evaluatorResult = _evaluator.EvaluateFeature(key, ffNameSanitized, attributes);

            if (evaluatorResult.Exception) _clientExtensionService.RecordException(method);

            _clientExtensionService.RecordLatency(method, evaluatorResult.ElapsedMilliseconds);

            var impression = _impressionsManager.Build(evaluatorResult, key, ffNameSanitized);
            
            if (impression != null) _impressionsManager.Track(new List<KeyImpression> { impression });

            return new SplitResult(evaluatorResult.Treatment, evaluatorResult.Config);
        }

        private Dictionary<string, TreatmentResult> GetTreatmentsSync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out Dictionary<string, TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var results = _evaluator.EvaluateFeatures(key, features, attributes);

            if (results.Exception) _clientExtensionService.RecordException(method);

            _clientExtensionService.RecordLatency(method, results.ElapsedMilliseconds);

            var toReturn = ParseTreatmentsAndRecordTelemetry(method, results, key, out var impressions);

            if (impressions.Any()) _impressionsManager.Track(impressions);

            return toReturn;
        }

        private Dictionary<string, TreatmentResult> ParseTreatmentsAndRecordTelemetry(Enums.API method, MultipleEvaluatorResult results, Key key, out List<KeyImpression> impressionsQueue)
        {
            impressionsQueue = new List<KeyImpression>();
            var treatmentsForFeatures = new Dictionary<string, TreatmentResult>();

            foreach (var treatmentResult in results.TreatmentResults)
            {
                treatmentsForFeatures.Add(treatmentResult.Key, treatmentResult.Value);

                var impression = _impressionsManager.Build(treatmentResult.Value, key, treatmentResult.Key);
                if (impression != null) impressionsQueue.Add(impression);
            }

            return treatmentsForFeatures;
        }
        #endregion
    }
}