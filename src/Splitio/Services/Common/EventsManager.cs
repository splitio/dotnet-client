using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Common
{
    public class EventsManager<E, I, M> : IEventsManager<E, I, M>
    {
        private readonly ConcurrentDictionary<E, Dictionary<string, object>> _activeSubscriptions;
        private readonly string Triggered = "Triggered";
        private readonly string EventHandler = "EventHandler"; 
        private readonly ConcurrentDictionary<I, bool> _internalEventsStatus;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");
        private readonly EventDelivery<E, M> _eventDelivery;
        private readonly object _lock = new object();

        public EventsManager() 
        {
            _activeSubscriptions = new ConcurrentDictionary<E, Dictionary<string, object>>();
            _internalEventsStatus = new ConcurrentDictionary<I, bool>();
            _eventDelivery = new EventDelivery<E, M>();
        }

        #region Public Methods
        public void Register(E sdkEvent, EventHandler<M> handler)
        {
            _activeSubscriptions.TryGetValue(sdkEvent, out var dict);
            if (dict == null)
            {
                _activeSubscriptions.TryAdd(sdkEvent, new Dictionary<string, object>()
                {
                    {Triggered, false},
                    {EventHandler, handler}
                });
                _logger.Debug($"EventManager: Event {sdkEvent} is registered");
            }
        }

        public void Unregister(E sdkEvent)
        {
            if (_activeSubscriptions.ContainsKey(sdkEvent)
                && _activeSubscriptions.TryGetValue(sdkEvent, out var dict) 
                && dict.Count > 0)
            {
                _activeSubscriptions.TryRemove(sdkEvent, out _);
            }
        }

        public void NotifyInternalEvent(I sdkInternalEvent, M eventMetadata, List<E> eventsToNotify)
        {
            lock (_lock)
            {
                _logger.Debug($"EventHandler: Handling internal event {sdkInternalEvent}");

                foreach (E sdkEvent in eventsToNotify)
                {
                    _logger.Debug($"EventHandler: Firing Sdk event {sdkEvent}");
                    _eventDelivery.Deliver(sdkEvent, eventMetadata, GetEventHandler(sdkEvent));
                    SetSdkEventTriggered(sdkEvent);
                }
            }
        }

        public bool EventAlreadyTriggered(E sdkEvent)
        {
            if (_activeSubscriptions.TryGetValue(sdkEvent, out Dictionary<string, object> triggered))
            {
                triggered.TryGetValue(Triggered, out var trig);
                return (bool)trig;
            }
            return false;
        }

        public bool GetSdkInternalEventStatus(I sdkInternalEvent)
        {
            _internalEventsStatus.TryGetValue(sdkInternalEvent, out var status);
            return status;
        }

        public void UpdateSdkInternalEventStatus(I sdkInternalEvent, bool status)
        {
            _internalEventsStatus.AddOrUpdate(sdkInternalEvent, status,
                (_, oldValue) => status);
            _logger.Debug($"EventManager: Internal Event {sdkInternalEvent} status is updated to {status}");
        }
        #endregion

        #region Private Methods
        private void SetSdkEventTriggered(E sdkEvent)
        {
            if (!_activeSubscriptions.TryGetValue(sdkEvent, out var dict))
            {
                return;
            }

            if ((bool)dict[Triggered])
            {
                return;
            }

            Dictionary<string, object> dict2 = new Dictionary<string, object>(dict);
            dict2[Triggered] = true;
            _activeSubscriptions.TryUpdate(sdkEvent, dict2, dict);
        }

        private EventHandler<M> GetEventHandler(E sdkEvent)
        {
            if (_activeSubscriptions.TryGetValue(sdkEvent, out var dict))
            {
                dict.TryGetValue(EventHandler, out var handler);
                return (EventHandler<M>)handler;
            }
            return null;
        }
        #endregion
    }
}
