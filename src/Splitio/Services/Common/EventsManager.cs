using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Concurrent;

namespace Splitio.Services.Common
{
    public class EventsManager : IEventsManager
    {
        ConcurrentDictionary<SdkEvent, bool> _eventsStatus;
        ConcurrentDictionary<SdkInternalEvent, bool> _internalEventsStatus;
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger("EventsManager");

        public event EventHandler<EventMetadata> RuleBasedSegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagsUpdatedHandler;
        public event EventHandler<EventMetadata> FlagKilledNotificationHandler;
        public event EventHandler<EventMetadata> SegmentsUpdatedHandler;
        public event EventHandler<EventMetadata> SdkReadyHandler;
        public event EventHandler<EventMetadata> SdkTimedOutHandler;

        public event EventHandler<EventMetadata> PublicSdkReadyHandler;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;
        public event EventHandler<EventMetadata> PublicSdkTimedOutHandler;

        #region Public Methods
        public EventsManager() 
        {
            _eventsStatus = BuildSdkEventStatus();
            _internalEventsStatus = BuildInternalSdkEventStatus();
        }

        public void UpdateSdkEventStatus(SdkEvent sdkEvent, bool status)
        {
            _eventsStatus.AddOrUpdate(sdkEvent, status,
                (_, oldValue) => status);
            _logger.Debug($"EventManager: Sdk Event {sdkEvent} status is updated to {status}");
        }

        public bool GetSdkEventStatus(SdkEvent sdkEvent)
        {
            _eventsStatus.TryGetValue(sdkEvent, out var status);
            return status;
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

        public virtual void OnSdkInternalEvent(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        {
            EventHandler<EventMetadata> handler = GetEventHandler(sdkInternalEvent);
            if (handler != null)
            {
                _logger.Debug($"EventManager: Triggering handle for Internal Event {sdkInternalEvent}");
                try
                {
                    handler(this, eventMetadata);
                }
                catch (Exception e)
                {
                    _logger.Error($"EventManager: Failed to run internal event {sdkInternalEvent} handler {e.Message}", e);
                }
            }
        }

        public virtual void OnSdkEvent(SdkEvent sdkEvent, EventMetadata eventMetadata)
        {
            EventHandler<EventMetadata> handler = GetPublicEventHandler(sdkEvent);
            if (handler != null)
            {
                _logger.Debug($"EventManager: Triggering handle for Sdk Event {sdkEvent}");
                try
                {
                    handler(this, eventMetadata);
                }
                catch (Exception e)
                {
                    _logger.Error($"EventManager: Failed to run event {sdkEvent} handler {e.Message}", e);
                }
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

        private ConcurrentDictionary<SdkEvent, bool> BuildSdkEventStatus()
        {
            ConcurrentDictionary<SdkEvent, bool> statuses = new ConcurrentDictionary<SdkEvent, bool>();
            statuses.TryAdd(SdkEvent.SdkReady, false);
            statuses.TryAdd(SdkEvent.SdkUpdate, false);
            statuses.TryAdd(SdkEvent.SdkReadyTimeout, false);
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

        private EventHandler<EventMetadata> GetPublicEventHandler(SdkEvent sdkEvent)
        {
            switch (sdkEvent)
            {
                case SdkEvent.SdkReady: return PublicSdkReadyHandler;
                case SdkEvent.SdkReadyTimeout: return PublicSdkTimedOutHandler;
                case SdkEvent.SdkUpdate: return PublicSdkUpdateHandler;
            }

            return null;
        }
        #endregion
    }
}
