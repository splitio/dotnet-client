using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using Splitio.Commons.Shared.Constants;
using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Telemetry.Storages;
using Splitio.Commons.Impressions.Interfaces;
using Splitio.Domain;
using Splitio.Enums.Extensions;
using Splitio.Commons.Cache.Filter;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Splitio.Commons.Engine.Filters;
using Splitio.Commons.Cache.Interfaces;
using Splitio.Commons.Engine.Evaluator;
using Splitio.Commons.Tasks;
using Splitio.Commons.SdkEvents;
using Splitio.Services.Cache.Classes;

namespace Splitio.Services.Client.Classes
{
    public abstract class SplitClient : ISplitClient
    {
        protected readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitClient));

        protected readonly IKeyValidator _keyValidator;
        protected readonly ISplitNameValidator _splitNameValidator;
        protected readonly IEventTypeValidator _eventTypeValidator;
        protected readonly IPropertiesValidator _propertiesValidator;
        protected readonly IWrapperAdapter _wrapperAdapter;
        protected readonly IConfigService _configService;
        protected readonly IFlagSetsValidator _flagSetsValidator;
        protected readonly string ApiKey;

        protected ISplitManager _manager;
        protected IEventsCache _eventsLog;
        protected ITrafficTypeValidator _trafficTypeValidator;
        protected IBlockUntilReadyService _blockUntilReadyService;
        protected IFactoryInstantiationsService _factoryInstantiationsService;
        protected IParser<Split, ParsedSplit> _splitParser;
        protected IParser<RuleBasedSegmentDto, RuleBasedSegment> _rbsParser;
        protected IEvaluator _evaluator;
        protected ITelemetryEvaluationProducer _telemetryEvaluationProducer;
        protected ITelemetryInitProducer _telemetryInitProducer;
        protected ITasksManager _tasksManager;
        protected IStatusManager _statusManager;
        protected ISyncManager _syncManager;
        protected IImpressionsCache _impressionsLog;
        protected IUniqueKeysTracker _uniqueKeysTracker;
        protected IImpressionListener _customerImpressionListener;
        protected IImpressionsManager _impressionsManager;
        protected IImpressionsSenderAdapter _impressionsSenderAdapter;
        protected IImpressionsCounter _impressionsCounter;
        protected IImpressionsObserver _impressionsObserver;
        protected IClientExtensionService _clientExtensionService;
        protected IFlagSetsFilter _flagSetsFilter;
        protected IFallbackTreatmentCalculator _fallbackTreatmentCalculator;
        protected IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;
        protected IInternalEventsTask _internalEventsTask;
        private EventHandler<EventMetadata> SdkReadyEvent;

        public event EventHandler<EventMetadata> SdkReady
        {
            add
            {
                SdkReadyEvent = (EventHandler<EventMetadata>)Delegate.Combine(SdkReadyEvent, value);
                if (_eventsManager.EventAlreadyTriggered(SdkEvent.SdkReady))
                {
                    SdkReadyEvent.Invoke(this, null);
                }
            }

            remove
            {
                SdkReadyEvent = (EventHandler<EventMetadata>)Delegate.Remove(SdkReadyEvent, value);
            }
        }
        public event EventHandler<EventMetadata> SdkUpdate;

        protected SplitClient(string apikey)
        {
            ApiKey = apikey;
            _wrapperAdapter = WrapperAdapter.Instance();
            _keyValidator = new KeyValidator();
            _splitNameValidator = new SplitNameValidator();
            _eventTypeValidator = new EventTypeValidator();
            _propertiesValidator = new PropertiesValidator();
            _factoryInstantiationsService = FactoryInstantiationsService.Instance();
            _flagSetsValidator = new FlagSetsValidator();
            _configService = new ConfigService(_wrapperAdapter, _flagSetsValidator, new SdkMetadataValidator());
        }

        #region GetTreatment
        public async Task<string> GetTreatmentAsync(Key key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> evaluationResult = await GetTreatmentsAsync(Commons.Dto.API.GetTreatmentAsync, key, new List<string> { feature }, attributes, evaluationOptions);

            return TreatmentWithConfig(evaluationResult).Treatment;
        }

        public async Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentAsync(new Key(key, null), feature, attributes, evaluationOptions);
        }

        public virtual string GetTreatment(Key key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> evaluationResult = GetTreatmentsSync(Commons.Dto.API.GetTreatment, key, new List<string> { feature }, attributes, evaluationOptions);

            return TreatmentWithConfig(evaluationResult).Treatment;
        }

        public virtual string GetTreatment(string key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatment(new Key(key, null), feature, attributes, evaluationOptions);
        }
        #endregion

        #region GetTreatments
        public async Task<Dictionary<string, string>> GetTreatmentsAsync(Key key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsAsync(Commons.Dto.API.GetTreatmentsAsync, key, features, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsAsync(new Key(key, null), features, attributes, evaluationOptions);
        }

        public Dictionary<string, string> GetTreatments(Key key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsSync(Commons.Dto.API.GetTreatments, key, features, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatments(string key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatments(new Key(key, null), features, attributes, evaluationOptions);
        }
        #endregion

        #region GetTreatmentWithConfig
        public async Task<SplitResult> GetTreatmentWithConfigAsync(Key key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> evaluationResult = await GetTreatmentsAsync(Commons.Dto.API.GetTreatmentWithConfigAsync, key, new List<string> { feature }, attributes, evaluationOptions);

            return TreatmentWithConfig(evaluationResult);
        }

        public async Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentWithConfigAsync(new Key(key, null), feature, attributes, evaluationOptions);
        }

        public SplitResult GetTreatmentWithConfig(Key key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> evaluationResult = GetTreatmentsSync(Commons.Dto.API.GetTreatmentWithConfig, key, new List<string> { feature }, attributes, evaluationOptions);

            return TreatmentWithConfig(evaluationResult);
        }

        public SplitResult GetTreatmentWithConfig(string key, string feature, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentWithConfig(new Key(key, null), feature, attributes, evaluationOptions);
        }
        #endregion

        #region GetTreatmentsWithConfig
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(Key key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsAsync(Commons.Dto.API.GetTreatmentsWithConfigAsync, key, features, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsWithConfigAsync(new Key(key, null), features, attributes, evaluationOptions);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(Key key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsSync(Commons.Dto.API.GetTreatmentsWithConfig, key, features, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(string key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentsWithConfig(new Key(key, null), features, attributes, evaluationOptions);
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSets
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsWithConfigByFlagSetsAsync(new Key(key, null), flagSets, attributes, evaluationOptions);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsByFlagSetsAsync(Commons.Dto.API.GetTreatmentsWithConfigByFlagSetsAsync, key, flagSets, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSets(string key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentsWithConfigByFlagSets(new Key(key, null), flagSets, attributes, evaluationOptions);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSets(Key key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsByFlagSets(Commons.Dto.API.GetTreatmentsWithConfigByFlagSets, key, flagSets, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }
        #endregion

        #region GetTreatmentsByFlagSets
        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsByFlagSetsAsync(new Key(key, null), flagSets, attributes, evaluationOptions);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsByFlagSetsAsync(Commons.Dto.API.GetTreatmentsByFlagSetsAsync, key, flagSets, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSets(string key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentsByFlagSets(new Key(key, null), flagSets, attributes, evaluationOptions);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSets(Key key, List<string> flagSets, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsByFlagSets(Commons.Dto.API.GetTreatmentsByFlagSets, key, flagSets, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSet
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetAsync(string key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsWithConfigByFlagSetAsync(new Key(key, null), flagSet, attributes, evaluationOptions);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetAsync(Key key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsByFlagSetsAsync(Commons.Dto.API.GetTreatmentsWithConfigByFlagSetAsync, key, new List<string> { flagSet }, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSet(string key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentsWithConfigByFlagSet(new Key(key, null), flagSet, attributes, evaluationOptions);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSet(Key key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsByFlagSets(Commons.Dto.API.GetTreatmentsWithConfigByFlagSet, key, new List<string> { flagSet }, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }
        #endregion

        #region GetTreatmentsByFlagSet
        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetAsync(string key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return await GetTreatmentsByFlagSetAsync(new Key(key, null), flagSet, attributes, evaluationOptions);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetAsync(Key key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = await GetTreatmentsByFlagSetsAsync(Commons.Dto.API.GetTreatmentsByFlagSetAsync, key, new List<string> { flagSet }, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSet(string key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            return GetTreatmentsByFlagSet(new Key(key, null), flagSet, attributes, evaluationOptions);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSet(Key key, string flagSet, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            List<TreatmentResult> results = GetTreatmentsByFlagSets(Commons.Dto.API.GetTreatmentsByFlagSet, key, new List<string> { flagSet }, attributes, evaluationOptions);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
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
            if (_eventsManager != null)
            {
                _internalEventsTask.Stop();
                UnregisterEvents();
            }
            _log.Info(Messages.Destroyed);
        }

        public virtual void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _log.Info(Messages.InitDestroy);

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            _syncManager.Shutdown();
            if (_eventsManager != null)
            {
                _internalEventsTask.Stop();
                UnregisterEvents();
            }
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
                _clientExtensionService.RecordLatency(Commons.Dto.API.Track, clock.ElapsedMilliseconds);

                return true;
            }
            catch (Exception e)
            {
                _log.Error("Exception caught trying to track an event", e);
                _clientExtensionService.RecordException(Commons.Dto.API.Track);

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
                await _clientExtensionService.RecordLatencyAsync(Commons.Dto.API.Track, clock.ElapsedMilliseconds);

                return true;
            }
            catch (Exception e)
            {
                _log.Error("Exception caught trying to track an event", e);
                await _clientExtensionService.RecordExceptionAsync(Commons.Dto.API.Track);

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
        protected void BuildStatusAndTaskManager()
        {
            _statusManager = new InMemoryReadinessGatesCache(_internalEventsTask);
            _tasksManager = new TasksManager(_statusManager);
        }

        protected void BuildUniqueKeysTracker(BaseConfig config)
        {
            var bloomFilter = new BloomFilter(config.BfExpectedElements, config.BfErrorRate);
            var filterAdapter = new FilterAdapter(bloomFilter);
            var trackerCache = new ConcurrentDictionary<string, HashSet<string>>();
            var trackerConfig = new ComponentConfig(config.UniqueKeysCacheMaxSize, config.UniqueKeysBulkSize);

            var mtksTask = _tasksManager.NewPeriodicTask(Commons.Dto.SplitTaskName.MTKsSender, config.UniqueKeysRefreshRate * 1000);
            var cacheLongTermCleaningTask = _tasksManager.NewPeriodicTask(Commons.Dto.SplitTaskName.CacheLongTermCleaning, Gral.IntervalToClearLongTermCache);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Commons.Dto.SplitTaskName.MtkSendBulkData);

            _uniqueKeysTracker = new UniqueKeysTracker(trackerConfig, filterAdapter, trackerCache, _impressionsSenderAdapter, mtksTask, cacheLongTermCleaningTask, sendBulkDataTask);
        }

        protected void BuildImpressionsCounter(BaseConfig config)
        {
            var trackerConfig = new ComponentConfig(config.ImpressionsCounterCacheMaxSize, config.ImpressionsCountBulkSize);
            var task = _tasksManager.NewPeriodicTask(Commons.Dto.SplitTaskName.ImpressionsCountSender, config.ImpressionsCounterRefreshRate * 1000);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Commons.Dto.SplitTaskName.ImpressionCounterSendBulkData);

            _impressionsCounter = new ImpressionsCounter(trackerConfig, _impressionsSenderAdapter, task, sendBulkDataTask);
        }

        protected void BuildClientExtension()
        {
            _clientExtensionService = new ClientExtensionService(_blockUntilReadyService, _statusManager, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer, _eventTypeValidator, _propertiesValidator, _trafficTypeValidator, _flagSetsValidator, _flagSetsFilter, _fallbackTreatmentCalculator);
        }

        protected void BuildFlagSetsFilter(HashSet<string> sets)
        {
            _flagSetsFilter = new FlagSetsFilter(sets);
        }

        protected void BuildFallbackCalculator(FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration)
        {
            _fallbackTreatmentCalculator = new FallbackTreatmentCalculator(fallbackTreatmentsConfiguration);
        }

        protected void BuildEventsManager()
        {
            _eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            _internalEventsTask = new InternalEventsTask(_eventsManager, new SplitQueue<SdkEventNotification>());
            _internalEventsTask.Start();
            RegisterEvents();
        }
        #endregion

        #region Private Async Methods
        private async Task<List<TreatmentResult>> GetTreatmentsAsync(Commons.Dto.API method, Key key, List<string> features, Dictionary<string, object> attributes, EvaluationOptions evaluationOptions)
        {
            try
            {
                features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

                if (controlTreatments != null) return controlTreatments;

                List<TreatmentResult> treatments = await _evaluator.EvaluateFeaturesAsync(method, key, features, attributes);

                await TrackImpressionsAsync(treatments, key, evaluationOptions);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return _clientExtensionService.ReturnControl(features);
            }
        }

        private async Task<List<TreatmentResult>> GetTreatmentsByFlagSetsAsync(Commons.Dto.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes, EvaluationOptions evaluationOptions)
        {
            try
            {
                flagSets = _clientExtensionService.FlagSetsValidations(method, key, flagSets, _log);

                List<TreatmentResult> treatments = await _evaluator.EvaluateFeaturesByFlagSetsAsync(method, key, flagSets, attributes);

                await TrackImpressionsAsync(treatments, key, evaluationOptions);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return new List<TreatmentResult>();
            }
        }

        private async Task TrackImpressionsAsync(List<TreatmentResult> evaluatorResults, Key key, EvaluationOptions evaluationOptions)
        {
            if (evaluationOptions == null)
            {
                evaluationOptions = new EvaluationOptions();
            }
            var impressions = BuildAndGetImpressions(evaluatorResults, key, evaluationOptions.Properties);

            if (impressions.Any())
                await _impressionsManager.TrackAsync(impressions);
        }
        #endregion

        #region Private Methods
        private void RegisterEvents()
        {
            _eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            _eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);
        }
        private void UnregisterEvents()
        {
            _eventsManager.Unregister(SdkEvent.SdkReady);
            _eventsManager.Unregister(SdkEvent.SdkUpdate);
        }
        private List<TreatmentResult> GetTreatmentsSync(Commons.Dto.API method, Key key, List<string> features, Dictionary<string, object> attributes = null, EvaluationOptions evaluationOptions = null)
        {
            try
            {
                features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

                if (controlTreatments != null) return controlTreatments;

                List<TreatmentResult> treatments = _evaluator.EvaluateFeatures(method, key, features, attributes);

                TrackImpressions(treatments, key, evaluationOptions);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return _clientExtensionService.ReturnControl(features);
            }
        }

        private List<TreatmentResult> GetTreatmentsByFlagSets(Commons.Dto.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes, EvaluationOptions evaluationOptions = null)
        {
            try
            {
                flagSets = _clientExtensionService.FlagSetsValidations(method, key, flagSets, _log);

                List<TreatmentResult> treatments = _evaluator.EvaluateFeaturesByFlagSets(method, key, flagSets, attributes);

                TrackImpressions(treatments, key, evaluationOptions);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return new List<TreatmentResult>();
            }
        }

        private List<KeyImpression> BuildAndGetImpressions(List<TreatmentResult> treatments, Key key, Dictionary<string, object> properties = null)
        {
            var impressions = new List<KeyImpression>();

            foreach (var treatment in treatments)
            {
                var impression = _impressionsManager.Build(treatment, key, properties);

                if (impression != null) impressions.Add(impression);
            }

            return impressions;
        }

        private void TrackImpressions(List<TreatmentResult> treatments, Key key, EvaluationOptions evaluationOptions)
        {
            try
            {
                if (evaluationOptions == null)
                {
                    evaluationOptions = new EvaluationOptions();
                }
                var impressions = BuildAndGetImpressions(treatments, key, evaluationOptions.Properties);

                if (impressions.Any())
                    _impressionsManager.Track(impressions);
            }
            catch (Exception ex)
            {
                _log.Warn("Something went wrong tracking impressions.", ex);
            }
        }

        private static SplitResult TreatmentWithConfig(List<TreatmentResult> results)
        {
            if (!results.Any()) return new SplitResult(Gral.Control, null);

            var result = results.FirstOrDefault();

            if (result == null) return new SplitResult(Gral.Control, null);

            return new SplitResult(result.Treatment, result.Config);
        }

        private void TriggerSdkReady(EventMetadata metaData)
        {
            SdkReadyEvent?.Invoke(this, metaData);
        }

        private void TriggerSdkUpdate(EventMetadata metaData)
        {
            SdkUpdate?.Invoke(this, metaData);
        }
        #endregion
    }
}