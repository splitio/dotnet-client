using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Splitio.Services.Events.Classes
{
    public class EventsLog : IEventsLog
    {
        private static readonly long MAX_SIZE_BYTES = 5 * 1024 * 1024L;
        
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(EventsLog));

        private readonly IEventSdkApiClient _apiClient;
        private readonly ISimpleProducerCache<WrappedEvent> _wrappedEventsCache;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ISplitTask _task;
        
        private readonly object _lock = new object();
        private readonly object _sendBulkEventsLock = new object();

        private long _acumulateSize;
        
        public EventsLog(IEventSdkApiClient apiClient,
            ISimpleCache<WrappedEvent> eventsCache,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISplitTask task,
            int maximumNumberOfKeysToCache = -1)
        {
            _wrappedEventsCache = (eventsCache as ISimpleProducerCache<WrappedEvent>) ?? new InMemorySimpleCache<WrappedEvent>(new BlockingQueue<WrappedEvent>(maximumNumberOfKeysToCache));
            _apiClient = apiClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _task = task;
            _task.SetEventHandler((object sender, ElapsedEventArgs e) => SendBulkEvents());
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_task.IsRunning()) return;

                _task.Start();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_task.IsRunning())
                    return;

                _task.Stop();
                SendBulkEvents();
            }
        }

        public void Log(WrappedEvent wrappedEvent)
        {
            var dropped = _wrappedEventsCache.AddItems(new List<WrappedEvent> { wrappedEvent });

            if (dropped == 0)
            {
                _acumulateSize += wrappedEvent.Size;
            }

            RecordStats(dropped);

            if (_wrappedEventsCache.HasReachedMaxSize() || _acumulateSize >= MAX_SIZE_BYTES)
            {
                SendBulkEvents();
            }
        }

        private void SendBulkEvents()
        {
            lock (_sendBulkEventsLock)
            {
                if (_wrappedEventsCache.IsEmpty()) return;

                if (_wrappedEventsCache.HasReachedMaxSize())
                {
                    _log.Warn("Split SDK events queue is full. Events may have been dropped. Consider increasing capacity.");
                }

                var wrappedEvents = _wrappedEventsCache.FetchAllAndClear();

                if (wrappedEvents.Count <= 0) return;

                try
                {
                    var events = wrappedEvents
                        .Select(x => x.Event)
                        .ToList();

                    _apiClient.SendBulkEventsTask(events);

                    _acumulateSize = 0;
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught updating events.", e);
                }
            }
        }

        private void RecordStats(int dropped)
        {
            if (_telemetryRuntimeProducer == null) return;

            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsQueued, 1 - dropped);
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsDropped, dropped);
        }
    }
}