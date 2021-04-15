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
        private readonly ITelemetryInitConsumer _telemetryInitConsumer;
        private readonly ITelemetryRuntimeConsumer _telemetryRuntimeConsumer;
        private readonly ITelemetryEvaluationConsumer _telemetryEvaluationConsumer;
        private readonly ITelemetryAPI _telemetryAPI;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ISplitLogger _log;
        private readonly IReadinessGatesCache _gates;
        private readonly SelfRefreshingConfig _configurationOptions;
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;
        private bool _firstTime;

        public TelemetrySyncTask(ITelemetryInitConsumer telemetryInitConsumer,
            ITelemetryRuntimeConsumer telemetryRuntimeConsumer,
            ITelemetryEvaluationConsumer telemetryEvaluationConsumer,
            ITelemetryAPI telemetryAPI,            
            ISplitCache splitCache,
            ISegmentCache segmentCache,
            IReadinessGatesCache gates,
            SelfRefreshingConfig configurationOptions,
            IFactoryInstantiationsService factoryInstantiationsService,
            bool firstTime = true,
            ISplitLogger log = null)
        {
            _telemetryInitConsumer = telemetryInitConsumer;
            _telemetryRuntimeConsumer = telemetryRuntimeConsumer;
            _telemetryEvaluationConsumer = telemetryEvaluationConsumer;
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
                    BURTimeouts = _telemetryInitConsumer.GetBURTimeouts(),
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
                    Tags = _telemetryRuntimeConsumer.PopTags().ToList(),
                    TimeUntilSDKReady = CurrentTimeHelper.CurrentTimeMillis() - _configurationOptions.SdkStartTime,
                    ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                    RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories(),
                    Storage = Constants.StorageType.Memory,
                    SDKNotReadyUsage = _telemetryInitConsumer.GetNonReadyUsages(),
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
                    AuthRejections = _telemetryRuntimeConsumer.PopAuthRejections(),
                    EventsDropped = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsDropped),
                    EventsQueued = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsQueued),
                    HTTPErrors = _telemetryRuntimeConsumer.PopHttpErrors(),
                    HTTPLatencies = _telemetryRuntimeConsumer.PopHttpLatencies(),
                    ImpressionsDeduped = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped),
                    ImpressionsDropped = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped),
                    ImpressionsQueued = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued),
                    LastSynchronizations = _telemetryRuntimeConsumer.GetLastSynchronizations(),
                    MethodExceptions = _telemetryEvaluationConsumer.PopExceptions(),
                    MethodLatencies = _telemetryEvaluationConsumer.PopLatencies(),
                    SessionLengthMs = _telemetryRuntimeConsumer.GetSessionLength(),
                    StreamingEvents = _telemetryRuntimeConsumer.PopStreamingEvents().ToList(),
                    Tags = _telemetryRuntimeConsumer.PopTags().ToList(),
                    TokenRefreshes = _telemetryRuntimeConsumer.PopTokenRefreshes(),
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
