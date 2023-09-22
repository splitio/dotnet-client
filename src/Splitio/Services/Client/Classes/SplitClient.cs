using Splitio.CommonLibraries;
using Splitio.Constants;
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
using System.Diagnostics;
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
        protected readonly string ApiKey;

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

        public SplitClient(string apikey)
        {
            ApiKey = apikey;

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

        #region GetTreatment
        public async Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = await GetTreatmentsAsync(Enums.API.GetTreatmentAsync, new Key(key, null), new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult).Treatment;
        }

        public virtual string GetTreatment(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = GetTreatmentsSync(Enums.API.GetTreatment, key, new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult).Treatment;
        }

        public virtual string GetTreatment(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return GetTreatment(new Key(key, null), feature, attributes);
        }
        #endregion

        #region GetTreatments
        public async Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsAsync, new Key(key, null), features, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatments(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(Enums.API.GetTreatments, key, features, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatments(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatments(new Key(key, null), features, attributes);
        }
        #endregion

        #region GetTreatmentWithConfig
        public async Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = await GetTreatmentsAsync(Enums.API.GetTreatmentWithConfigAsync, new Key(key, null), new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult);
        }

        public SplitResult GetTreatmentWithConfig(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = GetTreatmentsSync(Enums.API.GetTreatmentWithConfig, key, new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult);
        }

        public SplitResult GetTreatmentWithConfig(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentWithConfig(new Key(key, null), feature, attributes);
        }
        #endregion

        #region GetTreatmentsWithConfig
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsWithConfigAsync, new Key(key, null), features, attributes);

            return results
                .ToDictionary(r => r.FeatureFlagName, r => new SplitResult
                {
                    Treatment = r.Treatment,
                    Config = r.Config
                });
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(Enums.API.GetTreatmentsWithConfig, key, features, attributes);

            return results
                .ToDictionary(r => r.FeatureFlagName, r => new SplitResult
                {
                    Treatment = r.Treatment,
                    Config = r.Config
                });
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfig(new Key(key, null), features, attributes);
        }
        #endregion

        #region Destroy
        public virtual async Task DestroyAsync()
        {
            if (_statusManager.IsDestroyed()) return;

            _log.Info(Messages.InitDestroy);

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            await _syncManager.ShutdownAsync();

            _log.Info(Messages.Destroyed);
        }

        public virtual void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _log.Info(Messages.InitDestroy);

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            _syncManager.Shutdown();

            _log.Info(Messages.Destroyed);
        }
        #endregion

        #region Track
        public virtual bool Track(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            if (_statusManager.IsDestroyed()) return false;

            var clock = new Stopwatch();
            clock.Start();

            try
            {
                if (!_clientExtensionService.TrackValidations(key, trafficType, eventType, value, properties, out var wrappedEvent)) return false;

                _eventsLog.Log(wrappedEvent);
                _clientExtensionService.RecordLatency(Enums.API.Track, clock.ElapsedMilliseconds);

                return true;
            }
            catch (Exception e)
            {
                _log.Error("Exception caught trying to track an event", e);
                _clientExtensionService.RecordException(Enums.API.Track);

                return false;
            }
            finally { clock.Stop(); }
        }

        public virtual async Task<bool> TrackAsync(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            if (_statusManager.IsDestroyed()) return false;

            var clock = new Stopwatch();
            clock.Start();

            try
            {
                if (!_clientExtensionService.TrackValidations(key, trafficType, eventType, value, properties, out var wrappedEvent)) return false;

                await _eventsLog.LogAsync(wrappedEvent);
                await _clientExtensionService.RecordLatencyAsync(Enums.API.Track, clock.ElapsedMilliseconds);

                return true;
            }
            catch (Exception e)
            {
                _log.Error("Exception caught trying to track an event", e);
                await _clientExtensionService.RecordExceptionAsync(Enums.API.Track);

                return false;
            }
            finally { clock.Stop(); }
        }
        #endregion

        #region Public Methods
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
            _clientExtensionService = new ClientExtensionService(_blockUntilReadyService, _statusManager, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer, _eventTypeValidator, _eventPropertiesValidator, _trafficTypeValidator);
        }
        #endregion

        #region Private Async Methods
        private async Task<List<TreatmentResult>> GetTreatmentsAsync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes)
        {
            features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var evaluatorResult = await _evaluator.EvaluateFeaturesAsync(key, features, attributes);

            if (evaluatorResult.Exception) await _clientExtensionService.RecordExceptionAsync(method);

            await _clientExtensionService.RecordLatencyAsync(method, evaluatorResult.ElapsedMilliseconds);

            if (BuildAndGetImpressions(evaluatorResult, key, out var impressions))
                await _impressionsManager.TrackAsync(impressions);

            return evaluatorResult.Results;
        }
        #endregion

        #region Private Methods
        private List<TreatmentResult> GetTreatmentsSync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

            if (controlTreatments != null) return controlTreatments;

            var evaluatorResult = _evaluator.EvaluateFeatures(key, features, attributes);

            if (evaluatorResult.Exception) _clientExtensionService.RecordException(method);

            _clientExtensionService.RecordLatency(method, evaluatorResult.ElapsedMilliseconds);

            if (BuildAndGetImpressions(evaluatorResult, key, out var impressions))
                _impressionsManager.Track(impressions);

            return evaluatorResult.Results;
        }

        private bool BuildAndGetImpressions(MultipleEvaluatorResult evaluatorResult, Key key, out List<KeyImpression> impressions)
        {
            impressions = new List<KeyImpression>();

            foreach (var treatmentResult in evaluatorResult.Results)
            {
                var impression = _impressionsManager.Build(treatmentResult, key);

                if (impression != null) impressions.Add(impression);
            }

            return impressions.Any();
        }

        private static SplitResult TreatmentWithConfig(List<TreatmentResult> results)
        {
            if (!results.Any()) return new SplitResult(Gral.Control, null);

            var result = results.FirstOrDefault();

            if (result == null) return new SplitResult(Gral.Control, null);

            return new SplitResult(result.Treatment, result.Config);
        }
        #endregion
    }
}