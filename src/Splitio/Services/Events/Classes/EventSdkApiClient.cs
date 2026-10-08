using Newtonsoft.Json;
using Splitio.Commons.Dto;
using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Telemetry.Domain.Enums;
using Splitio.Commons.Telemetry.Storages;
using Splitio.Services.Common;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Events.Classes
{
    public class EventSdkApiClient : IEventSdkApiClient
    {
        private const int MaxAttempts = 3; 
        
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(EventSdkApiClient));
        
        private readonly ISplitioHttpClient _httpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly int _maxBulkSize;
        private readonly string _baseUrl;

        public EventSdkApiClient(ISplitioHttpClient httpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            string baseUrl,
            int maxBulkSize)
        {
            _httpClient = httpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _maxBulkSize = maxBulkSize;
            _baseUrl = baseUrl;
        }

        public async Task SendBulkEventsAsync(List<Event> events)
        {
            try
            {
                using (var clock = new SplitStopwatch())
                {
                    clock.Start();

                    if (events.Count <= _maxBulkSize)
                    {
                        await BuildJsonAndPostAsync(events, clock);
                        return;
                    }

                    while (events.Count > 0)
                    {
                        var bulkToPost = Commons.Shared.Utils.Helper.TakeFromList(events, _maxBulkSize);

                        await BuildJsonAndPostAsync(bulkToPost, clock);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Exception caught sending bulk of events", ex);
            }
        }
        
        #region Private Methods
        private async Task BuildJsonAndPostAsync(List<Event> events, Commons.Shared.Utils.SplitStopwatch clock)
        {
            var eventsJson = JsonConvertWrapper.SerializeObjectIgnoreNullValue(events);

            for (int i = 0; i < MaxAttempts; i++)
            {
                if (i > 0) await Task.Delay(500);

                var response = await _httpClient.PostAsync(EventsUrl, eventsJson);

                Commons.Shared.Utils.Helper.RecordTelemetrySync(nameof(SendBulkEventsAsync), response, ResourceEnum.EventSync, clock, _telemetryRuntimeProducer, _log);

                if (response.IsSuccessStatusCode)
                {
                    _log.Debug($"Post bulk events success in {i} attempts.");
                    return;
                }
            }

            _log.Debug($"Post bulk events fail after {MaxAttempts} attempts.");
        }

        private string EventsUrl => $"{_baseUrl}/api/events/bulk";
        #endregion
    }
}
