using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Integration_tests.Resources;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Integration_tests
{
    [TestClass]
    public class RedisTests : BaseIntegrationTests
    {
        private const string Host = "localhost";
        private const string Port = "6379";
        private const string Password = "";
        private const int Database = 0;
        private const string UserPrefix = "prefix-test";

        private readonly IRedisAdapter _redisAdapter;
        private readonly string rootFilePath;

        public RedisTests()
        {
            _redisAdapter = new RedisAdapter(Host, Port, Password, Database);
            _redisAdapter.Connect();

            rootFilePath = string.Empty;

#if NETCORE
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public void CheckingMachineIpAndMachineName_WithIPAddressesEnabled_ReturnsIpAndName()
        {
            // Arrange.
            LoadSplits();

            var configurations = GetConfigurationOptions();

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var treatmentResult1 = client.GetTreatment("mauro_test", "FACUNDO_TEST");
            var treatmentResult2 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var treatmentResult3 = client.GetTreatment("redo_test", "FACUNDO_TEST");
            var trackResult1 = client.Track("mauro", "user", "event_type");
            var trackResult2 = client.Track("nicolas", "user_2", "event_type_2");
            var trackResult3 = client.Track("redo", "user_3", "event_type_3");

            // Assert.
            Thread.Sleep(1500);

            // Impressions
            var redisImpressions = _redisAdapter.ListRange("SPLITIO.impressions");

            foreach (var item in redisImpressions)
            {
                var impression = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                Assert.AreNotEqual("NA", impression.M.I);
                Assert.AreNotEqual("NA", impression.M.N);
            }

            // Events 
            var sdkVersion = string.Empty;
            var redisEvents = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.events");

            foreach (var item in redisEvents)
            {
                var eventRedis = JsonConvert.DeserializeObject<EventRedis>(item);

                Assert.AreNotEqual("NA", eventRedis.M.I);
                Assert.AreNotEqual("NA", eventRedis.M.N);

                sdkVersion = eventRedis.M.S;
            }

            // Metrics
            var keys = _redisAdapter.Keys($"{UserPrefix}.SPLITIO/{sdkVersion}/*");

            foreach (var key in keys)
            {
                Assert.IsFalse(key.ToString().Contains("/NA/"));
            }

            CleanKeys();
        }

        [TestMethod]
        public void CheckingMachineIpAndMachineName_WithIPAddressesDisabled_ReturnsNA()
        {
            // Arrange.
            
            LoadSplits();

            var configurations = GetConfigurationOptions(ipAddressesEnabled: false);

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var treatmentResult1 = client.GetTreatment("mauro_test", "FACUNDO_TEST");
            var treatmentResult2 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var treatmentResult3 = client.GetTreatment("redo_test", "FACUNDO_TEST");
            var trackResult1 = client.Track("mauro", "user", "event_type");
            var trackResult2 = client.Track("nicolas", "user_2", "event_type_2");
            var trackResult3 = client.Track("redo", "user_3", "event_type_3");

            // Assert.
            Thread.Sleep(1500);

            // Impressions
            var redisImpressions = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.impressions");

            foreach (var item in redisImpressions)
            {
                var impression = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                Assert.AreEqual("NA", impression.M.I);
                Assert.AreEqual("NA", impression.M.N);
            }

            // Events 
            var sdkVersion = string.Empty;
            var redisEvents = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.events");

            foreach (var item in redisEvents)
            {
                var eventRedis = JsonConvert.DeserializeObject<EventRedis>(item);

                Assert.AreEqual("NA", eventRedis.M.I);
                Assert.AreEqual("NA", eventRedis.M.N);

                sdkVersion = eventRedis.M.S;
            }

            // Metrics
            var keys = _redisAdapter.Keys($"{UserPrefix}.SPLITIO/{sdkVersion}/*");

            foreach (var key in keys)
            {
                Assert.IsTrue(key.ToString().Contains("/NA/"));
            }

            CleanKeys();
        }

        [TestMethod]
        public void GetTreatment_WithImpressionModeInNone_ShouldGetUniqueKeys()
        {
            // Arrange.
            
            LoadSplits();

            var configurations = GetConfigurationOptions(ipAddressesEnabled: false);
            configurations.ImpressionsMode = ImpressionsMode.None;

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            client.GetTreatment("mauro_test", "FACUNDO_TEST");
            client.GetTreatment("nico_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "MAURO_TEST");

            client.Destroy();
            Thread.Sleep(500);
            var result = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.uniquekeys");

            // Assert.
            Assert.AreEqual(4, result.Count());
            Assert.IsTrue(result.Contains("FACUNDO_TEST::mauro_test"));
            Assert.IsTrue(result.Contains("FACUNDO_TEST::nico_test"));
            Assert.IsTrue(result.Contains("FACUNDO_TEST::redo_test"));
            Assert.IsTrue(result.Contains("MAURO_TEST::redo_test"));

            CleanKeys();
        }

        [TestMethod]
        public void GetTreatment_WithImpressionModeOptimized_ShouldGetImpressionCount()
        {
            // Arrange.
            
            LoadSplits();

            var configurations = GetConfigurationOptions(ipAddressesEnabled: false);
            configurations.ImpressionsMode = ImpressionsMode.Optimized;

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            client.GetTreatment("mauro_test", "FACUNDO_TEST");
            client.GetTreatment("nico_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "FACUNDO_TEST");
            client.GetTreatment("nico_test", "FACUNDO_TEST");

            client.GetTreatment("redo_test", "MAURO_TEST");
            client.GetTreatment("test_test", "MAURO_TEST");
            client.GetTreatment("redo_test", "MAURO_TEST");

            client.Destroy();
            Thread.Sleep(500);
            var result = _redisAdapter.HashGetAll($"{UserPrefix}.SPLITIO.impressions.count");
            var redisImpressions = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.impressions");

            // Assert.
            Assert.AreEqual(4, result.FirstOrDefault(x => ((string)x.Name).Contains("FACUNDO_TEST")).Value);
            Assert.AreEqual(3, result.FirstOrDefault(x => ((string)x.Name).Contains("MAURO_TEST")).Value);
            Assert.AreEqual(5, redisImpressions.Count());

            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("mauro_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("nico_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("redo_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("MAURO_TEST") && ((string)x).Contains("redo_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("MAURO_TEST") && ((string)x).Contains("test_test")));

            CleanKeys();
        }

        #region Protected Methods
        protected override ConfigurationOptions GetConfigurationOptions(string url = null, int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            var cacheConfig = new CacheAdapterConfigurationOptions
            {
                Host = Host,
                Port = Port,
                Password = Password,
                Database = Database,
                UserPrefix = UserPrefix
            };

            return new ConfigurationOptions
            {
                ImpressionListener = impressionListener,
                FeaturesRefreshRate = featuresRefreshRate ?? 1,
                SegmentsRefreshRate = 1,
                ImpressionsRefreshRate = 1,
                EventsPushRate = eventsPushRate ?? 1,
                IPAddressesEnabled = ipAddressesEnabled,
                CacheAdapterConfig = cacheConfig,
                Mode = Mode.Consumer
            };
        }

        protected override HttpClientMock GetHttpClientMock()
        {
            LoadSplits();

            return null;
        }

        protected override void AssertSentImpressions(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            Thread.Sleep(1500);

            var redisImpressions = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.impressions");

            Assert.AreEqual(sentImpressionsCount, redisImpressions.Length);

            foreach (var item in redisImpressions)
            {
                var actualImp = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                AssertImpression(actualImp, expectedImpressions.ToList());
            }
        }

        protected override void AssertSentEvents(List<EventBackend> eventsExcpected, HttpClientMock httpClientMock = null, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true)
        {
            Thread.Sleep(sleepTime);

            var redisEvents = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.events");

            Assert.AreEqual(eventsExcpected.Count, redisEvents.Length);

            foreach (var item in redisEvents)
            {
                var actualEvent = JsonConvert.DeserializeObject<EventRedis>(item);

                AssertEvent(actualEvent, eventsExcpected);
            }
        }
        #endregion

        #region Private Methods
        private void CleanKeys(string pattern = UserPrefix)
        {
            var keys = _redisAdapter.Keys($"{pattern}*");

            foreach (var k in keys)
            {
                _redisAdapter.Del(k);
            }
        }

        private void AssertImpression(KeyImpressionRedis impressionActual, List<KeyImpression> sentImpressions)
        {
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.I));
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.N));
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.S));

            Assert.IsTrue(sentImpressions
                .Where(si => impressionActual.I.B == si.bucketingKey)
                .Where(si => impressionActual.I.C == si.changeNumber)
                .Where(si => impressionActual.I.K == si.keyName)
                .Where(si => impressionActual.I.R == si.label)
                .Where(si => impressionActual.I.T == si.treatment)
                .Any());
        }

        private void AssertEvent(EventRedis eventActual, List<EventBackend> eventsExcpected)
        {
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.I));
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.N));
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.S));

            Assert.IsTrue(eventsExcpected
                .Where(ee => eventActual.E.EventTypeId == ee.EventTypeId)
                .Where(ee => eventActual.E.Key == ee.Key)
                .Where(ee => eventActual.E.Properties?.Count == ee.Properties?.Count)
                .Where(ee => eventActual.E.TrafficTypeName == ee.TrafficTypeName)
                .Where(ee => eventActual.E.Value == ee.Value)
                .Any());
        }

        private void LoadSplits()
        {
            CleanKeys(UserPrefix);

            var splitsJson = File.ReadAllText($"{rootFilePath}split_changes.json");

            var splitResult = JsonConvert.DeserializeObject<SplitChangesResult>(splitsJson);

            foreach (var split in splitResult.splits)
            {
                _redisAdapter.Set($"{UserPrefix}.SPLITIO.split.{split.name}", JsonConvert.SerializeObject(split));
            }
        }
        #endregion
    }
}
