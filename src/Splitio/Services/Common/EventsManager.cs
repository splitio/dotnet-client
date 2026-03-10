using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Common
{
    public class EventsManager<E, I, M> : IEventsManager<E, I, M>
    {
        public struct ValidSdkEvent
        {
            public E SdkEvent { get; set; }
            public bool Valid { get; set; }
        }

        private struct PublicEventProperties
        {
            public bool Triggered;
            public Action<M> EventHandler;
        }
        private readonly ConcurrentDictionary<E, PublicEventProperties> _activeSubscriptions;
        private readonly ConcurrentDictionary<I, bool> _internalEventsStatus;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");
        private readonly IEventDelivery<E, M> _eventDelivery;
        private readonly object _lock = new object();
        public EventManagerConfigData<E, I> _managerConfig { get; private set; }

        public EventsManager(EventManagerConfigData<E, I> eventsManagerConfig, IEventDelivery<E, M> eventDelivery) 
        {
            _activeSubscriptions = new ConcurrentDictionary<E, PublicEventProperties>();
            _internalEventsStatus = new ConcurrentDictionary<I, bool>();
            _eventDelivery = eventDelivery;
            _managerConfig = eventsManagerConfig;
        }

        #region Public Methods
        public void Register(E sdkEvent, Action<M> handler)
        {
            if (_activeSubscriptions.TryGetValue(sdkEvent, out var _))
            {
                return;
            }

            _activeSubscriptions.TryAdd(sdkEvent, new PublicEventProperties
            {
                Triggered = false,
                EventHandler = handler
            });
            _logger.Debug($"EventsManager: Event {sdkEvent} is registered");
        }

        public void Unregister(E sdkEvent)
        {
            if (_activeSubscriptions.TryRemove(sdkEvent, out _))
            {
                _logger.Debug($"EventsManager: Event {sdkEvent} is Unregistered");
            }
        }

        public void NotifyInternalEvent(I sdkInternalEvent, M eventMetadata)
        {
            lock (_lock)
            {
                foreach (E sortedEvent in _managerConfig.EvaluationOrder.Where(x => GetSdkEventIfApplicable(sdkInternalEvent).Contains(x)))
                {
                    _logger.Debug($"EventsManager: Firing Sdk event {sortedEvent}");
                    _eventDelivery.Deliver(sortedEvent, eventMetadata, GetEventHandler(sortedEvent));
                    SetSdkEventTriggered(sortedEvent);
                }
            }
        }

        public bool EventAlreadyTriggered(E sdkEvent)
        {
            if (_activeSubscriptions.TryGetValue(sdkEvent, out PublicEventProperties eventProperties))
            {
                return eventProperties.Triggered;
            }
            return false;
        }
        #endregion

        #region Private Methods
        private bool GetSdkInternalEventStatus(I sdkInternalEvent)
        {
            _internalEventsStatus.TryGetValue(sdkInternalEvent, out var status);
            return status;
        }

        private void UpdateSdkInternalEventStatus(I sdkInternalEvent, bool status)
        {
            _internalEventsStatus.AddOrUpdate(sdkInternalEvent, status,
                (_, oldValue) => status);
        }

        private void SetSdkEventTriggered(E sdkEvent)
        {
            if (!_activeSubscriptions.TryGetValue(sdkEvent, out var eventData))
            {
                return;
            }

            if (eventData.Triggered)
            {
                return;
            }

            PublicEventProperties newEventData = eventData;
            newEventData.Triggered = true;
            _activeSubscriptions.TryUpdate(sdkEvent, newEventData, eventData);
        }

        private Action<M> GetEventHandler(E sdkEvent)
        {
            if (!_activeSubscriptions.TryGetValue(sdkEvent, out var eventData))
            {
                return null;
            }

            return eventData.EventHandler;
        }

        public List<E> GetSdkEventIfApplicable(I sdkInternalEvent)
        {
            ValidSdkEvent finalSdkEvent = new ValidSdkEvent
            {
                Valid = false
            };
            UpdateSdkInternalEventStatus(sdkInternalEvent, true);
            List<E> eventsToFire = new List<E>();

            ValidSdkEvent requireAnySdkEvent = CheckRequireAny(sdkInternalEvent);
            if (requireAnySdkEvent.Valid)
            {
                if ((!EventAlreadyTriggered(requireAnySdkEvent.SdkEvent)
                    && ExecutionLimit(requireAnySdkEvent.SdkEvent) == 1) || ExecutionLimit(requireAnySdkEvent.SdkEvent) == -1)
                {
                    finalSdkEvent.SdkEvent = requireAnySdkEvent.SdkEvent;
                }

                finalSdkEvent.Valid = CheckPrerequisites(finalSdkEvent.SdkEvent)
                    && CheckSuppressedBy(finalSdkEvent.SdkEvent);

                if (finalSdkEvent.Valid)
                {
                    eventsToFire.Add(finalSdkEvent.SdkEvent);
                }
            }

            foreach (E sdkEvent in CheckRequireAll())
            {
                eventsToFire.Add(sdkEvent);
            }

            return eventsToFire;
        }

        private List<E> CheckRequireAll()
        {
            List<E> events = new List<E>();
            foreach (KeyValuePair<E, HashSet<I>> kvp in _managerConfig.RequireAll)
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
                    events.Add(kvp.Key);
                }
            }

            return events;
        }

        private bool CheckPrerequisites(E sdkEvent)
        {
            if (_managerConfig.Prerequisites.Any(kvp => kvp.Key.Equals(sdkEvent) &&
                kvp.Value.Any(x => !EventAlreadyTriggered(x))))
            {
                return false;
            }

            return true;
        }

        private bool CheckSuppressedBy(E sdkEvent)
        {
            if (_managerConfig.SuppressedBy.Any(kvp => kvp.Key.Equals(sdkEvent) &&
                kvp.Value.Any(x => EventAlreadyTriggered(x))))
            {
                return false;
            }

            return true;
        }

        private int ExecutionLimit(E sdkEvent)
        {
            if (!_managerConfig.ExecutionLimits.TryGetValue(sdkEvent, out int limit))
            {
                return -1;
            }

            return limit;
        }

        private ValidSdkEvent CheckRequireAny(I sdkInternalEvent)
        {
            ValidSdkEvent validSdkEvent = new ValidSdkEvent
            {
                Valid = false
            };

            var sdkEvent = _managerConfig.RequireAny.Where(kvp => kvp.Value.Contains(sdkInternalEvent));
            if (sdkEvent.Any())
            {
                validSdkEvent.Valid = true;
                validSdkEvent.SdkEvent = sdkEvent.First().Key;
                return validSdkEvent;
            }

            return validSdkEvent;
        }
        #endregion
    }
}
