using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Integration_tests
{
    [TestClass]
    public class InMemoryTests : BaseIntegrationTests
    {
        [TestMethod]
        public void GetTreatment_WithoutBUR_ReturnsControl()
        {
            // Arrange.
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey1";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                // Act.
                var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

                // Assert.
                Assert.AreEqual("control", treatmentResult);

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey2";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                // Act.
                var treatmentResult = client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");

                // Assert.
                Assert.AreEqual("control", treatmentResult.Treatment);
                Assert.IsNull(treatmentResult.Config);

                client.BlockUntilReady(10000);
                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatments_WithoutBUR_ReturnsControl()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey3";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                // Act.
                var treatmentResults = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST" });

                // Assert.            
                Assert.AreEqual("control", treatmentResults["FACUNDO_TEST"]);
                Assert.AreEqual("control", treatmentResults["MAURO_TEST"]);

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey4";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                // Act.
                var treatmentResults = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST" });

                // Assert.            
                Assert.AreEqual("control", treatmentResults["FACUNDO_TEST"].Treatment);
                Assert.AreEqual("control", treatmentResults["MAURO_TEST"].Treatment);
                Assert.IsNull(treatmentResults["FACUNDO_TEST"].Config);
                Assert.IsNull(treatmentResults["MAURO_TEST"].Config);

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatment_WithtBUR_ReturnsTimeOutException()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey5";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                // Act.
                var exceptionMessage = "";
                var isSdkReady = false;

                try
                {
                    client.BlockUntilReady(1);
                    isSdkReady = true;
                }
                catch (Exception ex)
                {
                    isSdkReady = false;
                    exceptionMessage = ex.Message;
                }

                // Assert.
                Assert.IsFalse(isSdkReady);
                Assert.AreEqual("SDK was not ready in 1 miliseconds", exceptionMessage);

                client.Destroy();
            }
        }

        [TestMethod]
        public void Manager_SplitNames_WithoutBUR_ReturnsNull()
        {
            // Arrange.
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey6";

                var splitFactory = new SplitFactory(apikey, configurations);
                var manager = splitFactory.Manager();

                // Act.
                var result = manager.SplitNames();

                // Assert.
                Assert.IsNull(result);

                splitFactory.Client().Destroy();
            }
        }

        [TestMethod]
        public void CheckingHeaders_WithIPAddressesEnabled_ReturnsWithIpAndName()
        {
            // Arrange.
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey7";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                // Act.
                var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

                // Assert.
                Assert.AreEqual("on", treatmentResult);

                Thread.Sleep(5000);

                var requests = httpClientMock.GetLogs();

                foreach (var req in requests)
                {
                    Assert.IsTrue(req
                        .RequestMessage
                        .Headers
                        .Any(h => h.Key.Equals("SplitSDKMachineIP") || h.Key.Equals("SplitSDKMachineName")));
                }

                client.Destroy();
            }
        }

        [TestMethod]
        public void CheckingHeaders_WithIPAddressesDisabled_ReturnsWithoutIpAndName()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl(), ipAddressesEnabled: false);

                var apikey = "apikey8";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                // Act.
                var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

                // Assert.
                Assert.AreEqual("on", treatmentResult);

                Thread.Sleep(5000);

                var requests = httpClientMock.GetLogs();

                foreach (var req in requests)
                {
                    Assert.IsFalse(req
                        .RequestMessage
                        .Headers
                        .Any(h => h.Key.Equals("SplitSDKMachineIP") || h.Key.Equals("SplitSDKMachineName")));
                }

                client.Destroy();
            }
        }

        [TestMethod]
        public void GetTreatments_ValidateDedupeImpressions_Optimized()
        {
            // Arrange.           
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

                var apikey = "apikey9";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                // Act.
                client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
                client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
                client.GetTreatmentWithConfig("test", "MAURO_TEST");
                client.GetTreatmentWithConfig("mauro", "MAURO_TEST");
                client.GetTreatmentWithConfig("mauro", "MAURO_TEST");
                client.GetTreatments("admin", new List<string> { "FACUNDO_TEST", "Test_Save_1" });
                client.GetTreatment("admin", "FACUNDO_TEST");
                client.GetTreatment("admin", "Test_Save_1");
                client.GetTreatmentsWithConfig("admin", new List<string> { "FACUNDO_TEST", "MAURO_TEST" });

                client.Destroy();
                Thread.Sleep(2000);

                // Assert.
                var sentImpressions = GetImpressionsSentBackend(httpClientMock);
                Assert.AreEqual(3, sentImpressions.Select(x => x.F).Distinct().Count());
                Assert.AreEqual(2, sentImpressions.Where(x => x.F.Equals("FACUNDO_TEST")).Sum(x => x.I.Count));
                Assert.AreEqual(3, sentImpressions.Where(x => x.F.Equals("MAURO_TEST")).Sum(x => x.I.Count));
                Assert.AreEqual(1, sentImpressions.Where(x => x.F.Equals("Test_Save_1")).Sum(x => x.I.Count));

                var impressionCounts = GetImpressionsCountsSentBackend(httpClientMock);
                //Assert.AreEqual(5, impressionCounts.Sum(x => x.Pf.Count()));                
                //Assert.AreEqual(5, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("FACUNDO_TEST")).Sum(z => z.Rc)));
                //Assert.AreEqual(4, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("MAURO_TEST")).Sum(z => z.Rc)));
                //Assert.AreEqual(2, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("Test_Save_1")).Sum(z => z.Rc)));
            }
        }

        [TestMethod]
        public void GetTreatments_ValidateDedupeImpressions_Debug()
        {
            // Arrange.
            using (var httpClientMock = GetHttpClientMock())
            {
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl());
                configurations.ImpressionsMode = ImpressionsMode.Debug;

                var apikey = "apikey10";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();

                client.BlockUntilReady(10000);

                // Act.
                client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
                client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
                client.GetTreatmentWithConfig("test", "MAURO_TEST");
                client.GetTreatmentWithConfig("mauro", "MAURO_TEST");
                client.GetTreatments("admin", new List<string> { "FACUNDO_TEST", "Test_Save_1" });
                client.GetTreatment("admin", "FACUNDO_TEST");
                client.GetTreatmentsWithConfig("admin", new List<string> { "FACUNDO_TEST", "MAURO_TEST" });

                client.Destroy();
                Thread.Sleep(2000);

                // Assert.
                var sentImpressions = GetImpressionsSentBackend(httpClientMock);
                Assert.AreEqual(3, sentImpressions.Select(x => x.F).Distinct().Count());
                Assert.AreEqual(5, sentImpressions.Where(x => x.F.Equals("FACUNDO_TEST")).Sum(x => x.I.Count));
                Assert.AreEqual(3, sentImpressions.Where(x => x.F.Equals("MAURO_TEST")).Sum(x => x.I.Count));
                Assert.AreEqual(1, sentImpressions.Where(x => x.F.Equals("Test_Save_1")).Sum(x => x.I.Count));

                var impressionCounts = GetImpressionsCountsSentBackend(httpClientMock);
                Assert.AreEqual(0, impressionCounts.Count);
            }
        }

        [TestMethod]
        public void Telemetry_ValidatesConfigInitAndStats()
        {
            // Arrange.
            using (var httpClientMock = GetHttpClientMock())
            {
                var impressionListener = new IntegrationTestsImpressionListener(50);
                var configurations = GetConfigurationOptions(httpClientMock.GetUrl(), impressionListener: impressionListener);

                var apikey = "apikey-telemetry";

                var splitFactory = new SplitFactory(apikey, configurations);
                var client = splitFactory.Client();
                client.Track("test-key", "tt", "test");

                try
                {
                    client.BlockUntilReady(0);
                }
                catch
                {
                    client.BlockUntilReady(10000);
                }

                // Act.
                var result = client.GetTreatment("nico_test", "FACUNDO_TEST");

                Thread.Sleep(3000);

                // Assert.
                Assert.AreEqual("on", result);

                var sentConfig = GetMetricsConfigSentBackend(httpClientMock);
                Assert.IsNotNull(sentConfig);
                Assert.AreEqual(configurations.StreamingEnabled, sentConfig.StreamingEnabled);
                Assert.AreEqual("memory", sentConfig.Storage);
                Assert.AreEqual(configurations.FeaturesRefreshRate, (int)sentConfig.Rates.Splits);
                Assert.AreEqual(configurations.SegmentsRefreshRate, (int)sentConfig.Rates.Events);
                Assert.AreEqual(60, (int)sentConfig.Rates.Impressions);
                Assert.AreEqual(3600, (int)sentConfig.Rates.Telemetry);
                Assert.IsTrue(sentConfig.UrlOverrides.Telemetry);
                Assert.IsTrue(sentConfig.UrlOverrides.Sdk);
                Assert.IsTrue(sentConfig.UrlOverrides.Events);
                Assert.IsFalse(sentConfig.UrlOverrides.Stream);
                Assert.IsFalse(sentConfig.UrlOverrides.Auth);
                Assert.AreEqual(30000, (int)sentConfig.ImpressionsQueueSize);
                Assert.AreEqual(5000, (int)sentConfig.EventsQueueSize);
                Assert.AreEqual(ImpressionsMode.Optimized, sentConfig.ImpressionsMode);
                Assert.IsTrue(sentConfig.ImpressionListenerEnabled);
                Assert.IsTrue(1 <= sentConfig.ActiveFactories);
                Assert.AreEqual(1, sentConfig.BURTimeouts);

                var sentStats = GetMetricsStatsSentBackend(httpClientMock);
                Assert.AreEqual(0, sentStats.Count);

                client.Destroy();
            }
        }

        #region Protected Methods
        protected override ConfigurationOptions GetConfigurationOptions(string url = null, int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            return new ConfigurationOptions
            {
                Endpoint = url,
                EventsEndpoint = url,
                TelemetryServiceURL = url,
                ReadTimeout = 20000,
                ConnectionTimeout = 20000,
                ImpressionListener = impressionListener,
                FeaturesRefreshRate = featuresRefreshRate ?? 1,
                SegmentsRefreshRate = 1,
                ImpressionsRefreshRate = 1,
                EventsPushRate = eventsPushRate ?? 1,
                EventsQueueSize = eventsQueueSize,
                IPAddressesEnabled = ipAddressesEnabled,
                StreamingEnabled = false,
            };
        }

        protected override HttpClientMock GetHttpClientMock()
        {
            var httpClientMock = new HttpClientMock();
            httpClientMock.SplitChangesOk("split_changes.json", "-1");
            httpClientMock.SplitChangesOk("split_changes_1.json", "1506703262916");

            httpClientMock.SegmentChangesOk("-1", "segment1");
            httpClientMock.SegmentChangesOk("1470947453877", "segment1");

            httpClientMock.SegmentChangesOk("-1", "segment2");
            httpClientMock.SegmentChangesOk("1470947453878", "segment2");

            httpClientMock.SegmentChangesOk("-1", "segment3");
            httpClientMock.SegmentChangesOk("1470947453879", "segment3");

            return httpClientMock;
        }

        protected override void AssertSentImpressions(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            if (sentImpressionsCount <= 0) return;

            var sentImpressions = GetImpressionsSentBackend(httpClientMock);

            var time = 1000;
            while (sentImpressionsCount != sentImpressions.Sum(si => si.I.Count))
            {
                if (time >= 10000)
                {
                    break;
                }

                Thread.Sleep(time);

                time = time + 100;
                sentImpressions = GetImpressionsSentBackend(httpClientMock);
            }

            Assert.AreEqual(sentImpressionsCount, sentImpressions.Sum(si => si.I.Count));

            foreach (var expectedImp in expectedImpressions)
            {
                var impressions = new List<ImpressionData>();

                foreach (var ki in sentImpressions.Where(si => si.F.Equals(expectedImp.feature)))
                {
                    impressions.AddRange(ki.I);
                }

                AssertImpression(expectedImp, impressions);
            }
        }

        protected void AssertImpression(KeyImpression impressionExpected, List<ImpressionData> sentImpressions)
        {
            Assert.IsTrue(sentImpressions
                .Where(si => impressionExpected.bucketingKey == si.B)
                .Where(si => impressionExpected.changeNumber == si.C)
                .Where(si => impressionExpected.keyName == si.K)
                .Where(si => impressionExpected.label == si.R)
                .Where(si => impressionExpected.treatment == si.T)
                .Any());
        }

        protected override void AssertSentEvents(List<EventBackend> eventsExpected, HttpClientMock httpClientMock = null, int sleepTime = 5000, int? eventsCount = null, bool validateEvents = true)
        {
            Thread.Sleep(sleepTime);

            var sentEvents = GetEventsSentBackend(httpClientMock);

            Assert.AreEqual(eventsCount ?? eventsExpected.Count, sentEvents.Count);

            if (validateEvents)
            {
                foreach (var expected in eventsExpected)
                {
                    Assert.IsTrue(sentEvents
                        .Where(ee => ee.Key == expected.Key)
                        .Where(ee => ee.EventTypeId == expected.EventTypeId)
                        .Where(ee => ee.Value == expected.Value)
                        .Where(ee => ee.TrafficTypeName == expected.TrafficTypeName)
                        .Where(ee => ee.Properties?.Count == expected.Properties?.Count)
                        .Any());
                }
            }
        }
        #endregion

        #region Private Methods
        private List<KeyImpressionBackend> GetImpressionsSentBackend(HttpClientMock httpClientMock = null)
        {
            var impressions = new List<KeyImpressionBackend>();
            var logs = httpClientMock.GetImpressionLogs();

            foreach (var log in logs)
            {
                var _impressions = JsonConvert.DeserializeObject<List<KeyImpressionBackend>>(log.RequestMessage.Body);

                impressions.AddRange(_impressions);
            }

            return impressions;
        }

        private Telemetry.Domain.Config GetMetricsConfigSentBackend(HttpClientMock httpClientMock)
        {
            var logs = httpClientMock.GetMetricsConfigLog();

            if (logs.FirstOrDefault() == null) return null;

            return JsonConvert.DeserializeObject<Telemetry.Domain.Config>(logs.FirstOrDefault().RequestMessage.Body);
        }

        private List<Telemetry.Domain.Stats> GetMetricsStatsSentBackend(HttpClientMock httpClientMock)
        {
            var stats = new List<Telemetry.Domain.Stats>();
            var logs = httpClientMock.GetMetricsUsageLog();

            foreach (var item in logs)
            {
                var stat = JsonConvert.DeserializeObject<Telemetry.Domain.Stats>(item.RequestMessage.Body);

                stats.Add(stat);
            }

            return stats;
        }

        private List<ImpressionCount> GetImpressionsCountsSentBackend(HttpClientMock httpClientMock = null)
        {
            var impressions = new List<ImpressionCount>();
            var logs = httpClientMock.GetImpressionCountsLogs();

            foreach (var log in logs)
            {
                var _impression = JsonConvert.DeserializeObject<ImpressionCount>(log.RequestMessage.Body);

                impressions.Add(_impression);
            }

            return impressions;
        }

        private List<EventBackend> GetEventsSentBackend(HttpClientMock httpClientMock = null)
        {
            var events = new List<EventBackend>();
            var logs = httpClientMock.GetEventsLog();

            foreach (var log in logs)
            {
                var _events = JsonConvert.DeserializeObject<List<EventBackend>>(log.RequestMessage.Body);

                foreach (var item in _events)
                {
                    events.Add(item);
                }
            }

            return events;
        }
        #endregion
    }
}
