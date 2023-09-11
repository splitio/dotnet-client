﻿using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Linq;
using System.Threading;

namespace Splitio.Telemetry.Common
{
    public class TelemetrySyncTask : ITelemetrySyncTask
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(TelemetrySyncTask));

        private readonly ITelemetryStorageConsumer _telemetryStorageConsumer;
        private readonly ITelemetryAPI _telemetryAPI;
        private readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private readonly ISegmentCache _segmentCache;        
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ITasksManager _tasksManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SelfRefreshingConfig _configurationOptions;
        private readonly object _lock = new object();

        private bool _running;

        public TelemetrySyncTask(ITelemetryStorageConsumer telemetryStorage,
            ITelemetryAPI telemetryAPI,
            IFeatureFlagCacheConsumer featureFlagCacheConsumer,
            ISegmentCache segmentCache,
            SelfRefreshingConfig configurationOptions,
            IFactoryInstantiationsService factoryInstantiationsService,
            IWrapperAdapter wrapperAdapter,
            ITasksManager tasksManager)
        {
            _telemetryStorageConsumer = telemetryStorage;
            _telemetryAPI = telemetryAPI;            
            _featureFlagCacheConsumer = featureFlagCacheConsumer;
            _segmentCache = segmentCache;
            _configurationOptions = configurationOptions;
            _factoryInstantiationsService = factoryInstantiationsService;
            _wrapperAdapter = wrapperAdapter;
            _tasksManager = tasksManager;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        #region Public Methods
        public void Start()
        {
            lock (_lock)
            {
                if (_running) return;

                _running = true;

                _tasksManager.Start(() =>
                {
                    //Delay first execution until expected time has passed
                    var intervalInMilliseconds = _configurationOptions.TelemetryRefreshRate * 1000;
                    _wrapperAdapter.TaskDelay(intervalInMilliseconds).Wait();

                    _tasksManager.StartPeriodic(() => RecordStats(), intervalInMilliseconds, _cancellationTokenSource, "Telemetry Stats.");
                }, _cancellationTokenSource, "Main Telemetry.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                RecordStats();
            }            
        }

        public void RecordConfigInit()
        {
            _tasksManager.Start(() => RecordInit(), new CancellationTokenSource(), "Telemetry ConfigInit.");
        }
        #endregion

        #region Private Methods
        private void RecordInit()
        {
            try
            {
                var config = new Config
                {
                    BURTimeouts = _telemetryStorageConsumer.GetBURTimeouts(),
                    EventsQueueSize = _configurationOptions.EventLogSize,
                    Rates = new Rates
                    {
                        Events = _configurationOptions.EventLogRefreshRate,
                        Impressions = _configurationOptions.TreatmentLogRefreshRate,
                        Segments = _configurationOptions.SegmentRefreshRate,
                        Splits = _configurationOptions.SplitsRefreshRate,
                        Telemetry = _configurationOptions.TelemetryRefreshRate
                    },
                    UrlOverrides = new UrlOverrides
                    {
                        Sdk = !_configurationOptions.BaseUrl.Equals(Constants.Urls.BaseUrl),
                        Events = !_configurationOptions.EventsBaseUrl.Equals(Constants.Urls.EventsBaseUrl),
                        Auth = !_configurationOptions.AuthServiceURL.Equals(Constants.Urls.AuthServiceURL),
                        Stream = !_configurationOptions.StreamingServiceURL.Equals(Constants.Urls.StreamingServiceURL),
                        Telemetry = !_configurationOptions.TelemetryServiceURL.Equals(Constants.Urls.TelemetryServiceURL)
                    },
                    StreamingEnabled = _configurationOptions.StreamingEnabled,
                    ImpressionsMode = _configurationOptions.ImpressionsMode,
                    ImpressionListenerEnabled = _configurationOptions.ImpressionListener != null,
                    OperationMode = (int)_configurationOptions.Mode,
                    ImpressionsQueueSize = _configurationOptions.TreatmentLogSize,
                    Tags = _telemetryStorageConsumer.PopTags().ToList(),
                    TimeUntilSDKReady = CurrentTimeHelper.CurrentTimeMillis() - _configurationOptions.SdkStartTime,
                    ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                    RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories(),
                    Storage = Constants.StorageType.Memory,
                    SDKNotReadyUsage = _telemetryStorageConsumer.GetNonReadyUsages(),
                    HTTPProxyDetected = IsHTTPProxyDetected()
                };

                _telemetryAPI.RecordConfigInit(config);
            }
            catch (Exception ex)
            {
                _log.Error("Something were wrong posting Config.", ex);
            }
        }

        private void RecordStats()
        {
            try
            {
                var stats = new Stats
                {
                    AuthRejections = _telemetryStorageConsumer.PopAuthRejections(),
                    EventsDropped = _telemetryStorageConsumer.GetEventsStats(EventsEnum.EventsDropped),
                    EventsQueued = _telemetryStorageConsumer.GetEventsStats(EventsEnum.EventsQueued),
                    HTTPErrors = _telemetryStorageConsumer.PopHttpErrors(),
                    HTTPLatencies = _telemetryStorageConsumer.PopHttpLatencies(),
                    ImpressionsDeduped = _telemetryStorageConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped),
                    ImpressionsDropped = _telemetryStorageConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped),
                    ImpressionsQueued = _telemetryStorageConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued),
                    LastSynchronizations = _telemetryStorageConsumer.GetLastSynchronizations(),
                    MethodExceptions = _telemetryStorageConsumer.PopExceptions(),
                    MethodLatencies = _telemetryStorageConsumer.PopLatencies(),
                    SessionLengthMs = _telemetryStorageConsumer.GetSessionLength(),
                    StreamingEvents = _telemetryStorageConsumer.PopStreamingEvents().ToList(),
                    Tags = _telemetryStorageConsumer.PopTags().ToList(),
                    TokenRefreshes = _telemetryStorageConsumer.PopTokenRefreshes(),
                    SplitCount = _featureFlagCacheConsumer.SplitsCount(),
                    SegmentCount = _segmentCache.SegmentsCount(),
                    SegmentKeyCount = _segmentCache.SegmentKeysCount(),
                    UpdatesFromSSE = _telemetryStorageConsumer.PopUpdatesFromSSE()
                };

                _telemetryAPI.RecordStats(stats);
            }
            catch (Exception ex)
            {
                _log.Error("Something were wrong posting Stats.", ex);
            }
        }

        private bool IsHTTPProxyDetected()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HTTP_PROXY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HTTPS_PROXY"));
        }
        #endregion
    }
}
