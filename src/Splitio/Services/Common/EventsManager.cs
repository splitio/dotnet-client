using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Common
{
    public class EventsManager : IEventsManager
    {
        ConcurrentDictionary<SdkEvent, Dictionary<string, object>> _activeEvents;
        readonly string Triggered = "Triggered";
        readonly string EventHandler = "EventHandler";
        ConcurrentDictionary<SdkInternalEvent, bool> _internalEventsStatus;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");

        public event EventHandler<EventMetadata> RuleBasedSegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagKilledNotificationHandler;
        public event EventHandler<EventMetadata> SegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> SdkReadyHandler;
        public event EventHandler<EventMetadata> SdkTimedOutHandler;

        #region Public Methods
        public EventsManager() 
        {
            _activeEvents = new ConcurrentDictionary<SdkEvent, Dictionary<string, object>>();
            _internalEventsStatus = BuildInternalSdkEventStatus();
        }

        public bool EventAlreadyTriggered(SdkEvent sdkEvent)
        {
            if (_activeEvents.TryGetValue(sdkEvent, out Dictionary<string, object> triggered))
            { 
                triggered.TryGetValue(Triggered, out var trig);
                return (bool)trig;
            }

            return false;
        }

        public bool IsEventRegistered(SdkEvent sdkEvent)
        {
            return _activeEvents.ContainsKey(sdkEvent);
        }

        public Action<EventMetadata> GetCallbackAction(SdkEvent sdkEvent)
        {
            if (_activeEvents.TryGetValue(sdkEvent, out var dict))
            {
                dict.TryGetValue(EventHandler, out var callbackAction);
                return (Action<EventMetadata>)callbackAction;
            }

            return null;
        }

        public void Register(SdkEvent sdkEvent, Action<EventMetadata> callbackAction)
        {
            _activeEvents.TryGetValue(sdkEvent, out var dict);            
            if (dict == null)
            {
                _activeEvents.TryAdd(sdkEvent, new Dictionary<string, object>()
                {
                    {Triggered, false},
                    {EventHandler, callbackAction}
                });
                _logger.Debug($"EventManager: Event {sdkEvent} is registered");
            }
        }
        
        public void Unregister(SdkEvent sdkEvent)
        {
            if (_activeEvents.TryGetValue(sdkEvent, out var dict))
            {
                if (dict.Count > 0)
                {
                    _activeEvents.TryRemove(sdkEvent, out _);
                    _logger.Debug($"EventManager: Event {sdkEvent} is unregistered");
                }
            }
        }

        public void SetSdkEventTriggered(SdkEvent sdkEvent)
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

        public void UpdateSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent, bool status)
        {
            _internalEventsStatus.AddOrUpdate(sdkInternalEvent, status, 
                (_, oldValue) => status);
            _logger.Debug($"EventManager: Internal Event {sdkInternalEvent} status is updated to {status}");
        }

        public bool GetSdkInternalEventStatus(SdkInternalEvent sdkInternalEvent)
        { 
            _internalEventsStatus.TryGetValue(sdkInternalEvent, out var status); 
            return status;
        }

        public void Destroy()
        { 
            _activeEvents.Clear();
            _internalEventsStatus.Clear();
            _logger.Debug("EventManager is destroyed.");
        }

        public virtual void OnSdkInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            EventHandler<EventMetadata> handler = GetEventHandler(sdkInternalEvent);
            if (handler != null)
            {
                _logger.Debug($"EventManager: Triggering handle for Internal Event {sdkInternalEvent}");
                handler(this, eventMetadata);
            }
        }
        #endregion

        #region Private Methods
        private ConcurrentDictionary<SdkInternalEvent, bool> BuildInternalSdkEventStatus()
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
        #endregion
    }
}
