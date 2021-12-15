using Newtonsoft.Json;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Events.Classes
{
    public class EventSdkApiClient : SdkApiClient, IEventSdkApiClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(EventSdkApiClient));

        private const string EventsUrlTemplate = "/api/events/bulk";
        private const int MaxAttempts = 3;

        private readonly ITasksManager _tasksManager;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly int _maxBulkSize;

        public EventSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager tasksManager,
            IWrapperAdapter wrapperAdapter,
            int maxBulkSize) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        {
            _tasksManager = tasksManager;
            _wrapperAdapter = wrapperAdapter;
            _maxBulkSize = maxBulkSize;
        }

        public void SendBulkEventsTask(List<Event> events)
        {
            _tasksManager.Start(async () => await SendBulkEventsAsync(events), new CancellationTokenSource(), "Send Bulk Events.");
        }

        #region Private Methods
        private async Task SendBulkEventsAsync(List<Event> events)
        {
            try
            {
                using (var clock = new Util.SplitStopwatch())
                {
                    clock.Start();

                    if (events.Count <= _maxBulkSize)
                    {
                        await BuildJsonAndPost(events, clock);
                        return;
                    }

                    while (events.Count > 0)
                    {
                        var bulkToPost = Util.Helper.TakeFromList(events, _maxBulkSize);

                        await BuildJsonAndPost(bulkToPost, clock);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Exception caught sending bulk of events", ex);
            }
        }

        private async Task BuildJsonAndPost(List<Event> events, Util.SplitStopwatch clock)
        {
            var eventsJson = JsonConvert.SerializeObject(events, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            for (int i = 0; i < MaxAttempts; i++)
            {
                if (i > 0) _wrapperAdapter.TaskDelay(500).Wait();

                var response = await ExecutePost(EventsUrlTemplate, eventsJson);

                RecordTelemetry(nameof(SendBulkEventsTask), (int)response.statusCode, response.content, ResourceEnum.EventSync, clock);

                if (response.statusCode >= System.Net.HttpStatusCode.OK && response.statusCode < System.Net.HttpStatusCode.Ambiguous)
                {
                    _log.Debug($"Post bulk events success in {i} attempts.");
                    return;
                }
            }

            _log.Debug($"Post bulk events fail after {MaxAttempts} attempts.");
        }
        #endregion
    }
}
