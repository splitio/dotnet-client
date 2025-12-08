using Splitio.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class EventsManager : IEventsManager
    {
        EventsManagerConfig _config;
        Dictionary<SdkEvent, Dictionary<string, object>> _activeEvents;
        string Triggered = "Triggered";
        string EventHandler = "EventHandler";
        Dictionary<SdkInternalEvent, bool> _internalEventsStatus;

        public EventsManager(EventsManagerConfig eventsManagerConfig) 
        {
            _config = eventsManagerConfig;
            _activeEvents = new Dictionary<SdkEvent, Dictionary<string, object>>();
            _internalEventsStatus = new Dictionary<SdkInternalEvent, bool>();
        }

        public Task NotifyInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            return Task.CompletedTask;
        }

        public bool EventAlreadyTriggered(SdkEvent sdkEvent)
        {
            _activeEvents.TryGetValue(sdkEvent, out Dictionary<string, object> triggered);
            triggered.TryGetValue(Triggered, out var trig);
            return (bool)trig;
        }

        public bool IsEventRegistered(SdkEvent sdkEvent)
        {
            return _activeEvents.ContainsKey(sdkEvent);
        }

        public void Register(SdkEvent sdkEvent, IEventHandler eventHandler)
        {
            if (!_activeEvents.ContainsKey(sdkEvent))
            {
                _activeEvents[sdkEvent] = new Dictionary<string, object>()
                {
                    {Triggered, false},
                    {EventHandler, eventHandler}
                };
            }
        }

        public void Unregister(SdkEvent sdkEvent)
        {
            if (_activeEvents.ContainsKey(sdkEvent))
            {
                _activeEvents.Remove(sdkEvent);
            }
        }

        public void UpdateSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent, bool status)
        {
            _internalEventsStatus[sdkInternalEvent] = status;
        }

        public bool GetSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent)
        { 
            return _internalEventsStatus[sdkInternalEvent]; 
        }

        public void Destroy()
        { 
            _activeEvents.Clear(); 
        }

        public virtual void OnSdkInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            EventHandler<EventMetadata> handler = GetEventHandler(sdkInternalEvent);
            if (handler != null)
            {
                handler(this, eventMetadata);
            }
        }

        private EventHandler<EventMetadata> GetEventHandler(SdkInternalEvent sdkInternalEvent)
        {
            switch (sdkInternalEvent)
            {
                case SdkInternalEvent.RuleBasedSegmentsUpdated: return RuleBasedSegmentsUpdatedHandler;
                case SdkInternalEvent.FlagsUpdated: return FlagsUpdatedHandler;
                case SdkInternalEvent.FlagKilledNotification: return FlagKilledNotificationHandler;
                case SdkInternalEvent.SegmentsUpdated: return SegmentsUpdatedHandler;
                case SdkInternalEvent.SdkReady: return SdkReadyHandler;
                case SdkInternalEvent.SdkTimedOut: return SdkTimedOutHandler;
            }

            return null;
        }

        public event EventHandler<EventMetadata> RuleBasedSegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagKilledNotificationHandler;
        public event EventHandler<EventMetadata> SegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> SdkReadyHandler;
        public event EventHandler<EventMetadata> SdkTimedOutHandler;
    }
}
