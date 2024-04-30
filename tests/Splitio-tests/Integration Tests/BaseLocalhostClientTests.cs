using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [DeploymentItem(@"Resources\test.splits")]
    [DeploymentItem(@"Resources\test1-polling.splits")]
    [DeploymentItem(@"Resources\test1-watcher.splits")]
    [DeploymentItem(@"Resources\test2-polling.splits")]
    [DeploymentItem(@"Resources\test2-watcher.splits")]
    [DeploymentItem(@"Resources\split.yaml")]
    [TestClass]
    public abstract class BaseLocalhostClientTests
    {
        private readonly string rootFilePath;
        private readonly string _mode;

        public BaseLocalhostClientTests(string mode)
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;
            _mode = mode;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public async Task GetTreatmentAsync()
        {
            // Arrange.
            var config = GetConfiguration($"{rootFilePath}test.splits");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            // Act.
            var result1 = await client.GetTreatmentAsync("id", "double_writes_to_cassandra");
            var result2 = await client.GetTreatmentAsync("id", "double_writes_to_cassandra");
            var result3 = await client.GetTreatmentAsync("id", "other_test_feature");
            var result4 = await client.GetTreatmentAsync("id", "other_test_feature");

            var result5 = await client.GetTreatmentsAsync("id", new List<string> { "other_test_feature", "double_writes_to_cassandra" });
            var result6 = await client.GetTreatmentsWithConfigAsync("id", new List<string> { "double_writes_to_cassandra" });
            var result7 = await client.GetTreatmentWithConfigAsync("id", "other_test_feature");

            // Assert.
            Assert.AreEqual("off", result1);
            Assert.AreEqual("off", result2);
            Assert.AreEqual("on", result3);
            Assert.AreEqual("on", result4);
            Assert.AreEqual("on", result7.Treatment);

            Assert.AreEqual(2, result5.Count);
            foreach (var item in result5)
            {
                Assert.AreNotEqual("control", item.Value);
            }

            Assert.AreEqual(1, result6.Count);
            foreach (var item in result6)
            {
                Assert.AreNotEqual("control", item.Value);
            }
        }

        [TestMethod]
        public void GetTreatmentSuccessfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}test.splits");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var result1 = client.GetTreatment("id", "double_writes_to_cassandra");
            var result2 = client.GetTreatment("id", "double_writes_to_cassandra");
            var result3 = client.GetTreatment("id", "other_test_feature");
            var result4 = client.GetTreatment("id", "other_test_feature");

            //Asert
            Assert.IsTrue(result1 == "off"); //default treatment
            Assert.IsTrue(result2 == "off"); //default treatment
            Assert.IsTrue(result3 == "on"); //default treatment
            Assert.IsTrue(result4 == "on"); //default treatment
        }

        [Ignore("Flaky Test")]
        [TestMethod]
        public void GetTreatmentSuccessfullyWhenUpdatingSplitsFile()
        {
            // Arrange
            var filePath = $"{rootFilePath}test2-{_mode}.splits";
            var config = GetConfiguration(filePath);
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            File.AppendAllText(filePath, Environment.NewLine + "other_test_feature2     off" + Environment.NewLine);
            Thread.Sleep(2000);

            // Act & Assert
            Assert.AreEqual("off", client.GetTreatment("id", "double_writes_to_cassandra"), $"1 - {_mode}");
            Assert.AreEqual("on", client.GetTreatment("id", "other_test_feature"), $"2 - {_mode}");
            Assert.AreEqual("off", client.GetTreatment("id", "other_test_feature2"), $"3 - {_mode}");

            using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.SetLength(0);
            }
            File.AppendAllText(filePath, Environment.NewLine);
            Thread.Sleep(2000);

            Assert.AreEqual("control", client.GetTreatment("id", "double_writes_to_cassandra"), $"4 - {_mode}");
            Assert.AreEqual("control", client.GetTreatment("id", "other_test_feature"), $"5 - {_mode}");
            Assert.AreEqual("control", client.GetTreatment("id", "other_test_feature2"), $"6 - {_mode}");

            File.AppendAllText(filePath, Environment.NewLine + "always_on on" + Environment.NewLine);
            Thread.Sleep(2000);

            Assert.AreEqual("on", client.GetTreatment("id", "always_on"), $"7 - {_mode}");
            Thread.Sleep(2000);
        }

        [TestMethod]
        public void GetTreatmentSuccessfullyWhenUpdatingSplitsFileSameFile()
        {
            // Arrange
            var filePath = $"{rootFilePath}test1-{_mode}.splits";
            using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.SetLength(0);
            }
            File.AppendAllText(filePath, Environment.NewLine);
            Thread.Sleep(1000);

            var config = GetConfiguration(filePath);
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            File.AppendAllText(filePath, Environment.NewLine + "other_test_feature2     off" + Environment.NewLine);
            Thread.Sleep(2000);

            // Act & Assert
            Assert.AreEqual("off", client.GetTreatment("id", "other_test_feature2"), $"1 - {_mode}"); //default treatment
            Assert.AreEqual("control", client.GetTreatment("id", "double_writes_to_cassandra"), $"2 - {_mode}");

            File.AppendAllText(filePath, Environment.NewLine + "always_on on" + Environment.NewLine);
            Thread.Sleep(2000);

            Assert.AreEqual("on", client.GetTreatment("id", "always_on"), $"3 - {_mode}");
            Thread.Sleep(2000);
        }

        [TestMethod]
        public void ClientDestroySuccessfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}test.splits");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            // Act & Assert
            Assert.AreEqual("off", client.GetTreatment("id", "double_writes_to_cassandra"));
            Assert.AreEqual("on", client.GetTreatment("id", "other_test_feature"));

            client.Destroy();

            var manager = client.GetSplitManager();
            Assert.AreEqual("control", client.GetTreatment("id", "double_writes_to_cassandra"));
            Assert.AreEqual(0, manager.Splits().Count);
            Assert.AreEqual(0, manager.SplitNames().Count);
            Assert.IsNull(manager.Split("double_writes_to_cassandra"));
        }

        [TestMethod]
        public void GetTreatment_WhenIsYamlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var result = client.GetTreatment("id", "testing_split_on");
            Assert.AreEqual("on", result);

            result = client.GetTreatment("key_for_wl", "testing_split_only_wl");
            Assert.AreEqual("whitelisted", result);

            result = client.GetTreatment("id", "testing_split_with_wl");
            Assert.AreEqual("not_in_whitelist", result);

            result = client.GetTreatment("key_for_wl", "testing_split_with_wl");
            Assert.AreEqual("one_key_wl", result);

            result = client.GetTreatment("key_for_wl_1", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result);

            result = client.GetTreatment("key_for_wl_2", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result);

            result = client.GetTreatment("key_for_wl_2", "testing_split_off_with_config");
            Assert.AreEqual("off", result);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenIsYamlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var result = client.GetTreatmentWithConfig("id", "testing_split_on");
            Assert.AreEqual("on", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl", "testing_split_only_wl");
            Assert.AreEqual("whitelisted", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("id", "testing_split_with_wl");
            Assert.AreEqual("not_in_whitelist", result.Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl", "testing_split_with_wl");
            Assert.AreEqual("one_key_wl", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_1", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result.Treatment);
            Assert.AreEqual("{\"color\": \"brown\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_2", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result.Treatment);
            Assert.AreEqual("{\"color\": \"brown\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_2", "testing_split_off_with_config");
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", result.Config);
        }

        [TestMethod]
        public void GetTreatment_WhenIsYmlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var result = client.GetTreatment("id", "testing_split_on");
            Assert.AreEqual("on", result);

            result = client.GetTreatment("key_for_wl", "testing_split_only_wl");
            Assert.AreEqual("whitelisted", result);

            result = client.GetTreatment("id", "testing_split_with_wl");
            Assert.AreEqual("not_in_whitelist", result);

            result = client.GetTreatment("key_for_wl", "testing_split_with_wl");
            Assert.AreEqual("one_key_wl", result);

            result = client.GetTreatment("key_for_wl_1", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result);

            result = client.GetTreatment("key_for_wl_2", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result);

            result = client.GetTreatment("key_for_wl_2", "testing_split_off_with_config");
            Assert.AreEqual("off", result);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenIsYmlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var result = client.GetTreatmentWithConfig("id", "testing_split_on");
            Assert.AreEqual("on", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl", "testing_split_only_wl");
            Assert.AreEqual("whitelisted", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("id", "testing_split_with_wl");
            Assert.AreEqual("not_in_whitelist", result.Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl", "testing_split_with_wl");
            Assert.AreEqual("one_key_wl", result.Treatment);
            Assert.IsNull(result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_1", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result.Treatment);
            Assert.AreEqual("{\"color\": \"brown\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_2", "testing_split_with_wl");
            Assert.AreEqual("multi_key_wl", result.Treatment);
            Assert.AreEqual("{\"color\": \"brown\"}", result.Config);

            result = client.GetTreatmentWithConfig("key_for_wl_2", "testing_split_off_with_config");
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", result.Config);
        }

        [TestMethod]
        public void GetTreatments_WhenIsYamlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var results = client.GetTreatments("id", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"]);
            Assert.AreEqual("control", results["testing_split_only_wl"]);
            Assert.AreEqual("not_in_whitelist", results["testing_split_with_wl"]);
            Assert.AreEqual("off", results["testing_split_off_with_config"]);

            results = client.GetTreatments("key_for_wl", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"]);
            Assert.AreEqual("whitelisted", results["testing_split_only_wl"]);
            Assert.AreEqual("one_key_wl", results["testing_split_with_wl"]);
            Assert.AreEqual("off", results["testing_split_off_with_config"]);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WhenIsYamlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var results = client.GetTreatmentsWithConfig("id", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("control", results["testing_split_only_wl"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("not_in_whitelist", results["testing_split_with_wl"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_with_wl"].Config);

            Assert.AreEqual("off", results["testing_split_off_with_config"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_off_with_config"].Config);

            results = client.GetTreatmentsWithConfig("key_for_wl", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("whitelisted", results["testing_split_only_wl"].Treatment);
            Assert.IsNull(results["testing_split_only_wl"].Config);

            Assert.AreEqual("one_key_wl", results["testing_split_with_wl"].Treatment);
            Assert.IsNull(results["testing_split_with_wl"].Config);

            Assert.AreEqual("off", results["testing_split_off_with_config"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_off_with_config"].Config);
        }

        [TestMethod]
        public void GetTreatments_WhenIsYmlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var results = client.GetTreatments("id", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"]);
            Assert.AreEqual("control", results["testing_split_only_wl"]);
            Assert.AreEqual("not_in_whitelist", results["testing_split_with_wl"]);
            Assert.AreEqual("off", results["testing_split_off_with_config"]);

            results = client.GetTreatments("key_for_wl", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"]);
            Assert.AreEqual("whitelisted", results["testing_split_only_wl"]);
            Assert.AreEqual("one_key_wl", results["testing_split_with_wl"]);
            Assert.AreEqual("off", results["testing_split_off_with_config"]);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WhenIsYmlFile_Successfully()
        {
            //Arrange
            var config = GetConfiguration($"{rootFilePath}split.yml");
            var client = new LocalhostClient(config);

            client.BlockUntilReady(1000);

            //Act
            var results = client.GetTreatmentsWithConfig("id", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("control", results["testing_split_only_wl"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("not_in_whitelist", results["testing_split_with_wl"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_with_wl"].Config);

            Assert.AreEqual("off", results["testing_split_off_with_config"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_off_with_config"].Config);

            results = client.GetTreatmentsWithConfig("key_for_wl", new List<string>
            {
                "testing_split_on",
                "testing_split_only_wl",
                "testing_split_with_wl",
                "testing_split_off_with_config"
            });

            Assert.AreEqual("on", results["testing_split_on"].Treatment);
            Assert.IsNull(results["testing_split_on"].Config);

            Assert.AreEqual("whitelisted", results["testing_split_only_wl"].Treatment);
            Assert.IsNull(results["testing_split_only_wl"].Config);

            Assert.AreEqual("one_key_wl", results["testing_split_with_wl"].Treatment);
            Assert.IsNull(results["testing_split_with_wl"].Config);

            Assert.AreEqual("off", results["testing_split_off_with_config"].Treatment);
            Assert.AreEqual("{\"color\": \"green\"}", results["testing_split_off_with_config"].Config);
        }

        protected abstract ConfigurationOptions GetConfiguration(string fileName);
    }
}
