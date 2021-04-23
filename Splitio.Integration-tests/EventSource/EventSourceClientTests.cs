using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio.Integration_tests.EventSource
{
    [TestClass]
    public class EventSourceClientTests
    {
        [TestMethod]
        public void EventSourceClient_SplitUpdateEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification  = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1585867723838}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate(object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate(object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };

                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.IsTrue(eventSourceClient.IsConnected());
                Assert.AreEqual(NotificationType.SPLIT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585867723838, ((SplitChangeNotifiaction)ev.Event).ChangeNumber);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }
        
        [TestMethod]
        public void EventSourceClient_SplitKillEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_KILL\\\",\\\"changeNumber\\\":1585868246622,\\\"defaultTreatment\\\":\\\"off\\\",\\\"splitName\\\":\\\"test-split\\\"}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SPLIT_KILL, ev.Event.Type);
                Assert.AreEqual(1585868246622, ((SplitKillNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("off", ((SplitKillNotification)ev.Event).DefaultTreatment);
                Assert.AreEqual("test-split", ((SplitKillNotification)ev.Event).SplitName);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }

        [TestMethod]
        public void EventSourceClient_SegmentUpdateEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_segments\",\"data\":\"{\\\"type\\\":\\\"SEGMENT_UPDATE\\\",\\\"changeNumber\\\":1585868933303,\\\"segmentName\\\":\\\"test-segment\\\"}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SEGMENT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585868933303, ((SegmentChangeNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("test-segment", ((SegmentChangeNotification)ev.Event).SegmentName);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }

        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingPaused_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_PAUSED\\\"}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_PAUSED, ((ControlNotification)ev.Event).ControlType);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }
        
        
        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingResumed_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_RESUMED\\\"}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_RESUMED, ((ControlNotification)ev.Event).ControlType);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }
        
        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingDisabled_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_DISABLED\\\"}\"}";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_DISABLED, ((ControlNotification)ev.Event).ControlType);
                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
            }
        }

        [TestMethod]
        public void EventSourceClient_IncorrectFormat_ShouldReceiveError()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SSE_Channels_Response(
                    @"{ 'event': 'message', 
                        'data': {
                            'id':'1',
                            'channel':'mauroc',
                            'content': {
                                'type': 'CONTROL', 
                                'controlType': 'test-control-type'
                            },
                            'name':'name-test'
                         }
                        }");

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
                Assert.AreEqual(0, eventsReceived.Count);
            }
        }

        [TestMethod]
        public void EventSourceClient_NotificationError_ShouldReceiveError()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "event: error\ndata: {\"message\":\"Token expired\",\"code\":40142,\"statusCode\":401,\"href\":\"https://help.ably.io/error/40142\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.DISCONNECT, action.Action);
            }
        }

        [TestMethod]
        public void EventSourceClient_KeepAliveResponse()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SSE_Channels_Response(":keepalive\n\n");

                var url = httpClientMock.GetUrl();
                var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
                var actionEvent = new BlockingCollection<SSEActionsEventArgs>(new ConcurrentQueue<SSEActionsEventArgs>());

                var notificationParser = new NotificationParser();
                var wrapperAdapter = new WrapperAdapter();
                var sseHttpClient = new SplitioHttpClient("api-key", 5000);
                var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

                var eventSourceClient = new EventSourceClient(notificationParser, wrapperAdapter, sseHttpClient, telemetryRuntimeProducer);
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    eventsReceived.TryAdd(e);
                };
                eventSourceClient.ActionEvent += delegate (object sender, SSEActionsEventArgs e)
                {
                    actionEvent.TryAdd(e);
                };
                eventSourceClient.ConnectAsync(url);

                actionEvent.TryTake(out SSEActionsEventArgs action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action.Action);
                Thread.Sleep(1000);
                Assert.AreEqual(0, eventsReceived.Count);
            }
        }
    }
}
