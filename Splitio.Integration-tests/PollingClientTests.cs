using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Integration_tests
{
    [TestClass]
    public class PollingClientTests : BaseIntegrationTests
    {
        [TestMethod]
        public async Task GetTreatments_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey9";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_2", });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("control", result["Random_Treatment"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);
            Assert.AreEqual("control", result["Random_Treatment_2"]);

            client.Destroy();

            // Validate impressions.
            var keyImpressions = impressionListener.GetQueue();

            var impression1 = keyImpressions
                .Where(ki => ki.feature.Equals("FACUNDO_TEST"))
                .Where(ki => ki.keyName.Equals("nico_test"))
                .FirstOrDefault();

            var impression2 = keyImpressions
                .Where(ki => ki.feature.Equals("MAURO_TEST"))
                .Where(ki => ki.keyName.Equals("nico_test"))
                .FirstOrDefault();

            var impression3 = keyImpressions
                .Where(ki => ki.feature.Equals("Test_Save_1"))
                .Where(ki => ki.keyName.Equals("nico_test"))
                .FirstOrDefault();

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            Assert.AreEqual(3, keyImpressions.Count);

            //Validate impressions sent to the be.            
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }

        [TestMethod]
        public void GetTreatment_WithoutBUR_ReturnsControl()
        {
            // Arrange.
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

        [TestMethod]
        public void GetTreatmentWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange
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

        [TestMethod]
        public void GetTreatments_WithoutBUR_ReturnsControl()
        {
            // Arrange.
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

        [TestMethod]
        public void GetTreatmentsWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange.
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

        [TestMethod]
        public void GetTreatment_WithtBUR_ReturnsTimeOutException()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

            var apikey = "apikey5";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            // Act.
            var exceptionMessage = "";
            var isSdkReady = false;

            try
            {
                client.BlockUntilReady(0);
                isSdkReady = true;
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            // Assert.
            Assert.IsFalse(isSdkReady);
            Assert.AreEqual("SDK was not ready in 0 milliseconds", exceptionMessage);

            client.Destroy();
        }

        [TestMethod]
        public void Manager_SplitNames_WithoutBUR_ReturnsNull()
        {
            // Arrange.
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

        [TestMethod]
        public async Task CheckingHeaders_WithIPAddressesEnabled_ReturnsWithIpAndName()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock.GetUrl());

            var apikey = "apikey7";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

            // Assert.
            Assert.AreEqual("on", treatmentResult);

            await Task.Delay(5000);

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

        [TestMethod]
        public async Task CheckingHeaders_WithIPAddressesDisabled_ReturnsWithoutIpAndName()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock.GetUrl(), ipAddressesEnabled: false);

            var apikey = "apikey8";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

            // Assert.
            Assert.AreEqual("on", treatmentResult);

            await Task.Delay(5000);

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

        [TestMethod]
        public void GetTreatments_ValidateDedupeImpressions_Optimized()
        {
            // Arrange.
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

            // Assert.
            var sentImpressions = GetImpressionsSentBackend(httpClientMock);
            Assert.AreEqual(3, sentImpressions.Select(x => x.F).Distinct().Count(), "1");
            Assert.AreEqual(2, sentImpressions.Where(x => x.F.Equals("FACUNDO_TEST")).Sum(x => x.I.Count), "2");
            Assert.AreEqual(3, sentImpressions.Where(x => x.F.Equals("MAURO_TEST")).Sum(x => x.I.Count), "3");
            Assert.AreEqual(1, sentImpressions.Where(x => x.F.Equals("Test_Save_1")).Sum(x => x.I.Count), "4");

            var impressionCounts = GetImpressionsCountsSentBackend(httpClientMock);
            var names = new List<string>();
            impressionCounts.ForEach(item => names.AddRange(item.Pf.Select(x => x.F)));
            Assert.AreEqual(3, names.Distinct().Count(), "5");
            Assert.AreEqual(3, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("FACUNDO_TEST")).Sum(z => z.Rc)), "6");
            Assert.AreEqual(1, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("MAURO_TEST")).Sum(z => z.Rc)), "7");
            Assert.AreEqual(1, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("Test_Save_1")).Sum(z => z.Rc)), "8");
        }

        [TestMethod]
        public void GetTreatments_ValidateDedupeImpressions_Debug()
        {
            // Arrange.
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

            // Assert.
            var sentImpressions = GetImpressionsSentBackend(httpClientMock);
            Assert.AreEqual(3, sentImpressions.Select(x => x.F).Distinct().Count());
            Assert.AreEqual(5, sentImpressions.Where(x => x.F.Equals("FACUNDO_TEST")).Sum(x => x.I.Count));
            Assert.AreEqual(3, sentImpressions.Where(x => x.F.Equals("MAURO_TEST")).Sum(x => x.I.Count));
            Assert.AreEqual(1, sentImpressions.Where(x => x.F.Equals("Test_Save_1")).Sum(x => x.I.Count));

            var impressionCounts = GetImpressionsCountsSentBackend(httpClientMock);
            Assert.AreEqual(0, impressionCounts.Count);
        }

        // TODO: None mode is not supported yet.
        [Ignore]
        [TestMethod]
        public void GetTreatments_WithImpressionsInNoneMode()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock.GetUrl());
            configurations.ImpressionsMode = ImpressionsMode.None;

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

            // Assert.
            var sentImpressions = GetImpressionsSentBackend(httpClientMock);
            Assert.AreEqual(0, sentImpressions.Count);

            var impressionCounts = GetImpressionsCountsSentBackend(httpClientMock);
            var names = new List<string>();
            impressionCounts.ForEach(item => names.AddRange(item.Pf.Select(x => x.F)));
            Assert.AreEqual(3, names.Distinct().Count(), "5");
            Assert.AreEqual(5, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("FACUNDO_TEST")).Sum(z => z.Rc)), "6");
            Assert.AreEqual(3, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("MAURO_TEST")).Sum(z => z.Rc)), "7");
            Assert.AreEqual(1, impressionCounts.Sum(x => x.Pf.Where(i => i.F.Equals("Test_Save_1")).Sum(z => z.Rc)), "8");
        }

        [TestMethod]
        public async Task Telemetry_ValidatesConfigInitAndStats()
        {
            // Arrange.
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

            await Task.Delay(5000);

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
            Assert.AreEqual(10000, (int)sentConfig.EventsQueueSize);
            Assert.AreEqual(ImpressionsMode.Optimized, sentConfig.ImpressionsMode);
            Assert.IsTrue(sentConfig.ImpressionListenerEnabled);
            Assert.IsTrue(1 <= sentConfig.ActiveFactories);
            // TODO: after refactor the sdk is ready in 0 seconds 
            //Assert.AreEqual(1, sentConfig.BURTimeouts);

            var sentStats = GetMetricsStatsSentBackend(httpClientMock);
            Assert.AreEqual(0, sentStats.Count);

            client.Destroy();
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

        protected override Task AssertSentImpressionsAsync(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            if (sentImpressionsCount <= 0) return Task.FromResult(0);

            var sentImpressions = GetImpressionsSentBackend(httpClientMock);

            Assert.AreEqual(sentImpressionsCount, sentImpressions.Sum(si => si.I.Count), "AssertSentImpressions");

            foreach (var expectedImp in expectedImpressions)
            {
                var impressions = new List<ImpressionData>();

                foreach (var ki in sentImpressions.Where(si => si.F.Equals(expectedImp.feature)))
                {
                    impressions.AddRange(ki.I);
                }

                AssertImpression(expectedImp, impressions);
            }

            return Task.FromResult(0);
        }

        protected static void AssertImpression(KeyImpression impressionExpected, List<ImpressionData> sentImpressions)
        {
            Assert.IsTrue(sentImpressions
                .Where(si => impressionExpected.bucketingKey == si.B)
                .Where(si => impressionExpected.changeNumber == si.C)
                .Where(si => impressionExpected.keyName == si.K)
                .Where(si => impressionExpected.label == si.R)
                .Where(si => impressionExpected.treatment == si.T)
                .Any());
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExpected, HttpClientMock httpClientMock = null, int sleepTime = 5000, int? eventsCount = null, bool validateEvents = true)
        {
            await Task.Delay(sleepTime);

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
        private static List<KeyImpressionBackend> GetImpressionsSentBackend(HttpClientMock httpClientMock = null)
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

        private static Telemetry.Domain.Config GetMetricsConfigSentBackend(HttpClientMock httpClientMock)
        {
            var logs = httpClientMock.GetMetricsConfigLog();

            if (logs.FirstOrDefault() == null) return null;

            return JsonConvert.DeserializeObject<Telemetry.Domain.Config>(logs.FirstOrDefault().RequestMessage.Body);
        }

        private static List<Telemetry.Domain.Stats> GetMetricsStatsSentBackend(HttpClientMock httpClientMock)
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

        private static List<ImpressionCount> GetImpressionsCountsSentBackend(HttpClientMock httpClientMock = null)
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

        private static List<EventBackend> GetEventsSentBackend(HttpClientMock httpClientMock = null)
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
