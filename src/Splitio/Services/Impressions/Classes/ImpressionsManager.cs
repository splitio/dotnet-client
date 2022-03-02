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

        public ImpressionsManager(IImpressionsLog impressionsLog,
            IImpressionListener customerImpressionListener,
            IImpressionsCounter impressionsCounter,
            bool addPreviousTime,
            ImpressionsMode impressionsMode,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager taskManager,
            IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsObserver impressionsObserver = null)
        {            
            _impressionsLog = impressionsLog;
            _customerImpressionListener = customerImpressionListener;
            _impressionsCounter = impressionsCounter;
            _addPreviousTime = addPreviousTime;
            _optimized = impressionsMode == ImpressionsMode.Optimized && addPreviousTime;
            _impressionsObserver = impressionsObserver;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _taskManager = taskManager;
            _impressionsMode = impressionsMode;
            _uniqueKeysTracker = uniqueKeysTracker;
        }

        public KeyImpression BuildImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey)
        {
            var impression = new KeyImpression(matchingKey, feature, treatment, time, changeNumber, label, bucketingKey);

            try
            {
                switch (_impressionsMode)
                {
                    case ImpressionsMode.Debug:
                        if (_addPreviousTime && _impressionsObserver != null)
                        {
                            impression.previousTime = _impressionsObserver.TestAndSet(impression);
                        }
                        break;
                    case ImpressionsMode.None:
                        _impressionsCounter.Inc(feature, time);
                        _uniqueKeysTracker.Track(matchingKey, feature);
                        break;
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
                _logger.Error("Exception caught building impression.", ex);
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

        public bool ShouldQueueImpression(KeyImpression impression)
        {
            return impression.previousTime == null || (ImpressionsHelper.TruncateTimeFrame(impression.previousTime.Value) != ImpressionsHelper.TruncateTimeFrame(impression.time));
        }

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

    public class TelemetryStats
    {
        public int Queued { get; set; }
        public int Dropped { get; set; }
        public int Deduped { get; set; }
    }
}
