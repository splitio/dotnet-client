﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Tests.Common
{
    [DeploymentItem(@"Resources\split_changes.json")]
    [DeploymentItem(@"Resources\split_changes_1.json")]
    [DeploymentItem(@"Resources\split_segment1.json")]
    [DeploymentItem(@"Resources\split_segment2.json")]
    [DeploymentItem(@"Resources\split_segment3.json")]
    [TestClass]
    public abstract class BaseAsyncClientTests
    {
        private readonly string _mode;

        public BaseAsyncClientTests(string mode)
        {
            _mode = mode;
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await CleanupAsync();
        }

        #region GetTreatmentAsync
        [TestMethod]
        public async Task GetTreatmentAsync_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result1 = await client.GetTreatmentAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentAsync("mauro_test", "FACUNDO_TEST");
            var result3 = await client.GetTreatmentAsync("1", "Test_Save_1");
            var result4 = await client.GetTreatmentAsync("24", "Test_Save_1");
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("off", result2);
            Assert.AreEqual("on", result3);
            Assert.AreEqual("off", result4);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("FACUNDO_TEST", "mauro_test");
            var impExpected3 = Helper.GetImpressionExpected("Test_Save_1", "1");
            var impExpected4 = Helper.GetImpressionExpected("Test_Save_1", "24");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions in listener.
            Helper.AssertImpressionListener(_mode, 4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "mauro_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "1"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "24"), impExpected4);
        }

        [TestMethod]
        public async Task GetTreatmentAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey2";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result1 = await client.GetTreatmentAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentAsync(string.Empty, "FACUNDO_TEST");
            var result3 = await client.GetTreatmentAsync("1", string.Empty);
            var result4 = await client.GetTreatmentAsync("24", "Test_Save_1");
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("control", result2);
            Assert.AreEqual("control", result3);
            Assert.AreEqual("off", result4);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("Test_Save_1", "24");

            //Validate impressions sent to the be.
            AssertSentImpressions(2, impExpected1, impExpected2);

            // Validate impressions in listener.
            Helper.AssertImpressionListener(_mode, 2, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "24"), impExpected2);
        }

        [TestMethod]
        public async Task GetTreatmentAsync_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey3";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result = await client.GetTreatmentAsync("nico_test", "Random_Treatment");

            // Assert.
            Assert.AreEqual("control", result);

            // Validate impressions in listener.
            Assert.AreEqual(0, impressionListener.Count());

            //Validate impressions sent to the be.
            AssertSentImpressions(0);

            await client.DestroyAsync();
        }
        #endregion

        #region GetTreatmentWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey4";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result1 = await client.GetTreatmentWithConfigAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentWithConfigAsync("mauro_test", "FACUNDO_TEST");
            var result3 = await client.GetTreatmentWithConfigAsync("mauro", "MAURO_TEST");
            var result4 = await client.GetTreatmentWithConfigAsync("test", "MAURO_TEST");
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1.Treatment);
            Assert.AreEqual("off", result2.Treatment);
            Assert.AreEqual("on", result3.Treatment);
            Assert.AreEqual("off", result4.Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result1.Config);
            Assert.IsNull(result2.Config);
            Assert.AreEqual("{\"version\":\"v2\"}", result3.Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result4.Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("FACUNDO_TEST", "mauro_test");
            var impExpected3 = Helper.GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = Helper.GetImpressionExpected("MAURO_TEST", "test");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "mauro_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "test"), impExpected4);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey5";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = await client.GetTreatmentWithConfigAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentWithConfigAsync(string.Empty, "FACUNDO_TEST");
            var result3 = await client.GetTreatmentWithConfigAsync("test", string.Empty);
            var result4 = await client.GetTreatmentWithConfigAsync("mauro", "MAURO_TEST");
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1.Treatment);
            Assert.AreEqual("control", result2.Treatment);
            Assert.AreEqual("control", result3.Treatment);
            Assert.AreEqual("on", result4.Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result1.Config);
            Assert.IsNull(result2.Config);
            Assert.IsNull(result3.Config);
            Assert.AreEqual("{\"version\":\"v2\"}", result4.Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("MAURO_TEST", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(2, impExpected1, impExpected2);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 2, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected2);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey6";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentWithConfigAsync("nico_test", "Random_Treatment");

            // Assert.
            Assert.AreEqual("control", result.Treatment);

            // Validate impressions in listener.
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match.");

            //Validate impressions sent to the be.
            AssertSentImpressions(0);

            await client.DestroyAsync();
        }
        #endregion

        #region GetTreatmentsAsync
        [TestMethod]
        public async Task GetTreatmentsAsync_WithtBUR_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey7";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsAsync("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = Helper.GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"),impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }

        [TestMethod]
        public async Task GetTreatmentsAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey8";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = await client.GetTreatmentsAsync("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = await client.GetTreatmentsAsync("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = await client.GetTreatmentsAsync(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1["FACUNDO_TEST"]);
            Assert.AreEqual("off", result1["Test_Save_1"]);
            Assert.AreEqual("on", result2["MAURO_TEST"]);
            Assert.AreEqual("off", result2["Test_Save_1"]);
            Assert.AreEqual("control", result3["FACUNDO_TEST"]);
            Assert.AreEqual("control", result3["MAURO_TEST"]);
            Assert.AreEqual("control", result3["Test_Save_1"]);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("Test_Save_1", "nico_test");
            var impExpected3 = Helper.GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = Helper.GetImpressionExpected("Test_Save_1", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "mauro"), impExpected4);
        }
        #endregion

        #region GetTreatmentsWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtBUR_ReturnsTreatments()
        {
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey10";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigAsync("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            await client.DestroyAsync();
            
            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = Helper.GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.           
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey11";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = await client.GetTreatmentsWithConfigAsync("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = await client.GetTreatmentsWithConfigAsync("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = await client.GetTreatmentsWithConfigAsync(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result1["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("off", result1["Test_Save_1"].Treatment);
            Assert.AreEqual("on", result2["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result2["Test_Save_1"].Treatment);
            Assert.AreEqual("control", result3["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("control", result3["MAURO_TEST"].Treatment);
            Assert.AreEqual("control", result3["Test_Save_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result1["FACUNDO_TEST"].Config);
            Assert.IsNull(result1["Test_Save_1"].Config);
            Assert.AreEqual("{\"version\":\"v2\"}", result2["MAURO_TEST"].Config);
            Assert.IsNull(result2["Test_Save_1"].Config);
            Assert.IsNull(result3["FACUNDO_TEST"].Config);
            Assert.IsNull(result3["MAURO_TEST"].Config);
            Assert.IsNull(result3["Test_Save_1"].Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("Test_Save_1", "nico_test");
            var impExpected3 = Helper.GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = Helper.GetImpressionExpected("Test_Save_1", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "mauro"), impExpected4);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey12";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigAsync("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_1" });
            await client.DestroyAsync();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);
            
            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = Helper.GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = Helper.GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);

            // Validate impressions.
            Helper.AssertImpressionListener(_mode, 3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }
        #endregion

        #region Async Manager
        [TestMethod]
        public async Task Manager_SplitNamesAsync_ReturnsSplitNames()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "base-apikey13";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            var manager = client.GetSplitManager();

            // Act.
            var result = await manager.SplitNamesAsync();

            // Assert.
            Assert.AreEqual(30, result.Count);
            Assert.IsInstanceOfType(result, typeof(List<string>));

            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task Manager_SplitsAsync_ReturnsSplitList()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var apikey = "base-apikey14";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(5000);

            // Act.
            var result = await manager.SplitsAsync();

            // Assert.
            Assert.AreEqual(30, result.Count);
            Assert.IsInstanceOfType(result, typeof(List<SplitView>));

            await splitFactory.Client().DestroyAsync();
        }

        [TestMethod]
        public async Task Manager_SplitAsync_ReturnsSplit()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var splitName = "MAURO_TEST";
            var apikey = "base-apikey15";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(5000);

            // Act.
            var result = await manager.SplitAsync(splitName);

            // Assert.
            Assert.IsNotNull(result);
            Assert.AreEqual(splitName, result.name);

            await splitFactory.Client().DestroyAsync();
        }

        [TestMethod]
        public async Task Manager_SplitAsync_WhenNameDoesntExist_ReturnsSplit()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var splitName = "Split_Name";
            var apikey = "base-apikey16";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(5000);

            // Act.
            var result = await manager.SplitAsync(splitName);

            // Assert.
            Assert.IsNull(result);

            await splitFactory.Client().DestroyAsync();
        }
        #endregion

        #region TrackAsync
        [TestMethod]
        public async Task TrackAsync_WithValidData_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var properties = new Dictionary<string, object>
            {
                { "property_1",  1 },
                { "property_2",  2 }
            };

            var events = new List<EventBackend>
            {
                new EventBackend { Key = "key_1", TrafficTypeName = "traffic_type_1", EventTypeId = "event_type_1", Value = 123, Properties = properties },
                new EventBackend { Key = "key_2", TrafficTypeName = "traffic_type_2", EventTypeId = "event_type_2", Value = 222 },
                new EventBackend { Key = "key_3", TrafficTypeName = "traffic_type_3", EventTypeId = "event_type_3", Value = 333 },
                new EventBackend { Key = "key_4", TrafficTypeName = "traffic_type_4", EventTypeId = "event_type_4", Value = 444, Properties = properties }
            };

            var apikey = "base-apikey17";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            foreach (var _event in events)
            {
                // Act.
                var result = await client.TrackAsync(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            AssertSentEvents(events);
            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task TrackAsync_WithBUR_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var properties = new Dictionary<string, object>
            {
                { "property_1",  1 },
                { "property_2",  2 }
            };

            var events = new List<EventBackend>
            {
                new EventBackend { Key = "key_1", TrafficTypeName = "traffic_type_1", EventTypeId = "event_type_1", Value = 123, Properties = properties },
                new EventBackend { Key = "key_2", TrafficTypeName = "traffic_type_2", EventTypeId = "event_type_2", Value = 222 },
                new EventBackend { Key = "key_3", TrafficTypeName = "traffic_type_3", EventTypeId = "event_type_3", Value = 333 },
                new EventBackend { Key = "key_4", TrafficTypeName = "traffic_type_4", EventTypeId = "event_type_4", Value = 444, Properties = properties }
            };

            var apikey = "base-apikey18";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            foreach (var _event in events)
            {
                // Act.
                var result = await client.TrackAsync(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            AssertSentEvents(events);
            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task TrackAsync_WithInvalidData_ReturnsFalse()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

            var properties = new Dictionary<string, object>
            {
                { "property_1",  1 },
                { "property_2",  2 }
            };

            var events = new List<EventBackend>
            {
                new EventBackend { Key = string.Empty, TrafficTypeName = "traffic_type_1", EventTypeId = "event_type_1", Value = 123, Properties = properties },
                new EventBackend { Key = "key_2", TrafficTypeName = string.Empty, EventTypeId = "event_type_2", Value = 222 },
                new EventBackend { Key = "key_3", TrafficTypeName = "traffic_type_3", EventTypeId = string.Empty, Value = 333 },
                new EventBackend { Key = "key_4", TrafficTypeName = "traffic_type_4", EventTypeId = "event_type_4", Value = 444, Properties = properties },
                new EventBackend { Key = "key_5", TrafficTypeName = "traffic_type_5", EventTypeId = "event_type_5"}
            };

            var apikey = "base-apikey19";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            foreach (var _event in events)
            {
                // Act.
                var result = await client.TrackAsync(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                if (string.IsNullOrEmpty(_event.Key) || _event.Key.Equals("key_2") || _event.Key.Equals("key_3"))
                    Assert.IsFalse(result);
                else
                    Assert.IsTrue(result);
            }

            events = events
                .Where(e => e.Key.Equals("key_4") || e.Key.Equals("key_5"))
                .ToList();

            //Validate Events sent to the be.
            AssertSentEvents(events);
            await client.DestroyAsync();
        }

        [TestMethod]
        public async Task TrackAsync_WithLowQueue_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();
            configurations.EventsPushRate = 60;
            configurations.EventsQueueSize = 3;

            var properties = new Dictionary<string, object>
            {
                { "property_1",  1 },
                { "property_2",  2 }
            };

            var events = new List<EventBackend>
            {
                new EventBackend { Key = "key_1", TrafficTypeName = "traffic_type_1", EventTypeId = "event_type_1", Value = 123, Properties = properties },
                new EventBackend { Key = "key_2", TrafficTypeName = "traffic_type_2", EventTypeId = "event_type_2", Value = 222 },
                new EventBackend { Key = "key_3", TrafficTypeName = "traffic_type_3", EventTypeId = "event_type_3", Value = 333 },
                new EventBackend { Key = "key_4", TrafficTypeName = "traffic_type_4", EventTypeId = "event_type_4", Value = 444, Properties = properties },
                new EventBackend { Key = "key_5", TrafficTypeName = "traffic_type_5", EventTypeId = "event_type_5"}
            };

            var apikey = "base-apikey20";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            foreach (var _event in events)
            {
                // Act.
                var result = await client.TrackAsync(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            await Task.Delay(1000);
            AssertSentEvents(events, eventsCount: 3, validateEvents: false);
            await client.DestroyAsync();
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSetsAsync
        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSets_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigByFlagSetsAsync("nico_test", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);
            Assert.AreEqual("{\"color\":\"green\"}", treatment.Value.Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");
            
            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);

            Helper.AssertImpressionListener(_mode, 1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSets_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigByFlagSetsAsync("key", null);
            var result2 = await client.GetTreatmentsWithConfigByFlagSetsAsync("key", new List<string> { string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsByFlagSetsAsync
        [TestMethod]
        public async Task GetTreatmentsByFlagSetsAsync_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSetsAsync1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsByFlagSetsAsync("nico_test", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);

            Helper.AssertImpressionListener(_mode, 1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public async Task GetTreatmentsByFlagSetsAsync_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSetsAsync1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsByFlagSetsAsync("key", null);
            var result2 = await client.GetTreatmentsByFlagSetsAsync("key", new List<string> { string.Empty, null });
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSetAsync
        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSetAsync_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSetAsync";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigByFlagSetAsync("nico_test", "set_1");
            await client.DestroyAsync();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);
            Assert.AreEqual("{\"color\":\"green\"}", treatment.Value.Config);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);

            Helper.AssertImpressionListener(_mode, 1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigByFlagSetAsync_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSetAsync";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigByFlagSetAsync("key", null);
            var result2 = await client.GetTreatmentsWithConfigByFlagSetAsync("key", string.Empty);
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsByFlagSet
        [TestMethod]
        public async Task GetTreatmentsByFlagSetAsync_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSetAsync";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsByFlagSetAsync("nico_test", "set_1");
            await client.DestroyAsync();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            var impExpected1 = Helper.GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);
            Helper.AssertImpressionListener(_mode, 1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public async Task GetTreatmentsByFlagSetAsync_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSetAsync";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsByFlagSetAsync("key", null);
            var result2 = await client.GetTreatmentsByFlagSetAsync("key", string.Empty);
            await client.DestroyAsync();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region Protected Methods
        protected abstract ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null);
        protected abstract void AssertSentImpressions(int sentImpressionsCount, params KeyImpression[] expectedImpressions);
        protected abstract void AssertSentEvents(List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true);
        protected abstract Task CleanupAsync();
        #endregion
    }
}