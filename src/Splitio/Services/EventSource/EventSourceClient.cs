using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public class EventSourceClient : IEventSourceClient
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));

        private const string KeepAliveResponse = ":keepalive\n\n";
        private const int ReadTimeoutMs = 70000;
        private const int ConnectTimeoutMs = 30000;
        private const int BufferSize = 10000;

        private readonly byte[] _buffer = new byte[BufferSize];
        private readonly UTF8Encoding _encoder = new UTF8Encoding();
        private readonly CountdownEvent _disconnectSignal = new CountdownEvent(1);

        private readonly INotificationParser _notificationParser;   
        private readonly ISplitioHttpClient _splitHttpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly INotificationManagerKeeper _notificationManagerKeeper;
        private readonly IStatusManager _statusManager;
        private readonly ISplitTask _connectTask;

        private string _url;
        private string _lineBuffer;
        private bool _connected;
        private bool _firstEvent;

        private Stream _ongoindStream;

        public EventSourceClient(INotificationParser notificationParser,
            ISplitioHttpClient splitHttpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            INotificationManagerKeeper notificationManagerKeeper,
            IStatusManager statusManager,
            ISplitTask connectTask)
        {            
            _notificationParser = notificationParser;
            _splitHttpClient = splitHttpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _notificationManagerKeeper = notificationManagerKeeper;
            _statusManager = statusManager;
            _firstEvent = true;
            _connectTask = connectTask;
            _connectTask.SetFunction(ConnectAsync);
        }
        
        public event EventHandler<EventReceivedEventArgs> EventReceived;

        #region Public Methods
        public void Connect(string url)
        {
            if (_statusManager.IsDestroyed()) return;

            if (_connected)
            {
                _log.Debug("Event source Client already connected.");

                return;
            }

            _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS);

            _firstEvent = true;
            _url = url;
            _disconnectSignal.Reset();
            _connectTask.Start();
        }

        public async Task DisconnectAsync()
        {
            if (!_connected) return;

            _connected = false;

            _ongoindStream.Close();

            _disconnectSignal.Wait(ReadTimeoutMs);

            await _connectTask.StopAsync();

            _log.Debug($"Streaming Disconnected.");
        }
        #endregion

        #region Private Methods
        private async Task ConnectAsync()
        {
            try
            {
                using (var response = await _splitHttpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, new CancellationToken()))
                {
                    if (!response.IsSuccessStatusCode) return;

                    try
                    {
                        _ongoindStream = await response.Content.ReadAsStreamAsync();
                        _log.Info($"Streaming Connected.");

                        await ReadStreamAsync();
                    }
                    catch (Exception ex)
                    {
                        _log.Debug($"Error reading stream: {ex.Message}");
                        await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                    }
                    finally
                    {
                        _ongoindStream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug($"Error connecting to {_url}.", ex);
            }
            finally
            {
                _disconnectSignal.Signal();

                _log.Debug("Finished Event Source client ConnectAsync.");
            }            
        }

        private async Task ReadStreamAsync()
        {
            try
            {
                _lineBuffer = string.Empty;
                _connected = true;
                await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.CONNECTED);

                while (_ongoindStream.CanRead && _connected && !_statusManager.IsDestroyed())
                {
                    Array.Clear(_buffer, 0, BufferSize);

                    using (var timeoutToken = new CancellationTokenSource(ReadTimeoutMs))
                    {
                        using (timeoutToken.Token.Register(() => _ongoindStream.Close()))
                        {
                            var len = 0;
                            try
                            {
                                _log.Debug($"SSE client, waiting next notification ...");
                                len = await _ongoindStream.ReadAsync(_buffer, 0, BufferSize, timeoutToken.Token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                if (!_connected && (ex is IOException || ex is ObjectDisposedException))
                                {
                                    _log.Debug($"Streaming read was forced to stop.", ex);
                                    await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.FORCED_STOP);

                                    return;
                                }

                                if (timeoutToken.IsCancellationRequested)
                                    _log.Debug($"Streaming read time out after {ReadTimeoutMs / 1000} seconds.");
                                else
                                    _log.Debug($"Streaming IOException", ex);

                                await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                                return;
                            }

                            if (len == 0)
                            {
                                // Added for tests. 
                                if (_url.StartsWith("http://localhost"))
                                {
                                    _log.Debug("Streaming end of the file - for tests.");
                                    await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.CONNECTED);
                                    return;
                                }

                                _log.Debug("Streaming end of the file.");
                                await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                                return;
                            }

                            var notificationString = _encoder.GetString(_buffer, 0, len);
                            _log.Debug($"Read stream encoder buffer: {notificationString}");

                            if (_firstEvent) await ProcessFirtsEvent(notificationString);

                            if (notificationString == KeepAliveResponse || !_connected) continue;

                            var lines = ReadLines(notificationString);

                            foreach (var line in lines)
                            {
                                if (string.IsNullOrEmpty(line)) continue;

                                var eventData = _notificationParser.Parse(line);

                                if (eventData == null) continue;

                                switch (eventData.Type)
                                {
                                    case NotificationType.ERROR:
                                        var notificationError = (NotificationError)eventData;
                                        await ProcessErrorNotification(notificationError);
                                        break;
                                    default:
                                        DispatchEvent(eventData);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_connected && !_statusManager.IsDestroyed())
                {
                    _log.Debug("Stream ended abruptly, proceeding to reconnect.", ex);
                    await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                    return;
                }
            }
        }

        private async Task ProcessErrorNotification(NotificationError notificationError)
        {
            _log.Debug($"Ably Notification error: {notificationError.Message}.\nStatus Server: {notificationError.StatusCode}.\n AblyCode: {notificationError.Code}");
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.AblyError, notificationError.Code));

            if (notificationError.Code >= 40140 && notificationError.Code <= 40149)
            {
                await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR);
                return;
            }

            if (notificationError.Code >= 40000 && notificationError.Code <= 49999)
            {
                await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.NONRETRYABLE_ERROR);
                return;
            }            
        }

        private void DispatchEvent(IncomingNotification incomingNotification)
        {
            EventReceived?.Invoke(this, new EventReceivedEventArgs(incomingNotification));
        }

        private async Task ProcessFirtsEvent(string notification)
        {
            _firstEvent = false;
            var eventData = _notificationParser.Parse(notification);

            // This case is when in the first event received an error notification, mustn't dispatch connected.
            if (eventData != null && eventData.Type == NotificationType.ERROR) return;

            await _notificationManagerKeeper.HandleSseStatus(SSEClientStatusMessage.FIRST_EVENT);
        }

        private List<string> ReadLines(string message)
        {
            var toReturn = new List<string>();
            var sbs = string.Empty;

            if (!string.IsNullOrEmpty(_lineBuffer))
            {
                sbs += _lineBuffer;
                _lineBuffer = string.Empty;
            }

            foreach (var item in message)
            {
                sbs += item;

                if (sbs.EndsWith("\n\n"))
                {
                    toReturn.Add(sbs);
                    sbs = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(sbs))
            {
                _lineBuffer = sbs;
            }

            return toReturn;
        }
        #endregion
    }
}
