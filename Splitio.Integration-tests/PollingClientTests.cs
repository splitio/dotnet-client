﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        private readonly HttpClientMock httpClientMock = new HttpClientMock("PollingClientTests");

        public PollingClientTests() : base("Polling")
        { }

        [TestMethod]
        public async Task GetTreatments_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey9";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_2", });
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("control", result["Random_Treatment"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);
            Assert.AreEqual("control", result["Random_Treatment_2"]);

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impressionExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            var impressionExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, impressionExpected1, impressionExpected2, impressionExpected3);
            await AssertImpressionListenerAsync(3, impressionListener);

            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impressionExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impressionExpected3);
        }

        [TestMethod]
        public async Task GetTreatment_WithoutBUR_ReturnsControl()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            // Act.
            var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");

            // Assert.
            Assert.AreEqual("control", treatmentResult);

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange
            var configurations = GetConfigurationOptions();

            var apikey = "apikey2";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            // Act.
            var treatmentResult = client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");

            // Assert.
            Assert.AreEqual("control", treatmentResult.Treatment);
            Assert.IsNull(treatmentResult.Config);

            client.BlockUntilReady(10000);
            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatments_WithoutBUR_ReturnsControl()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "apikey3";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            // Act.
            var treatmentResults = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST" });

            // Assert.            
            Assert.AreEqual("control", treatmentResults["FACUNDO_TEST"]);
            Assert.AreEqual("control", treatmentResults["MAURO_TEST"]);

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfig_WithoutBUR_ReturnsControl()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatment_WithtBUR_ReturnsTimeOutException()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task Manager_SplitNames_WithoutBUR_ReturnsNull()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "apikey6";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            // Act.
            var result = manager.SplitNames();

            // Assert.
            Assert.IsNull(result);

            await splitFactory.Client().DestroyAsync();
        }

        [TestMethod]
        public async Task CheckingHeaders_WithIPAddressesEnabled_ReturnsWithIpAndName()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task CheckingHeaders_WithIPAddressesDisabled_ReturnsWithoutIpAndName()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(ipAddressesEnabled: false);

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

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatments_ValidateDedupeImpressions_Optimized()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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

            await client.DestroyAsync();

            // Assert.
            var sentImpressions = InMemoryHelper.GetImpressionsSentBackend(httpClientMock);
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
        public async Task GetTreatments_ValidateDedupeImpressions_Debug()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();
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
            await client.DestroyAsync();

            // Assert.
            var sentImpressions = InMemoryHelper.GetImpressionsSentBackend(httpClientMock);
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
            var configurations = GetConfigurationOptions();
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
            var sentImpressions = InMemoryHelper.GetImpressionsSentBackend(httpClientMock);
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
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSets_WithFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);
            configurations.FlagSetsFilter = new List<string> { "set 1", "SET 2", "set3", "seto8787987979uiuyiuiyui@@", null, string.Empty };

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSets("key", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.AreEqual(0, impressionListener.Count(), "InMemory: Impression Listener not match");
        }

        [TestMethod]
        public async Task GetTreatmentsByFlagSets_WithFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);
            configurations.FlagSetsFilter = new List<string> { "set 1", "SET 2", "set3", "seto8787987979uiuyiuiyui@@", null, string.Empty };

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSets("key", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.AreEqual(0, impressionListener.Count(), "InMemory: Impression Listener not match");
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSet_WithFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);
            configurations.FlagSetsFilter = new List<string> { "set 1", "SET 2", "set3", "seto8787987979uiuyiuiyui@@", null, string.Empty };

            var apikey = "GetTreatmentsWithConfigByFlagSet";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSet("key", "set_5");
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.AreEqual(0, impressionListener.Count(), "InMemory: Impression Listener not match");
        }

        [TestMethod]
        public async Task GetTreatmentsByFlagSet_WithFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);
            configurations.FlagSetsFilter = new List<string> { "set 1", "SET 2", "set3", "seto8787987979uiuyiuiyui@@", null, string.Empty };

            var apikey = "GetTreatmentsByFlagSet";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSet("key", "set_5");
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.AreEqual(0, impressionListener.Count(), "InMemory: Impression Listener not match");
        }

        #region Protected Methods
        protected override ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            return new ConfigurationOptions
            {
                Endpoint = httpClientMock.GetUrl(),
                EventsEndpoint = httpClientMock.GetUrl(),
                TelemetryServiceURL = httpClientMock.GetUrl(),
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

        protected override async Task AssertSentImpressionsAsync(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            // await InMemoryHelper.AssertSentImpressionsAsync(sentImpressionsCount, httpClientMock, expectedImpressions);

            await Task.FromResult(0);
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExpected, int? eventsCount = null, bool validateEvents = true)
        {
            await InMemoryHelper.AssertSentEventsAsync(eventsExpected, httpClientMock, eventsCount, validateEvents);
        }
        #endregion

        #region Private Methods
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
        #endregion
    }
}
