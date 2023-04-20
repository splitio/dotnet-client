using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsManager : IImpressionsManager
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsManager));

        private readonly IImpressionsObserver _impressionsObserver;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IImpressionListener _customerImpressionListener;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _taskManager;
        private readonly ImpressionsMode _impressionsMode;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly bool _addPreviousTime;

        public ImpressionsManager(IImpressionsLog impressionsLog,
            IImpressionListener customerImpressionListener,
            IImpressionsCounter impressionsCounter,
            bool addPreviousTime,
            ImpressionsMode impressionsMode,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager taskManager,
            IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsObserver impressionsObserver)
        {
            _impressionsLog = impressionsLog;
            _customerImpressionListener = customerImpressionListener;
            _impressionsCounter = impressionsCounter;
            _addPreviousTime = addPreviousTime;
            _impressionsObserver = impressionsObserver;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _taskManager = taskManager;
            _impressionsMode = impressionsMode;
            _uniqueKeysTracker = uniqueKeysTracker;
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
                        ShouldCalculatePreviousTime(impression);
                        break;
                    // In NONE mode we should track the total amount of evaluations and the unique keys.
                    case ImpressionsMode.None:
                        _impressionsCounter.Inc(feature, time);
                        _uniqueKeysTracker.Track(matchingKey, feature);
                        break;
                    // In OPTIMIZED mode we should track the total amount of evaluations and deduplicate the impressions.
                    case ImpressionsMode.Optimized:
                    default:
                        ShouldCalculatePreviousTime(impression);

                        if (impression.previousTime.HasValue)
                            _impressionsCounter.Inc(feature, time);

                        impression.Optimized = ImpressionsManager.ShouldQueueImpression(impression);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught processing impressions.", ex);
            }

            return impression;
        }

        public bool BuildAndTrack(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey)
        {
            return Track(new List<KeyImpression>()
            {
                BuildImpression(matchingKey, feature, treatment, time, changeNumber, label, bucketingKey)
            });
        }

        public bool Track(List<KeyImpression> impressions)
        {
            if (!impressions.Any()) return false;

            var telemetryStats = new TelemetryStats();

            try
            {
                if (_impressionsMode == ImpressionsMode.None || _impressionsLog == null) return false;

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

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught tracking impressions.", ex);
                return false;
            }
            finally
            {
                RecordStats(telemetryStats);

                if (_customerImpressionListener != null)
                {
                    _taskManager.Start(() =>
                    {
                        foreach (var imp in impressions)
                        {
                            _customerImpressionListener.Log(imp);
                        }
                    }, "Impression Listener Log.");
                }
            }
        }

        // Public only for tests
        public static bool ShouldQueueImpression(KeyImpression impression)
        {
            return !impression.previousTime.HasValue || (ImpressionsHelper.TruncateTimeFrame(impression.previousTime.Value) != ImpressionsHelper.TruncateTimeFrame(impression.time));
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

        private void ShouldCalculatePreviousTime(KeyImpression impression)
        {
            if (!_addPreviousTime) return;

            impression.previousTime = _impressionsObserver.TestAndSet(impression);
        }
        #endregion
    }

    public class TelemetryStats
    {
        public int Queued { get; set; }
        public int Dropped { get; set; }
        public int Deduped { get; set; }
    }
}
