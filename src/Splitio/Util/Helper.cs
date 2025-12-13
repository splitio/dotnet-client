using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Util
{
    public class Helper
    {
        public struct ValidSdkEvent
        {
            public SdkEvent SdkEvent { get; set; }
            public bool Valid { get; set; }
        }

        public static List<T> TakeFromList<T>(List<T> items, int size)
        {
            if (items == null) return new List<T>();

            var count = size;

            if (items.Count < size)
            {
                count = items.Count;
            }

            var bulk = items.GetRange(0, count);
            items.RemoveRange(0, count);

            return bulk;
        }

        public static void RecordTelemetrySync(string method, HTTPResult response, ResourceEnum resource, SplitStopwatch clock, ITelemetryRuntimeProducer telemetryRuntimeProducer, ISplitLogger log)
        {
            if (response.IsSuccessStatusCode)
            {
                telemetryRuntimeProducer.RecordSyncLatency(resource, Metrics.Bucket(clock.ElapsedMilliseconds));
                telemetryRuntimeProducer.RecordSuccessfulSync(resource, CurrentTimeHelper.CurrentTimeMillis());
            }
            else
            {
                telemetryRuntimeProducer.RecordSyncError(resource, (int)response.StatusCode);
            }

            log.Debug($"Http status executing {method}: {response.StatusCode}");
        }

        public static bool HasNonASCIICharacters(string input)
        {
            foreach (var c in input)
            {
                if (c > 127) return true;
            }

            return false;
        }

        public static List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static TreatmentResult checkFallbackTreatment(string featureName, string label, bool exception, FallbackTreatmentCalculator fallbackTreatmentCalculator)
        {
            FallbackTreatment fallbackTreatment = fallbackTreatmentCalculator.resolve(featureName, label);
            return new TreatmentResult(featureName,
                fallbackTreatment.Label,
                fallbackTreatment.Treatment,
                false,
                null,
                getFallbackConfig(fallbackTreatment),
                exception
            );
        }

        public static string getFallbackConfig(FallbackTreatment fallbackTreatment)
        {
            if (fallbackTreatment.Config != null)
            {
                return fallbackTreatment.Config;
            }

            return null;
        }

        public static List<SdkEvent> GetSdkEventIfApplicable(SdkInternalEvent sdkInternalEvent, 
            EventsManagerConfig eventsManagerConfig, 
            EventsManager<SdkEvent,SdkInternalEvent,EventMetadata> eventsManager)
        {
            ValidSdkEvent finalSdkEvent = new ValidSdkEvent
            {
                Valid = false,
                SdkEvent = SdkEvent.SdkReady
            };
            eventsManager.UpdateSdkInternalEventStatus(sdkInternalEvent, true);
            List<SdkEvent> eventsToFire = new List<SdkEvent>();

            ValidSdkEvent requireAnySdkEvent = CheckRequireAny(sdkInternalEvent, eventsManagerConfig);
            if (requireAnySdkEvent.Valid)
            {
                if ((!eventsManager.EventAlreadyTriggered(requireAnySdkEvent.SdkEvent)
                    && ExecutionLimit(requireAnySdkEvent.SdkEvent, eventsManagerConfig) == 1) || ExecutionLimit(requireAnySdkEvent.SdkEvent, eventsManagerConfig) == -1)
                {
                    finalSdkEvent.SdkEvent = requireAnySdkEvent.SdkEvent;
                }

                finalSdkEvent.Valid = CheckPrerequisites(finalSdkEvent.SdkEvent, eventsManagerConfig, eventsManager) 
                    && CheckSuppressedBy(finalSdkEvent.SdkEvent, eventsManagerConfig, eventsManager);
            }

            if (finalSdkEvent.Valid)
            {
                eventsToFire.Add(finalSdkEvent.SdkEvent);
            }

            foreach (SdkEvent sdkEvent in CheckRequireAll(eventsManagerConfig, eventsManager))
            {
                eventsToFire.Add(sdkEvent);
            }

            return eventsToFire;
        }

        private static List<SdkEvent> CheckRequireAll(EventsManagerConfig eventsManagerConfig,
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager)
        {
            List<SdkEvent> events = new List<SdkEvent>();
            foreach (KeyValuePair<SdkEvent, HashSet<SdkInternalEvent>> kvp in eventsManagerConfig.RequireAll)
            {
                bool finalStatus = true;
                foreach (var val in kvp.Value)
                {
                    finalStatus &= eventsManager.GetSdkInternalEventStatus(val);
                }
                if (finalStatus
                    && CheckPrerequisites(kvp.Key, eventsManagerConfig, eventsManager)
                    && ((ExecutionLimit(kvp.Key, eventsManagerConfig) == 1 && !eventsManager.EventAlreadyTriggered(kvp.Key))
                        || (ExecutionLimit(kvp.Key, eventsManagerConfig) == -1))
                    && kvp.Value.Count > 0)
                {
                    events.Add(kvp.Key);
                }
            }

            return events;
        }

        private static bool CheckPrerequisites(SdkEvent sdkEvent, EventsManagerConfig eventsManagerConfig,
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager)
        {
            foreach (KeyValuePair<SdkEvent, HashSet<SdkEvent>> kvp in eventsManagerConfig.Prerequisites)
            {
                if (kvp.Key == sdkEvent)
                {
                    if (kvp.Value.Any(x => !eventsManager.EventAlreadyTriggered(x)))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return true;
        }

        private static bool CheckSuppressedBy(SdkEvent sdkEvent, EventsManagerConfig eventsManagerConfig,
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager)
        {
            foreach (KeyValuePair<SdkEvent, HashSet<SdkEvent>> kvp in eventsManagerConfig.SuppressedBy)
            {
                if (kvp.Key == sdkEvent)
                {
                    if (kvp.Value.Any(x => eventsManager.EventAlreadyTriggered(x)))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return true;
        }

        private static int ExecutionLimit(SdkEvent sdkEvent, EventsManagerConfig eventsManagerConfig)
        {
            if (!eventsManagerConfig.ExecutionLimits.ContainsKey(sdkEvent))
                return -1;

            eventsManagerConfig.ExecutionLimits.TryGetValue(sdkEvent, out int limit);
            return limit;
        }

        private static ValidSdkEvent CheckRequireAny(SdkInternalEvent sdkInternalEvent, EventsManagerConfig eventsManagerConfig)
        {
            ValidSdkEvent validSdkEvent;
            validSdkEvent.Valid = false;
            validSdkEvent.SdkEvent = SdkEvent.SdkUpdate;
            foreach (KeyValuePair<SdkEvent, HashSet<SdkInternalEvent>> kvp in eventsManagerConfig.RequireAny)
            {
                if (kvp.Value.Contains(sdkInternalEvent))
                {
                    validSdkEvent.Valid = true;
                    validSdkEvent.SdkEvent = kvp.Key;
                    return validSdkEvent;
                }
            }

            return validSdkEvent;
        }

        public static void BuildInternalSdkEventStatus(EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager)
        {
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkReady, false);
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.RuleBasedSegmentsUpdated, false);
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkTimedOut, false);
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SegmentsUpdated, false);
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagKilledNotification, false);
            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagsUpdated, false);
        }
    }
}
