﻿using Splitio.Constants;
using Splitio.Domain;
using Splitio.Enums.Extensions;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Evaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Filters;
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
        protected readonly IFlagSetsValidator _flagSetsValidator;
        protected readonly string ApiKey;

        protected ISplitManager _manager;
        protected IEventsLog _eventsLog;
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
        protected IImpressionsLog _impressionsLog;
        protected IUniqueKeysTracker _uniqueKeysTracker;
        protected IImpressionListener _customerImpressionListener;
        protected IImpressionsManager _impressionsManager;
        protected IImpressionsSenderAdapter _impressionsSenderAdapter;
        protected IImpressionsCounter _impressionsCounter;
        protected IImpressionsObserver _impressionsObserver;
        protected IClientExtensionService _clientExtensionService;
        protected IFlagSetsFilter _flagSetsFilter;

        public SplitClient(string apikey)
        {
            ApiKey = apikey;

            _wrapperAdapter = WrapperAdapter.Instance();
            _keyValidator = new KeyValidator();
            _splitNameValidator = new SplitNameValidator();
            _eventTypeValidator = new EventTypeValidator();
            _eventPropertiesValidator = new EventPropertiesValidator();
            _factoryInstantiationsService = FactoryInstantiationsService.Instance();
            _flagSetsValidator = new FlagSetsValidator();
            _configService = new ConfigService(_wrapperAdapter, _flagSetsValidator, new SdkMetadataValidator());
            _statusManager = new InMemoryReadinessGatesCache();
            _tasksManager = new TasksManager(_statusManager);
        }

        #region GetTreatment
        public async Task<string> GetTreatmentAsync(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = await GetTreatmentsAsync(Enums.API.GetTreatmentAsync, key, new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult).Treatment;
        }

        public async Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentAsync(new Key(key, null), feature, attributes);
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
        public async Task<Dictionary<string, string>> GetTreatmentsAsync(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsAsync, key, features, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsAsync(new Key(key, null), features, attributes);
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
        public async Task<SplitResult> GetTreatmentWithConfigAsync(Key key, string feature, Dictionary<string, object> attributes = null)
        {
            var evaluationResult = await GetTreatmentsAsync(Enums.API.GetTreatmentWithConfigAsync, key, new List<string> { feature }, attributes);

            return TreatmentWithConfig(evaluationResult);
        }

        public async Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentWithConfigAsync(new Key(key, null), feature, attributes);
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
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsAsync(Enums.API.GetTreatmentsWithConfigAsync, key, features, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsWithConfigAsync(new Key(key, null), features, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsSync(Enums.API.GetTreatmentsWithConfig, key, features, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfig(string key, List<string> features, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfig(new Key(key, null), features, attributes);
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSets
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsWithConfigByFlagSetsAsync(new Key(key, null), flagSets, attributes);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsByFlagSetsAsync(Enums.API.GetTreatmentsWithConfigByFlagSetsAsync, key, flagSets, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSets(string key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfigByFlagSets(new Key(key, null), flagSets, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSets(Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsByFlagSets(Enums.API.GetTreatmentsWithConfigByFlagSets, key, flagSets, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }
        #endregion

        #region GetTreatmentsByFlagSets
        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(string key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsByFlagSetsAsync(new Key(key, null), flagSets, attributes);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetsAsync(Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsByFlagSetsAsync(Enums.API.GetTreatmentsByFlagSetsAsync, key, flagSets, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSets(string key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsByFlagSets(new Key(key, null), flagSets, attributes);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSets(Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsByFlagSets(Enums.API.GetTreatmentsByFlagSets, key, flagSets, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSet
        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetAsync(string key, string flagSet, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsWithConfigByFlagSetAsync(new Key(key, null), flagSet, attributes);
        }

        public async Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigByFlagSetAsync(Key key, string flagSet, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsByFlagSetsAsync(Enums.API.GetTreatmentsWithConfigByFlagSetAsync, key, new List<string> { flagSet }, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSet(string key, string flagSet, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsWithConfigByFlagSet(new Key(key, null), flagSet, attributes);
        }

        public Dictionary<string, SplitResult> GetTreatmentsWithConfigByFlagSet(Key key, string flagSet, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsByFlagSets(Enums.API.GetTreatmentsWithConfigByFlagSet, key, new List<string> { flagSet }, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => new SplitResult(r.Treatment, r.Config));
        }
        #endregion

        #region GetTreatmentsByFlagSet
        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetAsync(string key, string flagSet, Dictionary<string, object> attributes = null)
        {
            return await GetTreatmentsByFlagSetAsync(new Key(key, null), flagSet, attributes);
        }

        public async Task<Dictionary<string, string>> GetTreatmentsByFlagSetAsync(Key key, string flagSet, Dictionary<string, object> attributes = null)
        {
            var results = await GetTreatmentsByFlagSetsAsync(Enums.API.GetTreatmentsByFlagSetAsync, key, new List<string> { flagSet }, attributes);

            return results.ToDictionary(r => r.FeatureFlagName, r => r.Treatment);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSet(string key, string flagSet, Dictionary<string, object> attributes = null)
        {
            return GetTreatmentsByFlagSet(new Key(key, null), flagSet, attributes);
        }

        public Dictionary<string, string> GetTreatmentsByFlagSet(Key key, string flagSet, Dictionary<string, object> attributes = null)
        {
            var results = GetTreatmentsByFlagSets(Enums.API.GetTreatmentsByFlagSet, key, new List<string> { flagSet }, attributes);

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
            var trackerConfig = new ComponentConfig(config.ImpressionsCounterCacheMaxSize, config.ImpressionsCountBulkSize);
            var task = _tasksManager.NewPeriodicTask(Enums.Task.ImpressionsCountSender, config.ImpressionsCounterRefreshRate * 1000);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Enums.Task.ImpressionCounterSendBulkData);

            _impressionsCounter = new ImpressionsCounter(trackerConfig, _impressionsSenderAdapter, task, sendBulkDataTask);
        }

        protected void BuildClientExtension()
        {
            _clientExtensionService = new ClientExtensionService(_blockUntilReadyService, _statusManager, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer, _eventTypeValidator, _eventPropertiesValidator, _trafficTypeValidator, _flagSetsValidator, _flagSetsFilter);
        }

        protected void BuildFlagSetsFilter(HashSet<string> sets)
        {
            _flagSetsFilter = new FlagSetsFilter(sets);
        }
        #endregion

        #region Private Async Methods
        private async Task<List<TreatmentResult>> GetTreatmentsAsync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes)
        {
            try
            {
                features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

                if (controlTreatments != null) return controlTreatments;

                var treatments = await _evaluator.EvaluateFeaturesAsync(method, key, features, attributes);

                await TrackImpressionsAsync(treatments, key);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return _clientExtensionService.ReturnControl(features);
            }
        }

        private async Task<List<TreatmentResult>> GetTreatmentsByFlagSetsAsync(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes)
        {
            try
            {
                flagSets = _clientExtensionService.FlagSetsValidations(method, key, flagSets, _log);

                var treatments = await _evaluator.EvaluateFeaturesByFlagSetsAsync(method, key, flagSets, attributes);

                await TrackImpressionsAsync(treatments, key);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return new List<TreatmentResult>();
            }
        }

        private async Task TrackImpressionsAsync(List<TreatmentResult> evaluatorResults, Key key)
        {
            var impressions = BuildAndGetImpressions(evaluatorResults, key);

            if (impressions.Any())
                await _impressionsManager.TrackAsync(impressions);
        }
        #endregion

        #region Private Methods
        private List<TreatmentResult> GetTreatmentsSync(Enums.API method, Key key, List<string> features, Dictionary<string, object> attributes = null)
        {
            try
            {
                features = _clientExtensionService.TreatmentsValidations(method, key, features, _log, out List<TreatmentResult> controlTreatments);

                if (controlTreatments != null) return controlTreatments;

                var treatments = _evaluator.EvaluateFeatures(method, key, features, attributes);

                TrackImpressions(treatments, key);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return _clientExtensionService.ReturnControl(features);
            }
        }

        private List<TreatmentResult> GetTreatmentsByFlagSets(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes)
        {
            try
            {
                flagSets = _clientExtensionService.FlagSetsValidations(method, key, flagSets, _log);

                var treatments = _evaluator.EvaluateFeaturesByFlagSets(method, key, flagSets, attributes);

                TrackImpressions(treatments, key);

                return treatments;
            }
            catch (Exception ex)
            {
                _telemetryEvaluationProducer.RecordException(method.ConvertToMethodEnum());

                _log.Warn("Something went wrong evaluating features.", ex);
                return new List<TreatmentResult>();
            }
        }

        private List<KeyImpression> BuildAndGetImpressions(List<TreatmentResult> treatments, Key key)
        {
            var impressions = new List<KeyImpression>();

            foreach (var treatment in treatments)
            {
                var impression = _impressionsManager.Build(treatment, key);

                if (impression != null) impressions.Add(impression);
            }

            return impressions;
        }

        private void TrackImpressions(List<TreatmentResult> treatments, Key key)
        {
            try
            {
                var impressions = BuildAndGetImpressions(treatments, key);

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
        #endregion
    }
}