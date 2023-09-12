using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public class SSEHandler : ISSEHandler
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SSEHandler));

        private readonly ISplitsWorker _splitsWorker;
        private readonly ISegmentsWorker _segmentsWorker;
        private readonly INotificationProcessor _notificationPorcessor;
        private readonly INotificationManagerKeeper _notificationManagerKeeper;
        private readonly IEventSourceClient _eventSourceClient;
        private readonly string _streaminServiceUrl;

        public SSEHandler(string streaminServiceUrl,
            ISplitsWorker splitsWorker,
            ISegmentsWorker segmentsWorker,
            INotificationProcessor notificationPorcessor,
            INotificationManagerKeeper notificationManagerKeeper,
            IEventSourceClient eventSourceClient = null)
        {
            _streaminServiceUrl = streaminServiceUrl;
            _splitsWorker = splitsWorker;
            _segmentsWorker = segmentsWorker;
            _notificationPorcessor = notificationPorcessor;
            _notificationManagerKeeper = notificationManagerKeeper;
            _eventSourceClient = eventSourceClient;

            _eventSourceClient.EventReceived += EventReceived;
        }

        #region Public Methods
        public bool Start(string token, string channels)
        {
            try
            {
                _log.Debug($"SSE Handler starting...");
                var url = $"{_streaminServiceUrl}?channels={channels}&v=1.1&accessToken={token}";

                return _eventSourceClient.Connect(url);
            }
            catch (Exception ex)
            {
                _log.Error($"SSE Handler Start: {ex.Message}");
            }

            return false;
        }

        public async Task StopAsync()
        {
            try
            {
                if (_eventSourceClient != null)
                {
                    await _eventSourceClient.DisconnectAsync();
                    _log.Debug($"SSE Handler stoped...");
                }
            }
            catch (Exception ex)
            {
                _log.Debug($"SSE Handler Stop: {ex.Message}");
            }
        }

        public void StartWorkers()
        {
            _splitsWorker.Start();
            _segmentsWorker.Start();
        }

        public async Task StopWorkersAsync()
        {
            await _splitsWorker.StopAsync();
            await _segmentsWorker.StopAsync();
        }
        #endregion

        #region Private Methods
        private void EventReceived(object sender, EventReceivedEventArgs e)
        {
            _log.Debug($"Event received {e.Event.Type}, {e.Event.Channel}");

            if (e.Event.Type == NotificationType.OCCUPANCY || e.Event.Type == NotificationType.CONTROL)
            {
                _notificationManagerKeeper.HandleIncomingEvent(e.Event);
            }
            else
            {
                _notificationPorcessor.Proccess(e.Event);
            }
        }
        #endregion
    }
}
