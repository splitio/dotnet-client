using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Tests.Common;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Integration_redis_tests
{
    [TestClass, TestCategory("Integration")]
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
            await RedisHelper.LoadSplitsAsync(rootFilePath, "{SPLITIO}" + UserPrefix, _redisAdapter);
        }

        [TestCleanup]
        public void CleanKeys()
        {
            RedisHelper.Cleanup(UserPrefix, _redisAdapter);
            RedisHelper.Cleanup("{SPLITIO}" + UserPrefix, _redisAdapter);
        }

        [TestMethod]
        public void GetTreatments_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
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

            // Validate impressions
            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impressionExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            var impressionExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.            
            AssertSentImpressions(3, impressionExpected1, impressionExpected2, impressionExpected3);
            AssertImpressionListener(3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impressionExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impressionExpected3);

            configurations = GetClusterConfigurationOptions(impressionListener: impressionListener);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();

            client2.BlockUntilReady(10000);

            // Act.
            result = client2.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_2", });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("control", result["Random_Treatment"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);
            Assert.AreEqual("control", result["Random_Treatment_2"]);

            client2.Destroy();

            // Validate impressions
            impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            impressionExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            impressionExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.            
            AssertSentImpressions(3, impressionExpected1, impressionExpected2, impressionExpected3);
            AssertImpressionListener(3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impressionExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impressionExpected3);
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

            configurations = GetClusterConfigurationOptions();

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();

            client2.BlockUntilReady(10000);
            client2.GetTreatment("mauro_test", "FACUNDO_TEST");
            client2.GetTreatment("nico_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "FACUNDO_TEST");
            client2.Track("mauro", "user", "event_type");
            client2.Track("nicolas", "user_2", "event_type_2");
            client2.Track("redo", "user_3", "event_type_3");

            // Assert.
            await Task.Delay(1500);

            // Impressions
            redisImpressions = _redisAdapter.ListRange("SPLITIO.impressions");

            foreach (var item in redisImpressions)
            {
                var impression = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                Assert.AreNotEqual("NA", impression.M.I);
                Assert.AreNotEqual("NA", impression.M.N);
            }

            // Events 
            sdkVersion = string.Empty;
            redisEvents = _redisAdapter.ListRange($"{{SPLITIO}}{UserPrefix}.SPLITIO.events");

            foreach (var item in redisEvents)
            {
                var eventRedis = JsonConvert.DeserializeObject<EventRedis>(item);

                Assert.AreNotEqual("NA", eventRedis.M.I);
                Assert.AreNotEqual("NA", eventRedis.M.N);

                sdkVersion = eventRedis.M.S;
            }

            // Metrics
            keys = _redisAdapter.Keys($"{{SPLITIO}}{UserPrefix}.SPLITIO/{sdkVersion}/*");

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

            configurations = GetClusterConfigurationOptions(ipAddressesEnabled: false);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            client2.GetTreatment("mauro_test", "FACUNDO_TEST");
            client2.GetTreatment("nico_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "FACUNDO_TEST");
            client2.Track("mauro", "user", "event_type");
            client2.Track("nicolas", "user_2", "event_type_2");
            client2.Track("redo", "user_3", "event_type_3");

            // Assert.
            await Task.Delay(1500);

            // Impressions
            redisImpressions = _redisAdapter.ListRange($"{{SPLITIO}}{UserPrefix}.SPLITIO.impressions");

            foreach (var item in redisImpressions)
            {
                var impression = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                Assert.AreEqual("NA", impression.M.I);
                Assert.AreEqual("NA", impression.M.N);
            }

            // Events 
            sdkVersion = string.Empty;
            redisEvents = _redisAdapter.ListRange($"{{SPLITIO}}{UserPrefix}.SPLITIO.events");

            foreach (var item in redisEvents)
            {
                var eventRedis = JsonConvert.DeserializeObject<EventRedis>(item);

                Assert.AreEqual("NA", eventRedis.M.I);
                Assert.AreEqual("NA", eventRedis.M.N);

                sdkVersion = eventRedis.M.S;
            }

            // Metrics
            keys = _redisAdapter.Keys($"{{SPLITIO}}{UserPrefix}.SPLITIO/{sdkVersion}/*");

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

            configurations = GetClusterConfigurationOptions(ipAddressesEnabled: false);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            client2.GetTreatment("mauro_test", "FACUNDO_TEST");
            client2.GetTreatment("nico_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "MAURO_TEST");

            client2.Destroy();
            result = _redisAdapter.ListRange($"{{SPLITIO}}{UserPrefix}.SPLITIO.uniquekeys");

            // Assert.
            Assert.AreEqual(2, result.Length);

            uniques = result.Select(x => JsonConvert.DeserializeObject<Mtks>(x)).ToList();

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

            configurations = GetClusterConfigurationOptions(ipAddressesEnabled: false);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            client2.GetTreatment("mauro_test", "FACUNDO_TEST");
            client2.GetTreatment("nico_test", "FACUNDO_TEST");
            client2.GetTreatment("redo_test", "FACUNDO_TEST");
            client2.GetTreatment("nico_test", "FACUNDO_TEST");

            client2.GetTreatment("redo_test", "MAURO_TEST");
            client2.GetTreatment("test_test", "MAURO_TEST");
            client2.GetTreatment("redo_test", "MAURO_TEST");

            client2.Destroy();
            result = _redisAdapter.HashGetAll($"{{SPLITIO}}{UserPrefix}.SPLITIO.impressions.count");
            redisImpressions = _redisAdapter.ListRange($"{{SPLITIO}}{UserPrefix}.SPLITIO.impressions");

            // Assert.
            Assert.AreEqual(4, result.FirstOrDefault(x => ((string)x.Name).Contains("FACUNDO_TEST")).Value);
            Assert.AreEqual(3, result.FirstOrDefault(x => ((string)x.Name).Contains("MAURO_TEST")).Value);
            Assert.AreEqual(5, redisImpressions.Length);

            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("mauro_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("nico_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("FACUNDO_TEST") && ((string)x).Contains("redo_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("MAURO_TEST") && ((string)x).Contains("redo_test")));
            Assert.AreEqual(1, redisImpressions.Count(x => ((string)x).Contains("MAURO_TEST") && ((string)x).Contains("test_test")));
        }

        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSets_WithFlagSetsInConfig()
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

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);
            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);

            configurations = GetClusterConfigurationOptions(impressionListener: impressionListener);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            result = client2.GetTreatmentsWithConfigByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3" });
            client2.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);

            impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);
            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
        }

        [TestMethod]
        public void GetTreatmentsByFlagSets_WithFlagSetsInConfig()
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

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);

            configurations = GetClusterConfigurationOptions(impressionListener: impressionListener);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            result = client2.GetTreatmentsByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3" });
            client2.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
        }

        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSet_WithFlagSetsInConfig()
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

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);

            configurations = GetClusterConfigurationOptions(impressionListener: impressionListener);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            result = client2.GetTreatmentsWithConfigByFlagSet("nico_test", "set_1");
            client2.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);

            impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
        }

        [TestMethod]
        public void GetTreatmentsByFlagSet_WithFlagSetsInConfig()
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
            var result = client.GetTreatmentsByFlagSet("nico_test", "set_1");
            client.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);

            configurations = GetClusterConfigurationOptions(impressionListener: impressionListener);

            var splitFactory2 = new SplitFactory(apikey, configurations);
            var client2 = splitFactory2.Client();
            client2.BlockUntilReady(10000);

            result = client2.GetTreatmentsByFlagSet("nico_test", "set_1");
            client2.Destroy();

            // Assert.
            Assert.AreEqual(1, result.Count);
            treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impressionExpected1);

            Assert.AreEqual(1, impressionListener.Count(), $"Redis: Impression Listener not match");
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
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

        protected static ConfigurationOptions GetClusterConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            var clusterNodes = new ClusterNodes(new List<string>() { Host + ":" + Port }, "{SPLITIO}");
            var cacheConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = clusterNodes,
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

        protected override void AssertSentImpressions(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            RedisHelper.AssertSentImpressions(_redisAdapter, UserPrefix, sentImpressionsCount, expectedImpressions);
        }

        protected override void AssertSentEvents(List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true)
        {
            RedisHelper.AssertSentEvents(_redisAdapter, UserPrefix, eventsExcpected, eventsCount, validateEvents);
        }
        #endregion
    }
}
