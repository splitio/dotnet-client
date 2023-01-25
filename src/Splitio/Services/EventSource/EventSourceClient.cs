using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public class EventSourceClient : IEventSourceClient
    {
        private const string KeepAliveResponse = ":keepalive\n\n";
        private const int ReadTimeoutMs = 70000;
        private const int ConnectTimeoutMs = 30000;
        private const int BufferSize = 10000;

        private readonly string[] _notificationSplitArray = new[] { "\n\n" };
        private readonly byte[] _buffer = new byte[BufferSize];
        private readonly UTF8Encoding _encoder = new UTF8Encoding();
        private readonly CountdownEvent _disconnectSignal = new CountdownEvent(1);
        private readonly CountdownEvent _initializationSignal = new CountdownEvent(1);

        private readonly ISplitLogger _log;
        private readonly INotificationParser _notificationParser;   
        private readonly ISplitioHttpClient _splitHttpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _tasksManager;

        private string _url;
        private bool _connected;
        private bool _firstEvent;
        
        private CancellationTokenSource _cancellationTokenSource;

        public EventSourceClient(INotificationParser notificationParser,
            ISplitioHttpClient splitHttpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager tasksManager)
        {            
            _notificationParser = notificationParser;
            _splitHttpClient = splitHttpClient;
            _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _tasksManager = tasksManager;

            _firstEvent = true;
        }
        
        public event EventHandler<EventReceivedEventArgs> EventReceived;
        public event EventHandler<SSEActionsEventArgs> ActionEvent;

        #region Public Methods
        public bool ConnectAsync(string url)
        {
            if (IsConnected())
            {
                _log.Debug("Event source Client already connected.");

                return false;
            }

            _firstEvent = true;
            _url = url;
            _disconnectSignal.Reset();
            _initializationSignal.Reset();
            _cancellationTokenSource = new CancellationTokenSource();

            _tasksManager.Start(() => ConnectAsync(_cancellationTokenSource.Token).Wait(), _cancellationTokenSource, "SSE - ConnectAsync");

            _initializationSignal.Wait(ConnectTimeoutMs);

            return IsConnected();
        }

        public bool IsConnected()
        {
            return _connected;
        }

        public void Disconnect(SSEClientActions action = SSEClientActions.DISCONNECT)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            
            _connected = false;

            _disconnectSignal.Wait(ReadTimeoutMs);

            DispatchActionEvent(action);
            _log.Debug($"Disconnected from {_url}");
        }
        #endregion

        #region Private Methods
        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var action = SSEClientActions.DISCONNECT;

            try
            {
                using (var response = await _splitHttpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    _log.Debug($"Response from {_url}: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode) return;

                    try
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            _log.Info($"Connected to {_url}");

                            await ReadStreamAsync(stream, cancellationToken);
                        }
                    }
                    catch (ReadStreamException ex)
                    {
                        _log.Debug(ex.Message);
                        action = ex.Action;
                    }
                    catch (Exception ex)
                    {
                        _log.Debug($"Error reading stream: {ex.Message}");
                        action = SSEClientActions.RETRYABLE_ERROR;
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
                _initializationSignal.Signal();
                Disconnect(action);                

                _log.Debug("Finished Event Source client ConnectAsync.");
            }            
        }

        private async Task ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && (IsConnected() || _firstEvent))
                {
                    if (stream.CanRead && (IsConnected() || _firstEvent))
                    {
                        Array.Clear(_buffer, 0, BufferSize);

                        using (var timeoutToken = new CancellationTokenSource(ReadTimeoutMs))
                        {
                            using (timeoutToken.Token.Register(() => stream.Close()))
                            {
                                var len = 0;
                                try
                                {
                                    _log.Debug($"Reading stream ....");
                                    len = await stream.ReadAsync(_buffer, 0, BufferSize, timeoutToken.Token).ConfigureAwait(false);
                                }
                                catch(Exception ex)
                                {
                                    _log.Debug($"Read Stream exception: {ex.GetType()}.|| {ex}");
                                    throw new ReadStreamException(SSEClientActions.RETRYABLE_ERROR, $"Streaming read time out after {ReadTimeoutMs/1000} seconds.");
                                }

                                if (len == 0)
                                {
                                    var exception = new ReadStreamException(SSEClientActions.RETRYABLE_ERROR, "Streaming end of the file.");

                                    // Added for tests. 
                                    if (_url.StartsWith("http://localhost"))
                                        exception = new ReadStreamException(SSEClientActions.DISCONNECT, "Streaming end of the file - for tests.");

                                    throw exception;
                                }

                                var notificationString = _encoder.GetString(_buffer, 0, len);
                                _log.Debug($"Read stream encoder buffer: {notificationString}");

                                if (_firstEvent)
                                {
                                    ProcessFirtsEvent(notificationString);
                                }

                                if (notificationString != KeepAliveResponse && IsConnected())
                                {
                                    var lines = notificationString.Split(_notificationSplitArray, StringSplitOptions.None);

                                    foreach (var line in lines)
                                    {
                                        if (!string.IsNullOrEmpty(line))
                                        {
                                            var eventData = _notificationParser.Parse(line);

                                            if (eventData != null)
                                            {
                                                if (eventData.Type == NotificationType.ERROR)
                                                {
                                                    var notificationError = (NotificationError)eventData;

                                                    ProcessErrorNotification(notificationError);
                                                }
                                                else
                                                {
                                                    DispatchEvent(eventData);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ReadStreamException ex)
            {
                _log.Debug("ReadStreamException", ex);

                throw ex;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _log.Debug("Stream ended abruptly, proceeding to reconnect.", ex);
                    throw new ReadStreamException(SSEClientActions.RETRYABLE_ERROR, ex.Message);
                }

                _log.Debug("Stream Token cancelled.", ex);
            }
            finally
            {
                _log.Debug($"Stop read stream");
            }
        }

        private void ProcessErrorNotification(NotificationError notificationError)
        {
            _log.Debug($"Notification error: {notificationError.Message}. Status Server: {notificationError.StatusCode}.");

            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.AblyError, notificationError.Code));

            if (notificationError.Code >= 40140 && notificationError.Code <= 40149)
            {
                throw new ReadStreamException(SSEClientActions.RETRYABLE_ERROR, $"Ably Notification code: {notificationError.Code}");
            }

            if (notificationError.Code >= 40000 && notificationError.Code <= 49999)
            {
                throw new ReadStreamException(SSEClientActions.NONRETRYABLE_ERROR, $"Ably Notification code: {notificationError.Code}");
            }            
        }

        private void DispatchEvent(IncomingNotification incomingNotification)
        {
            _log.Debug($"DispatchEvent: {incomingNotification}");
            EventReceived?.Invoke(this, new EventReceivedEventArgs(incomingNotification));
        }

        private void DispatchActionEvent(SSEClientActions action)
        {
            ActionEvent?.Invoke(this, new SSEActionsEventArgs(action));
        }

        private void ProcessFirtsEvent(string notification)
        {
            _firstEvent = false;
            var eventData = _notificationParser.Parse(notification);

            // This case is when in the first event received an error notification, mustn't dispatch connected.
            if (eventData != null && eventData.Type == NotificationType.ERROR) return;

            _connected = true;
            _initializationSignal.Signal();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SSEConnectionEstablished));
            DispatchActionEvent(SSEClientActions.CONNECTED);            
        }
        #endregion
    }
}
