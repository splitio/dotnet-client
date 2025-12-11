using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Common
{
    public class EventsManager : IEventsManager
    {
        private readonly ConcurrentDictionary<SdkEvent, Dictionary<string, object>> _activeEvents;
        private readonly string Triggered = "Triggered";
        private readonly string EventHandler = "EventHandler"; 
        private readonly ConcurrentDictionary<SdkInternalEvent, bool> _internalEventsStatus;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");
        private readonly EventsManagerConfig _config;
        private readonly EventDelivery _eventDelivery;
        private readonly object _lock = new object();
        struct ValidSdkEvent
        {
            public SdkEvent sdkEvent;
            public bool valid;
        }

        public EventsManager() 
        {
            _activeEvents = new ConcurrentDictionary<SdkEvent, Dictionary<string, object>>();
            _internalEventsStatus = BuildInternalSdkEventStatus();
            _config = EventsManagerConfig.BuildEventsManagerConfig();
            _eventDelivery = new EventDelivery();
        }

        #region Public Methods
        public void Register(SdkEvent sdkEvent, EventHandler<EventMetadata> handler)
        {
            _activeEvents.TryGetValue(sdkEvent, out var dict);
            if (dict == null)
            {
                _activeEvents.TryAdd(sdkEvent, new Dictionary<string, object>()
                {
                    {Triggered, false},
                    {EventHandler, handler}
                });
                _logger.Debug($"EventManager: Event {sdkEvent} is registered");
            }
        }

        public void Unregister(SdkEvent sdkEvent)
        {
            if (_activeEvents.ContainsKey(sdkEvent))
            { 
                if (_activeEvents.TryGetValue(sdkEvent, out var dict) && dict.Count > 0)
                {
                    _activeEvents.TryRemove(sdkEvent, out _);
                }
            }
        }

        public void NotifyInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            lock (_lock)
            {
                _logger.Debug($"EventHandler: Handling internal event {sdkInternalEvent}");
                UpdateSdkInternalEventStatus(sdkInternalEvent, true);
                ValidSdkEvent eventToNotify = GetSdkEventIfApplicable(sdkInternalEvent);
                if (eventToNotify.valid)
                {
                    _logger.Debug($"EventHandler: Firing Sdk event {eventToNotify.sdkEvent}");
                    _eventDelivery.Deliver(eventToNotify.sdkEvent, eventMetadata, GetEventHandler(eventToNotify.sdkEvent));
                    SetSdkEventTriggered(eventToNotify.sdkEvent);
                }

                foreach (SdkEvent sdkEvent in CheckRequireAll())
                {
                    _logger.Debug($"EventHandler: Firing Sdk event {sdkEvent}");
                    _eventDelivery.Deliver(sdkEvent, eventMetadata, GetEventHandler(sdkEvent));
                    SetSdkEventTriggered(sdkEvent);
                }
            }
        }
        #endregion

        #region Private Methods
        private void SetSdkEventTriggered(SdkEvent sdkEvent)
        {
            if (!_activeEvents.TryGetValue(sdkEvent, out var dict))
            {
                return;
            }

            if ((bool)dict[Triggered])
            {
                return;
            }

            Dictionary<string, object> dict2 = new Dictionary<string, object>(dict);
            dict2[Triggered] = true;
            _activeEvents.TryUpdate(sdkEvent, dict2, dict);
        }

        private bool EventAlreadyTriggered(SdkEvent sdkEvent)
        {
            if (_activeEvents.TryGetValue(sdkEvent, out Dictionary<string, object> triggered))
            {
                triggered.TryGetValue(Triggered, out var trig);
                return (bool)trig;
            }
            return false;
        }

        private EventHandler<EventMetadata> GetEventHandler(SdkEvent sdkEvent)
        {
            if (_activeEvents.TryGetValue(sdkEvent, out var dict))
            {
                dict.TryGetValue(EventHandler, out var handler);
                return (EventHandler<EventMetadata>)handler;
            }
            return null;
        }

        private void UpdateSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent, bool status)
        {
            _internalEventsStatus.AddOrUpdate(sdkInternalEvent, status,
                (_, oldValue) => status);
            _logger.Debug($"EventManager: Internal Event {sdkInternalEvent} status is updated to {status}");
        }

        private bool GetSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent)
        {
            _internalEventsStatus.TryGetValue(sdkInternalEvent, out var status);
            return status;
        }

        private static ConcurrentDictionary<SdkInternalEvent, bool> BuildInternalSdkEventStatus()
        {
            ConcurrentDictionary<SdkInternalEvent, bool> statuses = new ConcurrentDictionary<SdkInternalEvent, bool>();
            statuses.TryAdd(SdkInternalEvent.SdkReady, false);
            statuses.TryAdd(SdkInternalEvent.RuleBasedSegmentsUpdated, false);
            statuses.TryAdd(SdkInternalEvent.SdkTimedOut, false);
            statuses.TryAdd(SdkInternalEvent.SegmentsUpdated, false);
            statuses.TryAdd(SdkInternalEvent.FlagKilledNotification, false);
            statuses.TryAdd(SdkInternalEvent.FlagsUpdated, false);
            return statuses;
        }

        private ValidSdkEvent GetSdkEventIfApplicable(SdkInternalEvent sdkInternalEvent)
        {
            ValidSdkEvent finalSdkEvent;
            finalSdkEvent.valid = false;
            finalSdkEvent.sdkEvent = SdkEvent.SdkUpdate;

            ValidSdkEvent requireAnySdkEvent = CheckRequireAny(sdkInternalEvent);
            if (!requireAnySdkEvent.valid)
            {
                _logger.Debug($"EventHandler: No Event available for internal event {sdkInternalEvent}");
                return requireAnySdkEvent;
            }

            if (requireAnySdkEvent.valid && !EventAlreadyTriggered(requireAnySdkEvent.sdkEvent)
                && ExecutionLimit(requireAnySdkEvent.sdkEvent) == 1)
            {
                _logger.Debug($"EventHandler: Detected Event {requireAnySdkEvent.sdkEvent} is available for internal event {sdkInternalEvent}");
                finalSdkEvent.sdkEvent = requireAnySdkEvent.sdkEvent;
            }

            finalSdkEvent.valid = CheckPrerequisites(finalSdkEvent.sdkEvent);
            if (finalSdkEvent.valid)
            {
                _logger.Debug($"EventHandler: Event {requireAnySdkEvent.sdkEvent} is eligable for notification.");
            }
            return finalSdkEvent;
        }

        private List<SdkEvent> CheckRequireAll()
        {
            List<SdkEvent> events = new List<SdkEvent>();
            foreach (KeyValuePair<SdkEvent, HashSet<SdkInternalEvent>> kvp in _config.RequireAll)
            {
                bool finalStatus = true;
                foreach (var val in kvp.Value)
                {
                    finalStatus &= GetSdkInternalEventStatus(val);
                }
                if (finalStatus
                    && CheckPrerequisites(kvp.Key)
                    && ((ExecutionLimit(kvp.Key) == 1 && !EventAlreadyTriggered(kvp.Key))
                        || (ExecutionLimit(kvp.Key) == -1))
                    && kvp.Value.Count > 0)
                {
                    _logger.Debug($"EventHandler: Event {kvp.Key} is eligable as require all for notification.");
                    events.Add(kvp.Key);
                }
            }

            return events;
        }

        private bool CheckPrerequisites(SdkEvent sdkEvent)
        {
            foreach (KeyValuePair<SdkEvent, HashSet<SdkEvent>> kvp in _config.Prerequisites)
            {
                if (kvp.Key == sdkEvent)
                {
                    if (kvp.Value.Any(x => !EventAlreadyTriggered(x)))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return true;
        }

        private int ExecutionLimit(SdkEvent sdkEvent)
        {
            if (!_config.ExecutionLimits.ContainsKey(sdkEvent))
                return -1;

            _config.ExecutionLimits.TryGetValue(sdkEvent, out int limit);
            return limit;
        }

        private ValidSdkEvent CheckRequireAny(SdkInternalEvent sdkInternalEvent)
        {
            ValidSdkEvent validSdkEvent;
            validSdkEvent.valid = false;
            validSdkEvent.sdkEvent = SdkEvent.SdkUpdate;
            foreach (KeyValuePair<SdkEvent, HashSet<SdkInternalEvent>> kvp in _config.RequireAny)
            {
                if (kvp.Value.Contains(sdkInternalEvent))
                {
                    validSdkEvent.valid = true;
                    validSdkEvent.sdkEvent = kvp.Key;
                    return validSdkEvent;
                }
            }

            return validSdkEvent;
        }
        #endregion
    }
}
