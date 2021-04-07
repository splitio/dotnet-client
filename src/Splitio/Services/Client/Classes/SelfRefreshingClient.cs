﻿using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Events.Classes;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Client.Classes
{
    public class SelfRefreshingClient : SplitClient
    {
        private readonly SelfRefreshingConfig _config;

        /// <summary>
        /// Represents the initial number of buckets for a ConcurrentDictionary. 
        /// Should not be divisible by a small prime number. 
        /// The default capacity is 31. 
        /// More details : https://msdn.microsoft.com/en-us/library/dd287171(v=vs.110).aspx
        /// </summary>
        private const int InitialCapacity = 31;

        private IReadinessGatesCache _gates;
        private ISplitFetcher _splitFetcher;
        private ISplitSdkApiClient _splitSdkApiClient;
        private ISegmentSdkApiClient _segmentSdkApiClient;
        private ITreatmentSdkApiClient _treatmentSdkApiClient;
        private IEventSdkApiClient _eventSdkApiClient;
        private ISelfRefreshingSegmentFetcher _selfRefreshingSegmentFetcher;
        private ISyncManager _syncManager;
        private IImpressionsCounter _impressionsCounter;
        private ITelemetryAPI _telemetryAPI;

        public SelfRefreshingClient(string apiKey, 
            ConfigurationOptions config, 
            ISplitLogger log = null) : base(GetLogger(log))
        {
            _config = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfingTypes.InMemory);
            LabelsEnabled = _config.LabelsEnabled;
            Destroyed = false;
            
            ApiKey = apiKey;
            BuildSdkReadinessGates();
            BuildSdkApiClients();
            BuildSplitFetcher();
            BuildTreatmentLog(config);
            BuildImpressionManager();
            BuildEventLog(config);
            BuildEvaluator();
            BuildBlockUntilReadyService();
            BuildManager();
            BuildTelemetryAPI();
            BuildSyncManager();

            Start();
        }

        #region Public Methods
        public override void Destroy()
        {
            if (!Destroyed)
            {
                Stop();
                base.Destroy();
            }
        }
        #endregion

        #region Private Methods
        private void BuildSdkReadinessGates()
        {
            _gates = new InMemoryReadinessGatesCache();
        }

        private void BuildSplitFetcher()
        {
            var segmentRefreshRate = _config.RandomizeRefreshRates ? Random(_config.SegmentRefreshRate) : _config.SegmentRefreshRate;
            var splitsRefreshRate = _config.RandomizeRefreshRates ? Random(_config.SplitsRefreshRate) : _config.SplitsRefreshRate;

            _segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(_config.ConcurrencyLevel, InitialCapacity));

            var segmentChangeFetcher = new ApiSegmentChangeFetcher(_segmentSdkApiClient);
            var segmentTaskQueue = new SegmentTaskQueue();
            _selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(segmentChangeFetcher, _gates, segmentRefreshRate, _segmentCache, _config.NumberOfParalellSegmentTasks, segmentTaskQueue);

            var splitChangeFetcher = new ApiSplitChangeFetcher(_splitSdkApiClient);
            _splitParser = new InMemorySplitParser((SelfRefreshingSegmentFetcher)_selfRefreshingSegmentFetcher, _segmentCache);
            _splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(_config.ConcurrencyLevel, InitialCapacity));
            _splitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, _splitParser, _gates, splitsRefreshRate, _splitCache);

            _trafficTypeValidator = new TrafficTypeValidator(_splitCache);
        }

        private void BuildTreatmentLog(ConfigurationOptions config)
        {
            var impressionsCache = new InMemorySimpleCache<KeyImpression>(new BlockingQueue<KeyImpression>(_config.TreatmentLogSize));
            _impressionsLog = new ImpressionsLog(_treatmentSdkApiClient, _config.TreatmentLogRefreshRate, impressionsCache);

            _customerImpressionListener = config.ImpressionListener;
        }

        private void BuildImpressionManager()
        {
            var impressionsHasher = new ImpressionHasher();
            var impressionsObserver = new ImpressionsObserver(impressionsHasher);
            _impressionsCounter = new ImpressionsCounter();
            _impressionsManager = new ImpressionsManager(_impressionsLog, _customerImpressionListener, _impressionsCounter, true, _config.ImpressionsMode, impressionsObserver);
        }

        private void BuildEventLog(ConfigurationOptions config)
        {
            var eventsCache = new InMemorySimpleCache<WrappedEvent>(new BlockingQueue<WrappedEvent>(_config.EventLogSize));
            _eventsLog = new EventsLog(_eventSdkApiClient, _config.EventsFirstPushWindow, _config.EventLogRefreshRate, eventsCache);
        }        

        private int Random(int refreshRate)
        {
            Random random = new Random();
            return Math.Max(5, random.Next(refreshRate / 2, refreshRate));
        }

        private void BuildSdkApiClients()
        {
            var headers = GetHeaders();
            headers.Add("Accept-Encoding", "gzip");
            headers.Add("Keep-Alive", "true");

            _splitSdkApiClient = new SplitSdkApiClient(ApiKey, headers, _config.BaseUrl, _config.HttpConnectionTimeout, _config.HttpReadTimeout);
            _segmentSdkApiClient = new SegmentSdkApiClient(ApiKey, headers, _config.BaseUrl, _config.HttpConnectionTimeout, _config.HttpReadTimeout);
            _treatmentSdkApiClient = new TreatmentSdkApiClient(ApiKey, headers, _config.EventsBaseUrl, _config.HttpConnectionTimeout, _config.HttpReadTimeout);
            _eventSdkApiClient = new EventSdkApiClient(ApiKey, headers, _config.EventsBaseUrl, _config.HttpConnectionTimeout, _config.HttpReadTimeout);
        }

        private void BuildManager()
        {
            _manager = new SplitManager(_splitCache, _blockUntilReadyService);
        }

        private void BuildBlockUntilReadyService()
        {
            _blockUntilReadyService = new SelfRefreshingBlockUntilReadyService(_gates, _log);
        }

        private void BuildTelemetryAPI()
        {
            var httpClient = new SplitioHttpClient(ApiKey, _config.HttpConnectionTimeout, GetHeaders());
            _telemetryAPI = new TelemetryAPI(httpClient, _config.TelemetryServiceURL);
        }

        private void BuildSyncManager()
        {
            try
            {
                // Synchronizer
                var impressionsCountSender = new ImpressionsCountSender(_treatmentSdkApiClient, _impressionsCounter);
                var synchronizer = new Synchronizer(_splitFetcher, _selfRefreshingSegmentFetcher, _impressionsLog, _eventsLog, impressionsCountSender, _wrapperAdapter);

                // Workers
                var splitsWorker = new SplitsWorker(_splitCache, synchronizer);
                var segmentsWorker = new SegmentsWorker(_segmentCache, synchronizer);

                // NotificationProcessor
                var notificationProcessor = new NotificationProcessor(splitsWorker, segmentsWorker);

                // NotificationParser
                var notificationParser = new NotificationParser();

                // NotificationManagerKeeper
                var notificationManagerKeeper = new NotificationManagerKeeper();

                // EventSourceClient
                var headers = GetHeaders();
                headers.Add("SplitSDKClientKey", ApiKey.Substring(ApiKey.Length - 4));
                headers.Add("Accept", "text/event-stream");
                var sseHttpClient = new SplitioHttpClient(ApiKey, _config.HttpConnectionTimeout, headers);
                var eventSourceClient = new EventSourceClient(notificationParser, _wrapperAdapter, sseHttpClient);

                // SSEHandler
                var sseHandler = new SSEHandler(_config.StreamingServiceURL, splitsWorker, segmentsWorker, notificationProcessor, notificationManagerKeeper, eventSourceClient: eventSourceClient);

                // AuthApiClient
                var httpClient = new SplitioHttpClient(ApiKey, _config.HttpConnectionTimeout, GetHeaders());
                var authApiClient = new AuthApiClient(_config.AuthServiceURL, ApiKey, httpClient);

                // PushManager
                var pushManager = new PushManager(_config.AuthRetryBackoffBase, sseHandler, authApiClient, _wrapperAdapter);

                // SyncManager
                _syncManager = new SyncManager(_config.StreamingEnabled, synchronizer, pushManager, sseHandler, notificationManagerKeeper);
            }
            catch (Exception ex)
            {
                _log.Error($"BuildSyncManager: {ex.Message}");
            }
        }        

        private Dictionary<string, string> GetHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKVersion", _config.SdkVersion },
                { "SplitSDKImpressionsMode", _config.ImpressionsMode.Equals(ImpressionsMode.Optimized).ToString() }
            };

            if (!string.IsNullOrEmpty(_config.SdkMachineName) && !_config.SdkMachineName.Equals(Constans.Unknown))
            {
                headers.Add("SplitSDKMachineName", _config.SdkMachineName);
            }

            if (!string.IsNullOrEmpty(_config.SdkMachineIP) && !_config.SdkMachineIP.Equals(Constans.Unknown))
            {
                headers.Add("SplitSDKMachineIP", _config.SdkMachineIP);
            }

            return headers;
        }

        private void Start()
        {
            _syncManager.Start();
        }

        private void Stop()
        {
            _syncManager.Shutdown();
        }

        private static ISplitLogger GetLogger(ISplitLogger splitLogger = null)
        {
            return splitLogger ?? WrapperAdapter.GetLogger(typeof(SelfRefreshingClient));
        }
        #endregion
    }
}
