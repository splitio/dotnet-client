using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Telemetry.Domain;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            var config = new RedisConfig
            {
                RedisHost = Host,
                RedisPort = Port,
                RedisPassword = Password,
                RedisDatabase = Database,
                PoolSize = 1
            };
            var pool = new ConnectionPoolManager(config);
            _redisAdapter = new RedisAdapter(config, pool);

            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestInitialize]
        public void Init()
        {
            LoadSplits();
        }

        [TestMethod]
        public async Task CheckingMachineIpAndMachineName_WithIPAddressesEnabled_ReturnsIpAndName()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            client.GetTreatment("mauro_test", "FACUNDO_TEST");
            client.GetTreatment("nico_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "FACUNDO_TEST");
            client.Track("mauro", "user", "event_type");
            client.Track("nicolas", "user_2", "event_type_2");
            client.Track("redo", "user_3", "event_type_3");

            // Assert.
            await Task.Delay(1500);

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
        public async Task CheckingMachineIpAndMachineName_WithIPAddressesDisabled_ReturnsNA()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(ipAddressesEnabled: false);

            var apikey = "apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            client.GetTreatment("mauro_test", "FACUNDO_TEST");
            client.GetTreatment("nico_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "FACUNDO_TEST");
            client.Track("mauro", "user", "event_type");
            client.Track("nicolas", "user_2", "event_type_2");
            client.Track("redo", "user_3", "event_type_3");

            // Assert.
            await Task.Delay(1500);

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

        // TODO: None mode is not supported yet.
        [Ignore]
        [TestMethod]
        public async Task GetTreatment_WithImpressionModeInNone_ShouldGetUniqueKeys()
        {
            // Arrange.
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
            client.GetTreatment("redo_test", "FACUNDO_TEST");
            client.GetTreatment("redo_test", "MAURO_TEST");

            client.Destroy();
            await Task.Delay(500);
            var result = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.uniquekeys");

            // Assert.
            Assert.AreEqual(2, result.Count());

            var uniques = result.Select(x => JsonConvert.DeserializeObject<Mtks>(x)).ToList();

            Assert.IsTrue(uniques.Any(u => u.Feature.Equals("FACUNDO_TEST") && u.Keys.Contains("mauro_test") && u.Keys.Contains("nico_test") && u.Keys.Contains("redo_test")));
            Assert.IsTrue(uniques.Any(u => u.Feature.Equals("MAURO_TEST") && u.Keys.Contains("redo_test")));

            CleanKeys();
        }

        // TODO: Optimized mode is not supported yet.
        [Ignore]
        [TestMethod]
        public async Task GetTreatment_WithImpressionModeOptimized_ShouldGetImpressionCount()
        {
            // Arrange.
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
            await Task.Delay(500);
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
            // No-op

            return null;
        }

        protected override async Task AssertSentImpressionsAsync(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            await Task.Delay(1500);

            var redisImpressions = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.impressions");

            Assert.AreEqual(sentImpressionsCount, redisImpressions.Length);

            foreach (var item in redisImpressions)
            {
                var actualImp = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                AssertImpression(actualImp, expectedImpressions.ToList());
            }
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExcpected, HttpClientMock httpClientMock = null, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true)
        {
            await Task.Delay(sleepTime);

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

        private static void AssertImpression(KeyImpressionRedis impressionActual, List<KeyImpression> sentImpressions)
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

        private static void AssertEvent(EventRedis eventActual, List<EventBackend> eventsExcpected)
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
