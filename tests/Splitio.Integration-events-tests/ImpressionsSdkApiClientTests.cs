using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using Splitio.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Integration_events_tests
{
    [TestClass, TestCategory("Integration")]
    public class ImpressionsSdkApiClientTests
    {
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISplitioHttpClient _splitioHttpClient;

        public ImpressionsSdkApiClientTests()
        {
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _wrapperAdapter = WrapperAdapter.Instance();
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 10000,
                HttpReadTimeout = 10000
            };
            _splitioHttpClient = new SplitioHttpClient("api-key-test", config, new Dictionary<string, string>());
        }

        [TestMethod]
        public async Task SendBulkImpressions_WithThreeBulks()
        {
            var impressions = new List<KeyImpression>();

            for (int i = 0; i < 13; i++)
            {
                impressions.Add(new KeyImpression($"key-{i}", $"feature-{i}", "off", 1, 1, "label-test", $"bucket-{i}", false));
            }

            var data1 = "[{\"f\":\"feature-0\",\"i\":[{\"k\":\"key-0\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-0\"}]},{\"f\":\"feature-1\",\"i\":[{\"k\":\"key-1\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-1\"}]},{\"f\":\"feature-2\",\"i\":[{\"k\":\"key-2\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-2\"}]},{\"f\":\"feature-3\",\"i\":[{\"k\":\"key-3\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-3\"}]},{\"f\":\"feature-4\",\"i\":[{\"k\":\"key-4\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-4\"}]}]";
            var data2 = "[{\"f\":\"feature-5\",\"i\":[{\"k\":\"key-5\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-5\"}]},{\"f\":\"feature-6\",\"i\":[{\"k\":\"key-6\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-6\"}]},{\"f\":\"feature-7\",\"i\":[{\"k\":\"key-7\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-7\"}]},{\"f\":\"feature-8\",\"i\":[{\"k\":\"key-8\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-8\"}]},{\"f\":\"feature-9\",\"i\":[{\"k\":\"key-9\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-9\"}]}]";
            var data3 = "[{\"f\":\"feature-10\",\"i\":[{\"k\":\"key-10\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-10\"}]},{\"f\":\"feature-11\",\"i\":[{\"k\":\"key-11\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-11\"}]},{\"f\":\"feature-12\",\"i\":[{\"k\":\"key-12\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-12\"}]}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, data1, "ok");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, data2, "ok");
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, data3, "ok");

                var impressionsSdkApiClient = new ImpressionsSdkApiClient(_splitioHttpClient, _telemetryRuntimeProducer.Object, httpClientMock.GetUrl(), _wrapperAdapter, 5);
                await impressionsSdkApiClient.SendBulkImpressionsAsync(impressions);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetImpressionLogs();
                Assert.AreEqual(3, logs.Count);

                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data1)));
                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data2)));
                Assert.IsTrue(logs.Any(l => l.RequestMessage.Body.Equals(data3)));
            }
        }

        [TestMethod]
        public async Task SendBulkImpressions_WithOneBulk()
        {
            var impressions = new List<KeyImpression>();

            for (int i = 0; i < 10; i++)
            {
                impressions.Add(new KeyImpression($"key-{i}", $"feature-{i}", "off", 1, 1, "label-test", $"bucket-{i}", false));
            }

            var data1 = "[{\"f\":\"feature-0\",\"i\":[{\"k\":\"key-0\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-0\"}]},{\"f\":\"feature-1\",\"i\":[{\"k\":\"key-1\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-1\"}]},{\"f\":\"feature-2\",\"i\":[{\"k\":\"key-2\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-2\"}]},{\"f\":\"feature-3\",\"i\":[{\"k\":\"key-3\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-3\"}]},{\"f\":\"feature-4\",\"i\":[{\"k\":\"key-4\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-4\"}]},{\"f\":\"feature-5\",\"i\":[{\"k\":\"key-5\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-5\"}]},{\"f\":\"feature-6\",\"i\":[{\"k\":\"key-6\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-6\"}]},{\"f\":\"feature-7\",\"i\":[{\"k\":\"key-7\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-7\"}]},{\"f\":\"feature-8\",\"i\":[{\"k\":\"key-8\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-8\"}]},{\"f\":\"feature-9\",\"i\":[{\"k\":\"key-9\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-9\"}]}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/testImpressions/bulk", 200, data1, "ok");

                var impressionsSdkApiClient = new ImpressionsSdkApiClient(_splitioHttpClient, _telemetryRuntimeProducer.Object, httpClientMock.GetUrl(), _wrapperAdapter, 10);
                await impressionsSdkApiClient.SendBulkImpressionsAsync(impressions);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetImpressionLogs();
                Assert.AreEqual(1, logs.Count);
                Assert.AreEqual(logs.FirstOrDefault().RequestMessage.Body, data1);
            }
        }

        [TestMethod]
        public async Task SendBulkImpressions_WithRetries()
        {
            var impressions = new List<KeyImpression>();

            for (int i = 0; i < 10; i++)
            {
                impressions.Add(new KeyImpression($"key-{i}", $"feature-{i}", "off", 1, 1, "label-test", $"bucket-{i}", false));
            }

            var data1 = "[{\"f\":\"feature-0\",\"i\":[{\"k\":\"key-0\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-0\"}]},{\"f\":\"feature-1\",\"i\":[{\"k\":\"key-1\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-1\"}]},{\"f\":\"feature-2\",\"i\":[{\"k\":\"key-2\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-2\"}]},{\"f\":\"feature-3\",\"i\":[{\"k\":\"key-3\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-3\"}]},{\"f\":\"feature-4\",\"i\":[{\"k\":\"key-4\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-4\"}]},{\"f\":\"feature-5\",\"i\":[{\"k\":\"key-5\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-5\"}]},{\"f\":\"feature-6\",\"i\":[{\"k\":\"key-6\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-6\"}]},{\"f\":\"feature-7\",\"i\":[{\"k\":\"key-7\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-7\"}]},{\"f\":\"feature-8\",\"i\":[{\"k\":\"key-8\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-8\"}]},{\"f\":\"feature-9\",\"i\":[{\"k\":\"key-9\",\"t\":\"off\",\"m\":1,\"c\":1,\"r\":\"label-test\",\"b\":\"bucket-9\"}]}]";

            using (var httpClientMock = new HttpClientMock())
            {
                httpClientMock.Post_Response("/api/testImpressions/bulk", 500, data1, "fail");

                var impressionsSdkApiClient = new ImpressionsSdkApiClient(_splitioHttpClient, _telemetryRuntimeProducer.Object, httpClientMock.GetUrl(), _wrapperAdapter, 10);
                await impressionsSdkApiClient.SendBulkImpressionsAsync(impressions);

                Thread.Sleep(5000);

                var logs = httpClientMock.GetImpressionLogs();
                Assert.AreEqual(3, logs.Count);

                foreach (var log in logs)
                {
                    Assert.IsTrue(log.RequestMessage.Body.Equals(data1));
                }
            }
        }
    }
}
