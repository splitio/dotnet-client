using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsManager : IImpressionsManager
    {
        private readonly IImpressionsObserver _impressionsObserver;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IImpressionListener _customerImpressionListener;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _tasksManager;
        private readonly bool _optimized;
        private readonly bool _addPreviousTime;

        public ImpressionsManager(IImpressionsLog impressionsLog,
            IImpressionListener customerImpressionListener,
            IImpressionsCounter impressionsCounter,
            bool addPreviousTime,
            ImpressionsMode impressionsMode,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager tasksManager,
            IImpressionsObserver impressionsObserver = null)
        {            
            _impressionsLog = impressionsLog;
            _customerImpressionListener = customerImpressionListener;
            _impressionsCounter = impressionsCounter;
            _addPreviousTime = addPreviousTime;
            _optimized = impressionsMode == ImpressionsMode.Optimized && addPreviousTime;
            _impressionsObserver = impressionsObserver;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _tasksManager = tasksManager;
        }

        public KeyImpression BuildImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey)
        {
            var impression = new KeyImpression(matchingKey, feature, treatment, time, changeNumber, label, bucketingKey);

            if (_addPreviousTime && _impressionsObserver != null)
            {
                impression.previousTime = _impressionsObserver.TestAndSet(impression);
            }

            if (_optimized)
            {
                _impressionsCounter.Inc(feature, time);
                impression.Optimized = ShouldQueueImpression(impression);
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
            if (impressions.Any())
            {
                if (_impressionsLog != null)
                {
                    var dropped = 0;
                    var queued = 0;
                    var deduped = 0;

                    if (_optimized)
                    {
                        var optimizedImpressions = impressions.Where(i => i.Optimized).ToList();
                        deduped = impressions.Count() - optimizedImpressions.Count;

                        if (optimizedImpressions.Any())
                        {
                            dropped = _impressionsLog.Log(optimizedImpressions);
                            queued = optimizedImpressions.Count - dropped;
                        }
                    }
                    else
                    {
                        dropped = _impressionsLog.Log(impressions);
                        queued = impressions.Count - dropped;
                    }

                    RecordStats(queued, dropped, deduped);
                }

                if (_customerImpressionListener != null)
                {
                    _tasksManager.Start(() =>
                    {
                        foreach (var imp in impressions)
                        {
                            _customerImpressionListener.Log(imp);
                        }
                    }, new CancellationTokenSource(), "Impression Listener.sssssssssssssssssssssssssssss");
                }
            }
        }

        public bool ShouldQueueImpression(KeyImpression impression)
        {
            return impression.previousTime == null || (ImpressionsHelper.TruncateTimeFrame(impression.previousTime.Value) != ImpressionsHelper.TruncateTimeFrame(impression.time));
        }

        private void RecordStats(int queued, int dropped, int deduped)
        {
            if (_telemetryRuntimeProducer == null) return;

            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, deduped);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, dropped);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, queued);
        }
    }
}
