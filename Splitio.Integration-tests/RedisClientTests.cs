using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Telemetry.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Integration_tests
{
    [TestClass]
    public class RedisClientTests : BaseIntegrationTests
    {
        private const string Host = "localhost";
        private const string Port = "6379";
        private const string Password = "";
        private const int Database = 0;
        private const string UserPrefix = "prefix-test";

        private readonly RedisAdapterForTests _redisAdapter;
        private readonly string rootFilePath;

        public RedisClientTests() : base("Redis")
        {
            var config = new RedisConfig
            {
                RedisHost = Host,
                RedisPort = Port,
                RedisPassword = Password,
                RedisDatabase = Database,
                PoolSize = 1,
                RedisUserPrefix = UserPrefix,
            };
            var pool = new ConnectionPoolManager(config);
            _redisAdapter = new RedisAdapterForTests(config, pool);

            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestInitialize]
        public async Task InitAsync()
        {
            await RedisHelper.LoadSplitsAsync(rootFilePath, UserPrefix, _redisAdapter);
        }

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

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("control", result["Random_Treatment"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);
            Assert.AreEqual("control", result["Random_Treatment_2"]);

            client.Destroy();

            // Validate impressions.
            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            Helper.AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            Helper.AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            Helper.AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            Assert.AreEqual(3, impressionListener.Count());

            //Validate impressions sent to the be.            
            await AssertSentImpressionsAsync(3, impression1, impression2, impression3);
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
        }

        // TODO: None mode is not supported yet.
        [Ignore]
        [TestMethod]
        public void GetTreatment_WithImpressionModeInNone_ShouldGetUniqueKeys()
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
            var result = _redisAdapter.ListRange($"{UserPrefix}.SPLITIO.uniquekeys");

            // Assert.
            Assert.AreEqual(2, result.Count());

            var uniques = result.Select(x => JsonConvert.DeserializeObject<Mtks>(x)).ToList();

            Assert.IsTrue(uniques.Any(u => u.Feature.Equals("FACUNDO_TEST") && u.Keys.Contains("mauro_test") && u.Keys.Contains("nico_test") && u.Keys.Contains("redo_test")));
            Assert.IsTrue(uniques.Any(u => u.Feature.Equals("MAURO_TEST") && u.Keys.Contains("redo_test")));
        }

        // TODO: Optimized mode is not supported yet.
        [Ignore]
        [TestMethod]
        public void GetTreatment_WithImpressionModeOptimized_ShouldGetImpressionCount()
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
            var result = client.GetTreatmentsWithConfigByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3" });
            client.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);

            await DelayAsync();
            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");

            Helper.AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(1, impression1);
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
            var result = client.GetTreatmentsByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3" });
            client.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            await DelayAsync();
            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");

            Helper.AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(1, impression1);
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
            var result = client.GetTreatmentsWithConfigByFlagSet("nico_test", "set_1");
            client.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);

            await DelayAsync();
            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");

            Helper.AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(1, impression1);
        }

        #region Protected Methods
        protected override ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
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

        protected override async Task AssertSentImpressionsAsync(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            await RedisHelper.AssertSentImpressionsAsync(_redisAdapter, UserPrefix, sentImpressionsCount, expectedImpressions);
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExcpected, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true)
        {
            await RedisHelper.AssertSentEventsAsync(_redisAdapter, UserPrefix, eventsExcpected, sleepTime, eventsCount, validateEvents);
        }
        #endregion

        [TestCleanup]
        public void CleanKeys()
        {
            RedisHelper.Cleanup(UserPrefix, _redisAdapter);
        }
    }
}
