using Newtonsoft.Json;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Net;

namespace Splitio.Services.Events.Classes
{
    public class EventSdkApiClient : SdkApiClient, IEventSdkApiClient
    {
        private const string EventsUrlTemplate = "/api/events/bulk";
        
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(EventSdkApiClient));

        public EventSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        { }

        public async void SendBulkEvents(List<Event> events)
        {
            var eventsJson = JsonConvert.SerializeObject(events, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var response = await ExecutePost(EventsUrlTemplate, eventsJson);

            RecordTelemetry(nameof(SendBulkEvents), (int)response.statusCode, response.content, ResourceEnum.EventSync);
        }
    }
}
