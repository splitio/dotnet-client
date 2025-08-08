using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
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
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsManager));

        private readonly IImpressionsObserver _impressionsObserver;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IImpressionListener _customerImpressionListener;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _taskManager;
        private readonly ImpressionsMode _impressionsMode;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly IPropertiesValidator _propertiesValidator;
        private readonly bool _addPreviousTime;
        private readonly bool _labelsEnabled;

        public ImpressionsManager(IImpressionsLog impressionsLog,
            IImpressionListener customerImpressionListener,
            IImpressionsCounter impressionsCounter,
            bool addPreviousTime,
            ImpressionsMode impressionsMode,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager taskManager,
            IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsObserver impressionsObserver,
            bool labelsEnabled,
            IPropertiesValidator propertiesValidator)
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
            _labelsEnabled = labelsEnabled;
            _propertiesValidator = propertiesValidator;
        }

        #region Public Methods
        public KeyImpression Build(TreatmentResult result, Key key, Dictionary<string, object> properties)
        {
            if (Labels.SplitNotFound.Equals(result.Label)) return null;

            var impression = new KeyImpression(key.matchingKey, result.FeatureFlagName, result.Treatment, result.ImpTime, result.ChangeNumber, _labelsEnabled ? result.Label : null, key.bucketingKeyHadValue ? key.bucketingKey : null, result.ImpressionsDisabled);

            var validatorResult = _propertiesValidator.IsValid(properties);
            if (validatorResult.Success)
            {
                impression.Properties = JsonConvert.SerializeObject(validatorResult.Value);
            }
            
            try
            {
                // In NONE mode we should track the total amount of evaluations and the unique keys.
                if (_impressionsMode == ImpressionsMode.None || result.ImpressionsDisabled)
                {
                    _impressionsCounter.Inc(result.FeatureFlagName, result.ImpTime);
                    _uniqueKeysTracker.Track(key.matchingKey, result.FeatureFlagName);
                }
                else if (string.IsNullOrEmpty(impression.Properties))
                {
                    switch (_impressionsMode)
                    {
                        case ImpressionsMode.Debug:
                            // In DEBUG mode we should calculate the pt only.
                            ShouldCalculatePreviousTime(impression);
                            break;
                        case ImpressionsMode.Optimized:
                            // In OPTIMIZED mode we should track the total amount of evaluations and deduplicate the impressions.
                            ShouldCalculatePreviousTime(impression);
                            if (impression.PreviousTime.HasValue)
                                _impressionsCounter.Inc(result.FeatureFlagName, result.ImpTime);
                            impression.Optimized = ShouldQueueImpression(impression);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught processing impressions.", ex);
            }

            return impression;
        }

        public void Track(List<KeyImpression> impressions)
        {
            try
            {
                if (!GetImpressionsToTrack(impressions, out var impressionsToTrack)) return;

                var dropped = _impressionsLog.Log(impressionsToTrack);
                
                RecordTelemetry(impressionsToTrack.Count, dropped);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught tracking impressions.", ex);
            }
            finally
            {
                LogImpressionListener(impressions);
            }
        }

        public async Task TrackAsync(List<KeyImpression> impressions)
        {
            try
            {
                if (!GetImpressionsToTrack(impressions, out var impressionsToTrack)) return;

                var dropped = await _impressionsLog.LogAsync(impressionsToTrack);

                RecordTelemetry(impressionsToTrack.Count, dropped);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught tracking impressions.", ex);
            }
            finally
            {
                LogImpressionListener(impressions);
            }
        }

        // Public only for tests
        public static bool ShouldQueueImpression(KeyImpression impression)
        {
            return !impression.PreviousTime.HasValue || (ImpressionsHelper.TruncateTimeFrame(impression.PreviousTime.Value) != ImpressionsHelper.TruncateTimeFrame(impression.Time));
        }
        #endregion

        #region Private Methods
        private void ShouldCalculatePreviousTime(KeyImpression impression)
        {
            if (!_addPreviousTime) return;
            
            impression.PreviousTime = _impressionsObserver.TestAndSet(impression);
        }

        private void LogImpressionListener(List<KeyImpression> impressions)
        {
            if (_customerImpressionListener == null || !impressions.Any()) return;

            _taskManager.NewOnTimeTaskAndStart(Enums.Task.ImpressionListener, () =>
            {
                foreach (var imp in impressions)
                {
                    _customerImpressionListener.Log(imp);
                }
            });
        }

        private bool GetImpressionsToTrack(List<KeyImpression> impressions, out List<KeyImpression> impressionsToTrack)
        {
            impressionsToTrack = new List<KeyImpression>();

            if (_impressionsMode == ImpressionsMode.None || impressions.Count == 0 || _impressionsLog == null) return false;

            var filteredImpressions = impressions
                .Where(i => !i.ImpressionsDisabled)
                .ToList();

            if (filteredImpressions.Count == 0)
            {
                return false;
            }

            switch (_impressionsMode)
            {
                case ImpressionsMode.Debug:
                    impressionsToTrack = filteredImpressions;
                    break;
                case ImpressionsMode.Optimized:
                default:
                    impressionsToTrack = filteredImpressions
                        .Where(i => i.Optimized || !string.IsNullOrEmpty(i.Properties))
                        .ToList();

                    _telemetryRuntimeProducer?.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, filteredImpressions.Count - impressionsToTrack.Count);
                    break;
            }

            return true;
        }

        private void RecordTelemetry(int total, int dropped)
        {
            if (_telemetryRuntimeProducer == null) return;

            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, dropped);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, total - dropped);
        }
        #endregion
    }
}
