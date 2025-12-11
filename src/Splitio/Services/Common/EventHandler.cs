using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Common
{

    public class EventHandler : IEventHandler
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventHandler");
        private readonly EventsManagerConfig _config;
        private readonly EventsManager _eventsManager;
        struct ValidSdkEvent
        {
            public SdkEvent sdkEvent;
            public bool valid;
        }

        #region Public Methods
        public EventHandler(EventsManagerConfig eventsManagerConfig, EventsManager eventsManager) 
        {
            _config = eventsManagerConfig;
            _eventsManager = eventsManager;
        }

        public void SubscribeInternalEvents()
        {
            _eventsManager.RuleBasedSegmentsUpdatedHandler += EventManager_RuleBasedSegmentsUpdatedHandler;
            _eventsManager.FlagKilledNotificationHandler += EventManager_FlagKilledNotificationHandler;
            _eventsManager.FlagsUpdatedHandler += EventManager_FlagsUpdatedHandler;
            _eventsManager.SegmentsUpdatedHandler += EventManager_SegmentsUpdatedHandler;
            _eventsManager.SdkReadyHandler += EventManager_SdkReadyHandler;
            _eventsManager.SdkTimedOutHandler += EventManager_SdkTimedOutHandler;
            _logger.Debug("EventHandler: Subscription to internal events are added.");
        }

        public void ClearInternalEventsSubscription()
        {
            _eventsManager.RuleBasedSegmentsUpdatedHandler -= EventManager_RuleBasedSegmentsUpdatedHandler;
            _eventsManager.FlagKilledNotificationHandler -= EventManager_FlagKilledNotificationHandler;
            _eventsManager.FlagsUpdatedHandler -= EventManager_FlagsUpdatedHandler;
            _eventsManager.SegmentsUpdatedHandler -= EventManager_SegmentsUpdatedHandler;
            _eventsManager.SdkReadyHandler -= EventManager_SdkReadyHandler;
            _eventsManager.SdkTimedOutHandler -= EventManager_SdkTimedOutHandler;
            _logger.Debug("EventHandler: Subscription to internal events are removed.");
        }
        #endregion

        #region Private Methods
        private void HandleInternalEvent(EventMetadata eventMetadata, SdkInternalEvent sdkInternalEvent)
        {
            _logger.Debug($"EventHandler: Handling internal event {sdkInternalEvent}");
            ValidSdkEvent eventToNotify = GetSdkEventIfApplicable(sdkInternalEvent);
            if (eventToNotify.valid)
            {
                _logger.Debug($"EventHandler: Firing Sdk event {eventToNotify.sdkEvent}");
                _eventsManager.OnSdkEvent(eventToNotify.sdkEvent, eventMetadata);
                _eventsManager.UpdateSdkEventStatus(eventToNotify.sdkEvent, true);
            }

            foreach (SdkEvent sdkEvent in CheckRequireAll())
            {
                _logger.Debug($"EventHandler: Firing Sdk event {sdkEvent}");
                _eventsManager.OnSdkEvent(sdkEvent, eventMetadata);
                _eventsManager.UpdateSdkEventStatus(sdkEvent, true);
            }
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

            if (requireAnySdkEvent.valid && !_eventsManager.GetSdkEventStatus(requireAnySdkEvent.sdkEvent)
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
                    finalStatus &= _eventsManager.GetSdkInternalEventStatus(val);
                }
                if (finalStatus  
                    && CheckPrerequisites(kvp.Key)
                    && ((ExecutionLimit(kvp.Key) == 1 && !_eventsManager.GetSdkEventStatus(kvp.Key)) 
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
            foreach (KeyValuePair<SdkEvent, HashSet<SdkInternalEvent>> kvp in _config.Prerequisites)
            {
                if (kvp.Key == sdkEvent)
                {
                    if (kvp.Value.Any(x => !_eventsManager.GetSdkInternalEventStatus(x)))
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

        void EventManager_RuleBasedSegmentsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.RuleBasedSegmentsUpdated, true);    
            HandleInternalEvent(eventMetadata, SdkInternalEvent.RuleBasedSegmentsUpdated);
        }

        void EventManager_FlagsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagsUpdated, true);
            HandleInternalEvent(eventMetadata, SdkInternalEvent.FlagsUpdated);
        }

        void EventManager_FlagKilledNotificationHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagKilledNotification, true);
            HandleInternalEvent(eventMetadata, SdkInternalEvent.FlagKilledNotification);
        }

        void EventManager_SegmentsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SegmentsUpdated, true);
            HandleInternalEvent(eventMetadata, SdkInternalEvent.SegmentsUpdated);
        }

        void EventManager_SdkReadyHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkReady, true);
            HandleInternalEvent(eventMetadata, SdkInternalEvent.SdkReady);
        }

        void EventManager_SdkTimedOutHandler(object sender, EventMetadata eventMetadata)
        {
            _eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkTimedOut, true);
            HandleInternalEvent(eventMetadata, SdkInternalEvent.SdkTimedOut);
        }
        #endregion
    }
}
