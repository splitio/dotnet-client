using Splitio.CommonLibraries;
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
using System.Threading.Tasks;

namespace Splitio.Telemetry.Common
{
    public class TelemetrySyncTask : ITelemetrySyncTask
    {
        private readonly ITelemetryStorageConsumer _telemetryStorage;
        private readonly ITelemetryAPI _telemetryAPI;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ISplitLogger _log;
        private readonly IReadinessGatesCache _gates;
        private readonly SelfRefreshingConfig _configurationOptions;
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;
        private bool _firstTime;

        public TelemetrySyncTask(ITelemetryStorageConsumer telemetryStorage,
            ITelemetryAPI telemetryAPI,            
            ISplitCache splitCache,
            ISegmentCache segmentCache,
            IReadinessGatesCache gates,
            SelfRefreshingConfig configurationOptions,
            IFactoryInstantiationsService factoryInstantiationsService,
            bool firstTime = true,
            ISplitLogger log = null)
        {
            _telemetryStorage = telemetryStorage;
            _telemetryAPI = telemetryAPI;            
            _splitCache = splitCache;
            _segmentCache = segmentCache;
            _gates = gates;
            _configurationOptions = configurationOptions;
            _factoryInstantiationsService = factoryInstantiationsService;
            _firstTime = firstTime;
            _cancellationTokenSource = new CancellationTokenSource();
            _log = log ?? WrapperAdapter.GetLogger(typeof(TelemetrySyncTask));
        }

        #region Public Methods
        public void Start()
        {
            if (_firstTime)
            {
                _firstTime = false;

                Task.Factory.StartNew(() => RecordConfigInit());
            }

            PeriodicTaskFactory.Start(() => { RecordStats(); }, _configurationOptions.TelemetryRefreshRate * 1000, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            RecordStats();
        }
        #endregion

        #region Private Methods
        private void RecordConfigInit()
        {
            try
            {
                _gates.WaitUntilSdkInternalReady();

                var config = new Config
                {
                    BURTimeouts = _telemetryStorage.GetBURTimeouts(),
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
                    Tags = _telemetryStorage.PopTags().ToList(),
                    TimeUntilSDKReady = CurrentTimeHelper.CurrentTimeMillis() - _configurationOptions.SdkStartTime,
                    ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                    RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories(),
                    Storage = Constants.StorageType.Memory,
                    SDKNotReadyUsage = _telemetryStorage.GetNonReadyUsages(),
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
                    AuthRejections = _telemetryStorage.PopAuthRejections(),
                    EventsDropped = _telemetryStorage.GetEventsStats(EventsEnum.EventsDropped),
                    EventsQueued = _telemetryStorage.GetEventsStats(EventsEnum.EventsQueued),
                    HTTPErrors = _telemetryStorage.PopHttpErrors(),
                    HTTPLatencies = _telemetryStorage.PopHttpLatencies(),
                    ImpressionsDeduped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped),
                    ImpressionsDropped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped),
                    ImpressionsQueued = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued),
                    LastSynchronizations = _telemetryStorage.GetLastSynchronizations(),
                    MethodExceptions = _telemetryStorage.PopExceptions(),
                    MethodLatencies = _telemetryStorage.PopLatencies(),
                    SessionLengthMs = _telemetryStorage.GetSessionLength(),
                    StreamingEvents = _telemetryStorage.PopStreamingEvents().ToList(),
                    Tags = _telemetryStorage.PopTags().ToList(),
                    TokenRefreshes = _telemetryStorage.PopTokenRefreshes(),
                    SplitCount = _splitCache.SplitsCount(),
                    SegmentCount = _segmentCache.SegmentsCount(),
                    SegmentKeyCount = _segmentCache.SegmentKeysCount()
                };

                _telemetryAPI.RecordStats(stats);
            }
            catch (Exception ex)
            {
                _log.Error("Something were wrong posting Stats.", ex);
            }
        }
        #endregion
    }
}
