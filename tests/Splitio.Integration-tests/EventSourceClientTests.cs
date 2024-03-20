using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using Splitio.Tests.Common;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Splitio.Integration_tests
{
    [TestClass, TestCategory("Integration")]
    public class EventSourceClientTests
    {
        #region Without Space
        [TestMethod]
        public void EventSourceClient_NotificationError_STREAMING_OFF_WithoutSpace()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "event:error\ndata:{\"message\":\"Token expired\",\"code\":49000,\"statusCode\":500,\"href\":\"https://help.ably.io/error/40142\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var streamingStatusQueue = result.Item3;

                var connected = eventSourceClient.Connect(httpClientMock.GetUrl());
                Assert.IsTrue(connected);

                streamingStatusQueue.TryTake(out StreamingStatus action, 10000);
                Assert.AreEqual(StreamingStatus.STREAMING_OFF, action);
            }
        }

        [TestMethod]
        public void EventSourceClient_FullBuffer_ShouldProcessAllNotifications_WithoutSpace()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id:123123\nevent:message\ndata:{\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":<<CHANGE-NUMBER>>,\\\"pcn\\\":-1,\\\"c\\\":0,\\\"d\\\":\\\"eyJ0cmFmZmljVHlwZU5hbWUiOiJ1c2VyIiwiaWQiOiJkNDMxY2RkMC1iMGJlLTExZWEtOGE4MC0xNjYwYWRhOWNlMzkiLCJuYW1lIjoibWF1cm9famF2YSIsInRyYWZmaWNBbGxvY2F0aW9uIjoxMDAsInRyYWZmaWNBbGxvY2F0aW9uU2VlZCI6LTkyMzkxNDkxLCJzZWVkIjotMTc2OTM3NzYwNCwic3RhdHVzIjoiQUNUSVZFIiwia2lsbGVkIjpmYWxzZSwiZGVmYXVsdFRyZWF0bWVudCI6Im9mZiIsImNoYW5nZU51bWJlciI6MTY4NDMyOTg1NDM4NSwiYWxnbyI6MiwiY29uZmlndXJhdGlvbnMiOnt9LCJjb25kaXRpb25zIjpbeyJjb25kaXRpb25UeXBlIjoiV0hJVEVMSVNUIiwibWF0Y2hlckdyb3VwIjp7ImNvbWJpbmVyIjoiQU5EIiwibWF0Y2hlcnMiOlt7Im1hdGNoZXJUeXBlIjoiV0hJVEVMSVNUIiwibmVnYXRlIjpmYWxzZSwid2hpdGVsaXN0TWF0Y2hlckRhdGEiOnsid2hpdGVsaXN0IjpbImFkbWluIiwibWF1cm8iLCJuaWNvIl19fV19LCJwYXJ0aXRpb25zIjpbeyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9XSwibGFiZWwiOiJ3aGl0ZWxpc3RlZCJ9LHsiY29uZGl0aW9uVHlwZSI6IlJPTExPVVQiLCJtYXRjaGVyR3JvdXAiOnsiY29tYmluZXIiOiJBTkQiLCJtYXRjaGVycyI6W3sia2V5U2VsZWN0b3IiOnsidHJhZmZpY1R5cGUiOiJ1c2VyIn0sIm1hdGNoZXJUeXBlIjoiSU5fU0VHTUVOVCIsIm5lZ2F0ZSI6ZmFsc2UsInVzZXJEZWZpbmVkU2VnbWVudE1hdGNoZXJEYXRhIjp7InNlZ21lbnROYW1lIjoibWF1ci0yIn19XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImluIHNlZ21lbnQgbWF1ci0yIn0seyJjb25kaXRpb25UeXBlIjoiUk9MTE9VVCIsIm1hdGNoZXJHcm91cCI6eyJjb21iaW5lciI6IkFORCIsIm1hdGNoZXJzIjpbeyJrZXlTZWxlY3RvciI6eyJ0cmFmZmljVHlwZSI6InVzZXIifSwibWF0Y2hlclR5cGUiOiJBTExfS0VZUyIsIm5lZ2F0ZSI6ZmFsc2V9XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImRlZmF1bHQgcnVsZSJ9XX0=\\\"}\"}\n\n";
                var notifications = string.Empty;
                var countExpected = 20;
                var nList = new List<SplitChangeNotification>();

                for (int i = 0; i < countExpected; i++)
                {
                    var not = notification.Replace("<<CHANGE-NUMBER>>", i.ToString());
                    notifications += not;
                }

                httpClientMock.SSE_Channels_Response(notifications);

                var result = GetEventSourceClient();
                var streamingStatusQueue = result.Item3;
                var eventSourceClient = result.Item1;
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    nList.Add((SplitChangeNotification)e.Event);
                };

                var connected = eventSourceClient.Connect(httpClientMock.GetUrl());
                Assert.IsTrue(connected);

                streamingStatusQueue.TryTake(out StreamingStatus action, 10000);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);

                Thread.Sleep(2000);

                Assert.AreEqual(countExpected, nList.Count);
                foreach (var item in nList)
                {
                    Assert.IsTrue(item.ChangeNumber <= 19);
                }
            }
        }
        #endregion

        [TestMethod]
        public void EventSourceClient_FullBuffer_ShouldProcessAllNotifications()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 123123\nevent: message\ndata: {\"id\":\"1111\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":<<CHANGE-NUMBER>>,\\\"pcn\\\":-1,\\\"c\\\":0,\\\"d\\\":\\\"eyJ0cmFmZmljVHlwZU5hbWUiOiJ1c2VyIiwiaWQiOiJkNDMxY2RkMC1iMGJlLTExZWEtOGE4MC0xNjYwYWRhOWNlMzkiLCJuYW1lIjoibWF1cm9famF2YSIsInRyYWZmaWNBbGxvY2F0aW9uIjoxMDAsInRyYWZmaWNBbGxvY2F0aW9uU2VlZCI6LTkyMzkxNDkxLCJzZWVkIjotMTc2OTM3NzYwNCwic3RhdHVzIjoiQUNUSVZFIiwia2lsbGVkIjpmYWxzZSwiZGVmYXVsdFRyZWF0bWVudCI6Im9mZiIsImNoYW5nZU51bWJlciI6MTY4NDMyOTg1NDM4NSwiYWxnbyI6MiwiY29uZmlndXJhdGlvbnMiOnt9LCJjb25kaXRpb25zIjpbeyJjb25kaXRpb25UeXBlIjoiV0hJVEVMSVNUIiwibWF0Y2hlckdyb3VwIjp7ImNvbWJpbmVyIjoiQU5EIiwibWF0Y2hlcnMiOlt7Im1hdGNoZXJUeXBlIjoiV0hJVEVMSVNUIiwibmVnYXRlIjpmYWxzZSwid2hpdGVsaXN0TWF0Y2hlckRhdGEiOnsid2hpdGVsaXN0IjpbImFkbWluIiwibWF1cm8iLCJuaWNvIl19fV19LCJwYXJ0aXRpb25zIjpbeyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9XSwibGFiZWwiOiJ3aGl0ZWxpc3RlZCJ9LHsiY29uZGl0aW9uVHlwZSI6IlJPTExPVVQiLCJtYXRjaGVyR3JvdXAiOnsiY29tYmluZXIiOiJBTkQiLCJtYXRjaGVycyI6W3sia2V5U2VsZWN0b3IiOnsidHJhZmZpY1R5cGUiOiJ1c2VyIn0sIm1hdGNoZXJUeXBlIjoiSU5fU0VHTUVOVCIsIm5lZ2F0ZSI6ZmFsc2UsInVzZXJEZWZpbmVkU2VnbWVudE1hdGNoZXJEYXRhIjp7InNlZ21lbnROYW1lIjoibWF1ci0yIn19XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImluIHNlZ21lbnQgbWF1ci0yIn0seyJjb25kaXRpb25UeXBlIjoiUk9MTE9VVCIsIm1hdGNoZXJHcm91cCI6eyJjb21iaW5lciI6IkFORCIsIm1hdGNoZXJzIjpbeyJrZXlTZWxlY3RvciI6eyJ0cmFmZmljVHlwZSI6InVzZXIifSwibWF0Y2hlclR5cGUiOiJBTExfS0VZUyIsIm5lZ2F0ZSI6ZmFsc2V9XX0sInBhcnRpdGlvbnMiOlt7InRyZWF0bWVudCI6Im9uIiwic2l6ZSI6MH0seyJ0cmVhdG1lbnQiOiJvZmYiLCJzaXplIjoxMDB9LHsidHJlYXRtZW50IjoiVjQiLCJzaXplIjowfSx7InRyZWF0bWVudCI6InY1Iiwic2l6ZSI6MH1dLCJsYWJlbCI6ImRlZmF1bHQgcnVsZSJ9XX0=\\\"}\"}\n\n";
                var notifications = string.Empty;
                var countExpected = 20;
                var nList = new List<SplitChangeNotification>();

                for (int i = 0; i < countExpected; i++)
                {
                    var not = notification.Replace("<<CHANGE-NUMBER>>", i.ToString());
                    notifications += not;
                }

                httpClientMock.SSE_Channels_Response(notifications);

                var result = GetEventSourceClient();
                var streamingStatusQueue = result.Item3;
                var eventSourceClient = result.Item1;
                eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
                {
                    nList.Add((SplitChangeNotification)e.Event);
                };

                eventSourceClient.Connect(httpClientMock.GetUrl());

                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);

                Thread.Sleep(2000);

                Assert.AreEqual(countExpected, nList.Count);
                foreach (var item in nList)
                {
                    Assert.IsTrue(item.ChangeNumber <= 19);
                }
            }
        }

        [TestMethod]
        public void EventSourceClient_SplitUpdateEvent_ShouldReceiveEvent()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "id: 234234432\nevent: message\ndata: {\"id\":\"jSOE7oGJWo:0:0\",\"clientId\":\"pri:ODc1NjQyNzY1\",\"timestamp\":1588254699236,\"encoding\":\"json\",\"channel\":\"xxxx_xxxx_splits\",\"data\":\"{\\\"type\\\":\\\"SPLIT_UPDATE\\\",\\\"changeNumber\\\":1585867723838}\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SPLIT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585867723838, ((SplitChangeNotification)ev.Event).ChangeNumber);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SPLIT_KILL, ev.Event.Type);
                Assert.AreEqual(1585868246622, ((SplitKillNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("off", ((SplitKillNotification)ev.Event).DefaultTreatment);
                Assert.AreEqual("test-split", ((SplitKillNotification)ev.Event).SplitName);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.SEGMENT_UPDATE, ev.Event.Type);
                Assert.AreEqual(1585868933303, ((SegmentChangeNotification)ev.Event).ChangeNumber);
                Assert.AreEqual("test-segment", ((SegmentChangeNotification)ev.Event).SegmentName);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_PAUSED, ((ControlNotification)ev.Event).ControlType);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_RESUMED, ((ControlNotification)ev.Event).ControlType);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                eventsReceived.TryTake(out EventReceivedEventArgs ev, 10000);
                Assert.AreEqual(NotificationType.CONTROL, ev.Event.Type);
                Assert.AreEqual(ControlType.STREAMING_DISABLED, ((ControlNotification)ev.Event).ControlType);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
                Assert.AreEqual(0, eventsReceived.Count);
            }
        }

        [TestMethod]
        public void EventSourceClient_NotificationError_STREAMING_BACKOFF()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "event: error\ndata: {\"message\":\"Token expired\",\"code\":40142,\"statusCode\":401,\"href\":\"https://help.ably.io/error/40142\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                Thread.Sleep(1000);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_BACKOFF, action);
            }
        }

        [TestMethod]
        public void EventSourceClient_NotificationError_STREAMING_OFF()
        {
            using (var httpClientMock = new HttpClientMock())
            {
                var notification = "event: error\ndata: {\"message\":\"Token expired\",\"code\":49000,\"statusCode\":500,\"href\":\"https://help.ably.io/error/40142\"}\n\n";
                httpClientMock.SSE_Channels_Response(notification);

                var result = GetEventSourceClient();
                var eventSourceClient = result.Item1;
                var eventsReceived = result.Item2;
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());
                Thread.Sleep(1000);
                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_OFF, action);
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
                var streamingStatusQueue = result.Item3;

                eventSourceClient.Connect(httpClientMock.GetUrl());

                streamingStatusQueue.TryDequeue(out StreamingStatus action);
                Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
                Thread.Sleep(1000);
                Assert.AreEqual(0, eventsReceived.Count);
            }
        }

        private static (IEventSourceClient, BlockingCollection<EventReceivedEventArgs>, SplitQueue<StreamingStatus>) GetEventSourceClient()
        {
            var eventsReceived = new BlockingCollection<EventReceivedEventArgs>(new ConcurrentQueue<EventReceivedEventArgs>());
            var streamingStatusQueue = new SplitQueue<StreamingStatus>();

            var notificationParser = new NotificationParser();
            var wrapperAdapter = WrapperAdapter.Instance();
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 5000,
                HttpReadTimeout = 5000
            };
            var sseHttpClient = new SplitioHttpClient("api-key", config, new Dictionary<string, string>());
            var telemetryRuntimeProducer = new InMemoryTelemetryStorage();
            var notificationManagerKeeper = new NotificationManagerKeeper(telemetryRuntimeProducer, streamingStatusQueue);
            var statusManager = new InMemoryReadinessGatesCache();
            var tasksManager = new TasksManager(statusManager);
            var task = tasksManager.NewOnTimeTask(Enums.Task.SSEConnect);

            var eventSourceClient = new EventSourceClient(notificationParser, sseHttpClient, telemetryRuntimeProducer, notificationManagerKeeper, statusManager, task);
            eventSourceClient.EventReceived += delegate (object sender, EventReceivedEventArgs e)
            {
                eventsReceived.TryAdd(e);
            };

            return (eventSourceClient, eventsReceived, streamingStatusQueue);
        }
    }
}
