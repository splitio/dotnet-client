using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                var notification  = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1585867723838}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus  = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SPLIT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585867723838, ((SplitChangeNotification)ev.Event).ChangeNumber);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
            }
        }
        
        [TestMethod]
        public void EventSourceClient_SplitKillEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_KILL\\\",\\\"changeNumber\\\":1585868246622,\\\"defaultTreatment\\\":\\\"off\\\",\\\"splitName\\\":\\\"test-split\\\"}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SPLIT_KILL, ev.Event.Type);
                Assert.AreEqual(1585868246622, ((SplitKillNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("off", ((SplitKillNotification)ev.Event).DefaultTreatment);
                Assert.AreEqual("test-split", ((SplitKillNotification)ev.Event).SplitName);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
            }
        }

        [TestMethod]
        public void EventSourceClient_SegmentUpdateEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_segments\",\"data\":\"{\\\"type\\\":\\\"SEGMENT_UPDATE\\\",\\\"changeNumber\\\":1585868933303,\\\"segmentName\\\":\\\"test-segment\\\"}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SEGMENT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585868933303, ((SegmentChangeNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("test-segment", ((SegmentChangeNotification)ev.Event).SegmentName);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
            }
        }

        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingPaused_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_PAUSED\\\"}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_PAUSED, ((ControlNotification)ev.Event).ControlType);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
            }
        }
        
        
        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingResumed_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_RESUMED\\\"}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_RESUMED, ((ControlNotification)ev.Event).ControlType);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
            }
        }
        
        [TestMethod]
        public void EventSourceClient_ControlEvent_StreamingDisabled_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"[?occupancy=metrics.publishers]control_pri\",\"data\":\"{\\\"type\\\":\\\"CONTROL\\\",\\\"controlType\\\":\\\"STREAMING_DISABLED\\\"}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_DISABLED, ((ControlNotification)ev.Event).ControlType);
                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
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

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
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

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.DISCONNECT, action);
            }
        }

        [TestMethod]
        public void EventSourceClient_KeepAliveResponse()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.SSE_Channels_Response(":keepalive\n\n");

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var sseClientStatus = result.Item3;

                eventSourceClient.ConnectAsync(httpClientMock.GetUrl());

                sseClientStatus.TryTake(out SSEClientActions action, 10000);
                Assert.AreEqual(SSEClientActions.CONNECTED, action);
                Thread.Sleep(1000);
                Assert.AreEqual(0, eventsReceived.Count);
            }
        }

        private (IEventSourceClient, BlockingCollection<EventReceivedEventArgs>, BlockingCollection<SSEClientActions>) GetEventSourceClient()
        {
            var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
            var sseClientStatus = new BlockingCollection<SSEClientActions>(new ConcurrentQueue<SSEClientActions>());

            var notificationParser = new NotificationParser();
            var wrapperAdapter = WrapperAdapter.Instance();
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 5000,
                HttpReadTimeout = 5000
            };
            var sseHttpClient = new SplitioHttpClient("api-key", config, new Dictionary<string, string>());
            var telemetryRuntimeProducer = new InMemoryTelemetryStorage();

            var eventSourceClient = new EventSourceClient(notificationParser, sseHttpClient, telemetryRuntimeProducer, new TasksManager(wrapperAdapter), sseClientStatus);
            eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
            {
                eventsReceived.TryAdd(e);
            };

            return (eventSourceClient, eventsReceived, sseClientStatus);
        }
    }
}
