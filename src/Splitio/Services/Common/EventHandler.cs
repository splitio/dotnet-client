using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Common
{
    public class EventHandler : IEventHandler
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventHandler");
        EventsManagerConfig _config;
        EventsManager _eventsManager;
        EventDelivery _eventDelivery;
        struct ValidSdkEvent
        {
            public SdkEvent sdkEvent;
            public bool valid;
        }

        #region Public Methods
        public EventHandler(EventsManagerConfig eventsManagerConfig, EventsManager eventsManager, EventDelivery eventDelivery)
        {
            _config = eventsManagerConfig;
            _eventsManager = eventsManager;
            _eventDelivery = eventDelivery;

            _eventsManager.RuleBasedSegmentsUpdatedHandler += EventManager_RuleBasedSegmentsUpdatedHandler;
            _eventsManager.FlagKilledNotificationHandler += EventManager_FlagKilledNotificationHandler;
            _eventsManager.FlagsUpdatedHandler += EventManager_FlagsUpdatedHandler;
            _eventsManager.SegmentsUpdatedHandler += EventManager_SegmentsUpdatedHandler;
            _eventsManager.SdkReadyHandler += EventManager_SdkReadyHandler;
            _eventsManager.SdkTimedOutHandler += EventManager_SdkTimedOutHandler;
        }

        public void Handle(SdkEvent sdkEvent, EventMetadata eventMetadata)
        {
            _logger.Debug($"EventHandler: Delivering notification for Event {sdkEvent}");
            _eventDelivery.Deliver(sdkEvent, eventMetadata);
        }
        #endregion

        #region Private Methods
        private void HandleInternalEvent(EventMetadata eventMetadata, SdkInternalEvent sdkInternalEvent)
        {
            _logger.Debug($"EventHandler: Handling internal event {sdkInternalEvent}");
            ValidSdkEvent eventToNotify = GetSdkEventIfApplicable(sdkInternalEvent);
            if (eventToNotify.valid)
            {
                Handle(eventToNotify.sdkEvent, eventMetadata);
            }

            foreach (SdkEvent sdkEvent in CheckRequireAll())
            {
                Handle(sdkEvent, eventMetadata);
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

            if (requireAnySdkEvent.valid && !_eventsManager.EventAlreadyTriggered(requireAnySdkEvent.sdkEvent)
                && ExecutionLimit(requireAnySdkEvent.sdkEvent) == 1)
            {
                _logger.Debug($"EventHandler: Detected Event {requireAnySdkEvent.sdkEvent} is available for internal event {sdkInternalEvent}");
                finalSdkEvent.sdkEvent = requireAnySdkEvent.sdkEvent;
            }

            if (!_eventsManager.IsEventRegistered(finalSdkEvent.sdkEvent))
            {
                finalSdkEvent.valid = requireAnySdkEvent.valid;
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
                if (finalStatus && _eventsManager.IsEventRegistered(kvp.Key) 
                    && CheckPrerequisites(kvp.Key)
                    && ((ExecutionLimit(kvp.Key) == 1 && !_eventsManager.EventAlreadyTriggered(kvp.Key)) 
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
                    foreach (var val in kvp.Value)
                    {
                        if (!_eventsManager.GetSdkInternalEventStatus(val))
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
                foreach (var val in kvp.Value)
                {
                    if (val == sdkInternalEvent)
                    {
                        validSdkEvent.valid = true;
                        validSdkEvent.sdkEvent = kvp.Key;
                        return validSdkEvent;
                    }
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
