using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsManager : IImpressionsManager
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(ImpressionsManager));

        private readonly IImpressionsObserver _impressionsObserver;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IImpressionListener _customerImpressionListener;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _taskManager;
        private readonly ImpressionsMode _impressionsMode;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly bool _optimized;
        private readonly bool _addPreviousTime;

        public ImpressionsManager(ImpressionsManagerConfig config)
        {            
            _impressionsLog = config.ImpressionsLog;
            _customerImpressionListener = config.CustomerImpressionListener;
            _impressionsCounter = config.ImpressionsCounter;
            _addPreviousTime = config.AddPreviousTime;
            _optimized = config.ImpressionsMode == ImpressionsMode.Optimized && _addPreviousTime;
            _impressionsObserver = config.ImpressionsObserver;
            _telemetryRuntimeProducer = config.TelemetryRuntimeProducer;
            _taskManager = config.TaskManager;
            _impressionsMode = config.ImpressionsMode;
            _uniqueKeysTracker = config.UniqueKeysTracker;
        }

        #region Public Methods
        public KeyImpression BuildImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey)
        {
            var impression = new KeyImpression(matchingKey, feature, treatment, time, changeNumber, label, bucketingKey);

            try
            {
                switch (_impressionsMode)
                {
                    // In DEBUG mode we should calculate the pt only. 
                    case ImpressionsMode.Debug: 
                        if (_addPreviousTime && _impressionsObserver != null)
                        {
                            impression.previousTime = _impressionsObserver.TestAndSet(impression);
                        }
                        break;
                    // In NONE mode we should track the total amount of evaluations and the unique keys.
                    case ImpressionsMode.None:   
                        _impressionsCounter.Inc(feature, time);
                        _uniqueKeysTracker.Track(matchingKey, feature);
                        break;
                    // In OPTIMIZED mode we should track the total amount of evaluations and deduplicate the impressions.
                    case ImpressionsMode.Optimized:
                    default:
                        if (_addPreviousTime && _impressionsObserver != null)
                        {
                            impression.previousTime = _impressionsObserver.TestAndSet(impression);
                        }

                        _impressionsCounter.Inc(feature, time);
                        impression.Optimized = ShouldQueueImpression(impression);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught processing impressions.", ex);
            }

            return impression;
        }

        public void BuildAndTrack(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey)
        {
            Track(new List<KeyImpression>()
            {
                BuildImpression(matchingKey, feature, treatment, time, changeNumber, label, bucketingKey)
            });
        }

        public void Track(List<KeyImpression> impressions)
        {
            if (!impressions.Any()) return;

            var telemetryStats = new TelemetryStats();

            try
            {
                if (_impressionsMode == ImpressionsMode.None || _impressionsLog == null) return;

                switch (_impressionsMode)
                {
                    case ImpressionsMode.Debug:
                        TrackDebugMode(impressions, telemetryStats);
                        break;
                    case ImpressionsMode.Optimized:
                    default:
                        TrackOptimizedMode(impressions, telemetryStats);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught tracking impressions.", ex);
            }
            finally
            {
                RecordStats(telemetryStats);

                if (_customerImpressionListener != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        foreach (var imp in impressions)
                        {
                            _customerImpressionListener.Log(imp);
                        }
                    });
                }
            }
        }

        // Public only for tests
        public bool ShouldQueueImpression(KeyImpression impression)
        {
            return impression.previousTime == null || (ImpressionsHelper.TruncateTimeFrame(impression.previousTime.Value) != ImpressionsHelper.TruncateTimeFrame(impression.time));
        }
        #endregion

        #region Private Methods
        private void RecordStats(TelemetryStats telemetryStats)
        {
            if (_telemetryRuntimeProducer == null) return;

            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, telemetryStats.Deduped);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, telemetryStats.Dropped);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, telemetryStats.Queued);
        }

        private void TrackOptimizedMode(List<KeyImpression> impressions, TelemetryStats telemetryStats)
        {
            var optimizedImpressions = impressions.Where(i => i.Optimized).ToList();
            telemetryStats.Deduped = impressions.Count() - optimizedImpressions.Count;

            if (optimizedImpressions.Any())
            {
                telemetryStats.Dropped = _impressionsLog.Log(optimizedImpressions);
                telemetryStats.Queued = optimizedImpressions.Count - telemetryStats.Dropped;
            }
        }

        private void TrackDebugMode(List<KeyImpression> impressions, TelemetryStats telemetryStats)
        {
            telemetryStats.Dropped = _impressionsLog.Log(impressions);
            telemetryStats.Queued = impressions.Count - telemetryStats.Dropped;
        }
        #endregion
    }

    public class ImpressionsManagerConfig
    {
        public ImpressionsManagerConfig() { }

        public ImpressionsManagerConfig(IImpressionsLog impressionsLog,
            IImpressionListener customerImpressionListener,
            IImpressionsCounter impressionsCounter,
            bool addPreviousTime,
            ImpressionsMode impressionsMode,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager taskManager,
            IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsObserver impressionsObserver)
        {
            ImpressionsLog = impressionsLog;
            CustomerImpressionListener = customerImpressionListener;
            ImpressionsCounter = impressionsCounter;
            AddPreviousTime = addPreviousTime;
            ImpressionsObserver = impressionsObserver;
            TelemetryRuntimeProducer = telemetryRuntimeProducer;
            TaskManager = taskManager;
            ImpressionsMode = impressionsMode;
            UniqueKeysTracker = uniqueKeysTracker;
        }

        public IImpressionsLog ImpressionsLog { get; set; }
        public IImpressionListener CustomerImpressionListener { get; set; }
        public IImpressionsCounter ImpressionsCounter { get; set; }
        public bool AddPreviousTime { get; set; }
        public ImpressionsMode ImpressionsMode { get; set; }
        public ITelemetryRuntimeProducer TelemetryRuntimeProducer { get; set; }
        public ITasksManager TaskManager { get; set; }
        public IUniqueKeysTracker UniqueKeysTracker { get; set; }
        public IImpressionsObserver ImpressionsObserver { get; set; }
    }

    public class TelemetryStats
    {
        public int Queued { get; set; }
        public int Dropped { get; set; }
        public int Deduped { get; set; }
    }
}
