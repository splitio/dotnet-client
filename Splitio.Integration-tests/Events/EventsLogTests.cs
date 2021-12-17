using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Events.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Integration_tests.Events
{
    [TestClass]
    public class EventsLogTests
    {
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly ITasksManager _tasksManger;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly int _bulkSize = 5;

        public EventsLogTests()
        {
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _wrapperAdapter = new WrapperAdapter();
            _tasksManger = new TasksManager(_wrapperAdapter);
        }

        [TestMethod]
        public void SendBulkEventsTask_With3Bulks()
        {
            var events = new List<Event>
            {
                new Event { key = "key-01" },
                new Event { key = "key-02" },
                new Event { key = "key-03" },
                new Event { key = "key-04" },
                new Event { key = "key-05" },
                new Event { key = "key-06" },
                new Event { key = "key-07" },
                new Event { key = "key-08" },
                new Event { key = "key-09" },
                new Event { key = "key-10" },
                new Event { key = "key-11" },
                new Event { key = "key-12" },
                new Event { key = "key-13" },
            };

            var data1 = "[{\"key\":\"key-01\",\"timestamp\":0},{\"key\":\"key-02\",\"timestamp\":0},{\"key\":\"key-03\",\"timestamp\":0},{\"key\":\"key-04\",\"timestamp\":0},{\"key\":\"key-05\",\"timestamp\":0}]";
            var data2 = "[{\"key\":\"key-06\",\"timestamp\":0},{\"key\":\"key-07\",\"timestamp\":0},{\"key\":\"key-08\",\"timestamp\":0},{\"key\":\"key-09\",\"timestamp\":0},{\"key\":\"key-10\",\"timestamp\":0}]";
            var data3 = "[{\"key\":\"key-11\",\"timestamp\":0},{\"key\":\"key-12\",\"timestamp\":0},{\"key\":\"key-13\",\"timestamp\":0}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/events/bulk", 200, data1, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, data2, "ok");
                httpClientMock.Post_Response("/api/events/bulk", 200, data3, "ok");

                var eventSdkApiClient = new EventSdkApiClient("api-key-test", new Dictionary<string, string>(), httpClientMock.GetUrl(), 10000, 10000, _telemetryRuntimeProducer.Object, _tasksManger, _wrapperAdapter, _bulkSize);
                eventSdkApiClient.SendBulkEventsTask(events);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetEventsLog();
                Assert.AreEqual(3, logs.Count);

                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data1)));
                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data2)));
                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data3)));
            }
        }

        [TestMethod]
        public void SendBulkEventsTask_WithOneBulk()
        {
            var events = new List<Event>
            {
                new Event { key = "key-01" },
                new Event { key = "key-02" },
                new Event { key = "key-03" },
                new Event { key = "key-04" },
                new Event { key = "key-05" },
                new Event { key = "key-06" }
            };

            var data = "[{\"key\":\"key-01\",\"timestamp\":0},{\"key\":\"key-02\",\"timestamp\":0},{\"key\":\"key-03\",\"timestamp\":0},{\"key\":\"key-04\",\"timestamp\":0},{\"key\":\"key-05\",\"timestamp\":0},{\"key\":\"key-06\",\"timestamp\":0}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/events/bulk", 200, data, "ok");

                var eventSdkApiClient = new EventSdkApiClient("api-key-test", new Dictionary<string, string>(), httpClientMock.GetUrl(), 10000, 10000, _telemetryRuntimeProducer.Object, _tasksManger, _wrapperAdapter, 6);
                eventSdkApiClient.SendBulkEventsTask(events);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetEventsLog();
                Assert.AreEqual(1, logs.Count);

                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data)));
            }
        }

        [TestMethod]
        public void SendBulkEventsTask_WithRetries()
        {
            var events = new List<Event>
            {
                new Event { key = "key-01" },
                new Event { key = "key-02" },
                new Event { key = "key-03" },
                new Event { key = "key-04" },
                new Event { key = "key-05" },
                new Event { key = "key-06" }
            };

            var data = "[{\"key\":\"key-01\",\"timestamp\":0},{\"key\":\"key-02\",\"timestamp\":0},{\"key\":\"key-03\",\"timestamp\":0},{\"key\":\"key-04\",\"timestamp\":0},{\"key\":\"key-05\",\"timestamp\":0},{\"key\":\"key-06\",\"timestamp\":0}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/events/bulk", 500, data, "fail");

                var eventSdkApiClient = new EventSdkApiClient("api-key-test", new Dictionary<string, string>(), httpClientMock.GetUrl(), 10000, 10000, _telemetryRuntimeProducer.Object, _tasksManger, _wrapperAdapter, 6);
                eventSdkApiClient.SendBulkEventsTask(events);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetEventsLog();
                Assert.AreEqual(3, logs.Count);

                foreach (var log in logs)
                {
                    Assert.IsTrue(log.RequestMessage.Body.Equals(data));
                }
            }
        }
    }
}
