using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Telemetry.Common
{
    public class TelemetrySyncTask : ITelemetrySyncTask
    {
        private readonly ITelemetryStorageConsumer _telemetryStorageConsumer;
        private readonly ITelemetryAPI _telemetryAPI;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly ISplitLogger _log;
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;
        private readonly ISplitTask _statsTask;
        private readonly ISplitTask _initTask;
        private readonly SelfRefreshingConfig _configurationOptions;


        public TelemetrySyncTask(ITelemetryStorageConsumer telemetryStorage,
            ITelemetryAPI telemetryAPI,            
            ISplitCache splitCache,
            ISegmentCache segmentCache,
            SelfRefreshingConfig configurationOptions,
            IFactoryInstantiationsService factoryInstantiationsService,
            ISplitTask statsTask,
            ISplitTask initTask)
        {
            _telemetryStorageConsumer = telemetryStorage;
            _telemetryAPI = telemetryAPI;            
            _splitCache = splitCache;
            _segmentCache = segmentCache;
            _configurationOptions = configurationOptions;
            _factoryInstantiationsService = factoryInstantiationsService;
            _log = WrapperAdapter.Instance().GetLogger(typeof(TelemetrySyncTask));
            _statsTask = statsTask;
            _statsTask.SetAction(RecordStats);
            _initTask = initTask;
        }

        #region Public Methods
        public void Start()
        {
            _statsTask.Start();
        }

        public async Task StopAsync()
        {
            await _statsTask.StopAsync();
            RecordStats();       
        }

        public void RecordConfigInit(long timeUntilSDKReady)
        {
            _initTask.SetAction(() => RecordInit(timeUntilSDKReady));
            _initTask.Start();
        }
        #endregion

        #region Private Methods
        private void RecordInit(long timeUntilSDKReady)
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
                    TimeUntilSDKReady = timeUntilSDKReady,
                    ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                    RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories(),
                    Storage = Constants.StorageType.Memory,
                    SDKNotReadyUsage = _telemetryStorageConsumer.GetNonReadyUsages(),
                    HTTPProxyDetected = IsHTTPProxyDetected(),
                    FlagSets = _configurationOptions.FlagSets.Count
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
                    SplitCount = _splitCache.SplitsCount(),
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
