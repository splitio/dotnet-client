using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryRuntimeConsumer : TelemetryStorageBase, ITelemetryRuntimeConsumer
    {
        public TelemetryRuntimeConsumer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public long GetEventsStats(EventsEnum data)
        {
            return _telemetryStorage.EventsDataRecords.TryGetValue(data, out long value) ? value : 0;
        }

        public long GetImpressionsStats(ImpressionsEnum data)
        {
            return _telemetryStorage.ImpressionsDataRecords.TryGetValue(data, out long value) ? value : 0;
        }

        public LastSynchronization GetLastSynchronizations()
        {
            return new LastSynchronization
            {
                Splits = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.SplitSync, out long splitsValue) ? splitsValue : 0,
                Segments = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.SegmentSync, out long segValue) ? segValue : 0,
                Impressions = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.ImpressionSync, out long impValue) ? impValue : 0,
                ImpressionCount = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.ImpressionCountSync, out long impCountValue) ? impCountValue : 0,
                Events = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.EventSync, out long evetsValue) ? evetsValue : 0,
                Telemetry = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.TelemetrySync, out long telValue) ? telValue : 0,
                Token = _telemetryStorage.LastSynchronizationRecords.TryGetValue(ResourceEnum.TokenSync, out long tokenValue) ? tokenValue : 0
            };
        }

        public long GetSessionLength()
        {
            return _telemetryStorage.SdkRecords.TryGetValue(SdkRecordsEnum.Session, out long value) ? value : 0;
        }

        public long PopAuthRejections()
        {
            if (!_telemetryStorage.PushCounters.TryGetValue(PushCountersEnum.AuthRejecttions, out long authRejections))
            {
                return 0;
            }

            _telemetryStorage.PushCounters[PushCountersEnum.AuthRejecttions] = 0;

            return authRejections;
        }

        public HTTPErrors PopHttpErrors()
        {
            var erros = new HTTPErrors
            {
                Events = _telemetryStorage.HttpErrors[ResourceEnum.EventSync],
                Impressions = _telemetryStorage.HttpErrors[ResourceEnum.ImpressionSync],
                ImpressionCount = _telemetryStorage.HttpErrors[ResourceEnum.ImpressionCountSync],
                Segments = _telemetryStorage.HttpErrors[ResourceEnum.SegmentSync],
                Splits = _telemetryStorage.HttpErrors[ResourceEnum.SplitSync],
                Telemetry = _telemetryStorage.HttpErrors[ResourceEnum.TelemetrySync],
                Token = _telemetryStorage.HttpErrors[ResourceEnum.TokenSync]
            };

            _telemetryStorage.HttpErrors.Clear();
            _telemetryStorage.InitHttpErrors();

            return erros;
        }

        public HTTPLatencies PopHttpLatencies()
        {
            var latencies = new HTTPLatencies
            {
                Events = _telemetryStorage.HttpLatencies[ResourceEnum.EventSync],
                Impressions = _telemetryStorage.HttpLatencies[ResourceEnum.ImpressionSync],
                ImpressionCount = _telemetryStorage.HttpLatencies[ResourceEnum.ImpressionCountSync],
                Segments = _telemetryStorage.HttpLatencies[ResourceEnum.SegmentSync],
                Splits = _telemetryStorage.HttpLatencies[ResourceEnum.SplitSync],
                Telemetry = _telemetryStorage.HttpLatencies[ResourceEnum.TelemetrySync],
                Token = _telemetryStorage.HttpLatencies[ResourceEnum.TokenSync]
            };

            _telemetryStorage.HttpLatencies.Clear();
            _telemetryStorage.InitHttpLatencies();

            return latencies;
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            lock (_telemetryStorage.StreamingEventsLock)
            {
                var events = _telemetryStorage.StreamingEvents;
                _telemetryStorage.StreamingEvents.Clear();

                return events;
            }
        }

        public IList<string> PopTags()
        {
            lock (_telemetryStorage.TagsLock)
            {
                var tags = _telemetryStorage.Tags;
                _telemetryStorage.Tags.Clear();

                return tags;
            }
        }

        public long PopTokenRefreshes()
        {
            if (!_telemetryStorage.PushCounters.TryGetValue(PushCountersEnum.TokenRefreshes, out long tokenRefreshes))
            {
                return 0;
            }

            _telemetryStorage.PushCounters[PushCountersEnum.TokenRefreshes] = 0;

            return tokenRefreshes;
        }
    }
}
