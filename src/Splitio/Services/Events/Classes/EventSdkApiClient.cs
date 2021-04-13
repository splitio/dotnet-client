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

            if ((int)response.statusCode < (int)HttpStatusCode.OK || (int)response.statusCode >= (int)HttpStatusCode.Ambiguous)
            {
                _log.Error($"Http status executing SendBulkEvents: {response.statusCode.ToString()} - {response.content}");

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.EventSync, (int)response.statusCode);
            }
        }
    }
}
