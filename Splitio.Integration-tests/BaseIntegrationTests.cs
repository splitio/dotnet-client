﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Integration_tests
{
    [DeploymentItem(@"Resources\split_changes.json")]
    [DeploymentItem(@"Resources\split_changes_1.json")]
    [DeploymentItem(@"Resources\split_segment1.json")]
    [DeploymentItem(@"Resources\split_segment2.json")]
    [DeploymentItem(@"Resources\split_segment3.json")]
    [TestClass]
    public abstract class BaseIntegrationTests
    {
        protected static readonly HttpClientMock httpClientMock = new HttpClientMock("test");

        #region GetTreatmentAsync
        [TestMethod]
        public async Task GetTreatmentAsync_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result1 = await client.GetTreatmentAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentAsync("mauro_test", "FACUNDO_TEST");
            var result3 = await client.GetTreatmentAsync("1", "Test_Save_1");
            var result4 = await client.GetTreatmentAsync("24", "Test_Save_1");

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("off", result2);
            Assert.AreEqual("on", result3);
            Assert.AreEqual("off", result4);

            await client.DestroyAsync();

            // Validate impressions in listener.
            await Task.Delay(500);
            Assert.AreEqual(4, impressionListener.Count(), "Impression Listener not match.");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("FACUNDO_TEST", "mauro_test");
            var impression3 = impressionListener.Get("Test_Save_1", "1");
            var impression4 = impressionListener.Get("Test_Save_1", "24");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262916, "FACUNDO_TEST", "mauro_test", "in segment all", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "1", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "24", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatmentAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey2";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(5000);

            // Act.
            var result1 = await client.GetTreatmentAsync("nico_test", "FACUNDO_TEST");
            var result2 = await client.GetTreatmentAsync(string.Empty, "FACUNDO_TEST");
            var result3 = await client.GetTreatmentAsync("1", string.Empty);
            var result4 = await client.GetTreatmentAsync("24", "Test_Save_1");

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("control", result2);
            Assert.AreEqual("control", result3);
            Assert.AreEqual("off", result4);

            await client.DestroyAsync();

            // Validate impressions in listener.
            await Task.Delay(500);
            Assert.AreEqual(2, impressionListener.Count(), "Impression Listener not match.");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "24");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "24", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(2, httpClientMock, impression1, impression2);

            client.Destroy();
        }

        [TestMethod]
        public async Task GetTreatmentAsync_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

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
            await AssertSentImpressionsAsync(0, httpClientMock);

            await client.DestroyAsync();
        }
        #endregion 

        #region GetTreatment
        [TestMethod]
        public async Task GetTreatment_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatment("mauro_test", "FACUNDO_TEST");
            var result3 = client.GetTreatment("1", "Test_Save_1");
            var result4 = client.GetTreatment("24", "Test_Save_1");

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("off", result2);
            Assert.AreEqual("on", result3);
            Assert.AreEqual("off", result4);

            client.Destroy();

            // Validate impressions in listener.
            Assert.AreEqual(4, impressionListener.Count(), "Impression Listener not match");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("FACUNDO_TEST", "mauro_test");
            var impression3 = impressionListener.Get("Test_Save_1", "1");
            var impression4 = impressionListener.Get("Test_Save_1", "24");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262916, "FACUNDO_TEST", "mauro_test", "in segment all", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "1", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "24", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatment_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey2";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatment(string.Empty, "FACUNDO_TEST");
            var result3 = client.GetTreatment("1", string.Empty);
            var result4 = client.GetTreatment("24", "Test_Save_1");

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("control", result2);
            Assert.AreEqual("control", result3);
            Assert.AreEqual("off", result4);

            client.Destroy();

            // Validate impressions in listener.
            Assert.AreEqual(2, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "24");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "24", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(2, httpClientMock, impression1, impression2);

            client.Destroy();
        }

        [TestMethod]
        public async Task GetTreatment_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey3";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatment("nico_test", "Random_Treatment");

            // Assert.
            Assert.AreEqual("control", result);

            // Validate impressions in listener.
            Assert.AreEqual(0, impressionListener.Count());

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(0, httpClientMock);

            client.Destroy();
        }
        #endregion

        #region GetTreatmentWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

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

            // Validate impressions.
            Assert.AreEqual(4, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("FACUNDO_TEST", "mauro_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("MAURO_TEST", "test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262916, "FACUNDO_TEST", "mauro_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1506703262966, "MAURO_TEST", "test", "not in split", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

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

            // Validate impressions.
            Assert.AreEqual(2, impressionListener.Count(), "Impression Listener not Match.");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(2, httpClientMock, impression1, impression2);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfigAsync_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey6";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentWithConfigAsync("nico_test", "Random_Treatment");

            // Assert.
            Assert.AreEqual("control", result.Treatment);

            // Validate impressions in listener.
            Assert.AreEqual(0, impressionListener.Count(), "Impression Listener not Match.");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(0, httpClientMock);

            await client.DestroyAsync();
        }
        #endregion

        #region GetTreatmentWithConfig        
        [TestMethod]
        public async Task GetTreatmentWithConfig_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey4";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatmentWithConfig("mauro_test", "FACUNDO_TEST");
            var result3 = client.GetTreatmentWithConfig("mauro", "MAURO_TEST");
            var result4 = client.GetTreatmentWithConfig("test", "MAURO_TEST");

            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result1.Treatment);
            Assert.AreEqual("off", result2.Treatment);
            Assert.AreEqual("on", result3.Treatment);
            Assert.AreEqual("off", result4.Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result1.Config);
            Assert.IsNull(result2.Config);
            Assert.AreEqual("{\"version\":\"v2\"}", result3.Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result4.Config);

            // Validate impressions.
            Assert.AreEqual(4, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("FACUNDO_TEST", "mauro_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("MAURO_TEST", "test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262916, "FACUNDO_TEST", "mauro_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1506703262966, "MAURO_TEST", "test", "not in split", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey5";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatmentWithConfig("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatmentWithConfig(string.Empty, "FACUNDO_TEST");
            var result3 = client.GetTreatmentWithConfig("test", string.Empty);
            var result4 = client.GetTreatmentWithConfig("mauro", "MAURO_TEST");

            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result1.Treatment);
            Assert.AreEqual("control", result2.Treatment);
            Assert.AreEqual("control", result3.Treatment);
            Assert.AreEqual("on", result4.Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result1.Config);
            Assert.IsNull(result2.Config);
            Assert.IsNull(result3.Config);
            Assert.AreEqual("{\"version\":\"v2\"}", result4.Config);

            // Validate impressions.
            Assert.AreEqual(2, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(2, httpClientMock, impression1, impression2);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey6";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentWithConfig("nico_test", "Random_Treatment");

            // Assert.
            Assert.AreEqual("control", result.Treatment);

            // Validate impressions in listener.
            Assert.AreEqual(0, impressionListener.Count());

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(0, httpClientMock);

            client.Destroy();
        }
        #endregion

        #region GetTreatmentsAsync
        [TestMethod]
        public async Task GetTreatmentsAsync_WithtBUR_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey7";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsAsync("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);

            await client.DestroyAsync();

            // Validate impressions.
            await Task.Delay(500);
            Assert.AreEqual(3, impressionListener.Count(), "Impression Listener not Match");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }

        [TestMethod]
        public async Task GetTreatmentsAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey8";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = await client.GetTreatmentsAsync("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = await client.GetTreatmentsAsync("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = await client.GetTreatmentsAsync(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

            // Assert.
            Assert.AreEqual("on", result1["FACUNDO_TEST"]);
            Assert.AreEqual("off", result1["Test_Save_1"]);
            Assert.AreEqual("on", result2["MAURO_TEST"]);
            Assert.AreEqual("off", result2["Test_Save_1"]);
            Assert.AreEqual("control", result3["FACUNDO_TEST"]);
            Assert.AreEqual("control", result3["MAURO_TEST"]);
            Assert.AreEqual("control", result3["Test_Save_1"]);

            await client.DestroyAsync();

            // Validate impressions.
            await Task.Delay(500);
            Assert.AreEqual(4, impressionListener.Count(), "Impression Listener not match.");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "nico_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("Test_Save_1", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "mauro", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }
        #endregion

        #region GetTreatments
        [TestMethod]
        public async Task GetTreatments_WithtBUR_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey7";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);

            client.Destroy();

            // Validate impressions.
            Assert.AreEqual(3, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }

        [TestMethod]
        public async Task GetTreatments_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey8";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = client.GetTreatments("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = client.GetTreatments(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

            // Assert.
            Assert.AreEqual("on", result1["FACUNDO_TEST"]);
            Assert.AreEqual("off", result1["Test_Save_1"]);
            Assert.AreEqual("on", result2["MAURO_TEST"]);
            Assert.AreEqual("off", result2["Test_Save_1"]);
            Assert.AreEqual("control", result3["FACUNDO_TEST"]);
            Assert.AreEqual("control", result3["MAURO_TEST"]);
            Assert.AreEqual("control", result3["Test_Save_1"]);

            client.Destroy();

            // Validate impressions.
            Assert.AreEqual(4, impressionListener.Count(), "Impression Listener not Match.");

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "nico_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("Test_Save_1", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "mauro", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }
        #endregion

        #region GetTreatmentsWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtBUR_ReturnsTreatments()
        {
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

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

            // Validate impressions.
            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            Assert.AreEqual(3, impressionListener.Count());

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.           
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey11";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = await client.GetTreatmentsWithConfigAsync("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = await client.GetTreatmentsWithConfigAsync("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = await client.GetTreatmentsWithConfigAsync(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

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

            await client.DestroyAsync();

            // Validate impressions.
            Assert.AreEqual(4, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "nico_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("Test_Save_1", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "mauro", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfigAsync_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey12";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = await client.GetTreatmentsWithConfigAsync("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_1" });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);

            await client.DestroyAsync();

            // Validate impressions.
            await Task.Delay(500);
            Assert.AreEqual(3, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }
        #endregion

        #region GetTreatmentsWithConfig
        [TestMethod]
        public async Task GetTreatmentsWithConfig_WithtBUR_ReturnsTreatments()
        {
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey10";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);

            // Validate impressions.
            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            Assert.AreEqual(3, impressionListener.Count());

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfig_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.           
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey11";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = client.GetTreatmentsWithConfig("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = client.GetTreatmentsWithConfig(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });

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

            client.Destroy();

            // Validate impressions.
            Assert.AreEqual(4, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("Test_Save_1", "nico_test");
            var impression3 = impressionListener.Get("MAURO_TEST", "mauro");
            var impression4 = impressionListener.Get("Test_Save_1", "mauro");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");
            AssertImpression(impression3, 1506703262966, "MAURO_TEST", "mauro", "whitelisted", "on");
            AssertImpression(impression4, 1503956389520, "Test_Save_1", "mauro", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(4, httpClientMock, impression1, impression2, impression3, impression4);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfig_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), impressionListener: impressionListener);

            var apikey = "base-apikey12";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_1" });

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment_1"].Treatment);

            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);

            client.Destroy();

            // Validate impressions.
            Assert.AreEqual(3, impressionListener.Count());

            var impression1 = impressionListener.Get("FACUNDO_TEST", "nico_test");
            var impression2 = impressionListener.Get("MAURO_TEST", "nico_test");
            var impression3 = impressionListener.Get("Test_Save_1", "nico_test");

            AssertImpression(impression1, 1506703262916, "FACUNDO_TEST", "nico_test", "whitelisted", "on");
            AssertImpression(impression2, 1506703262966, "MAURO_TEST", "nico_test", "not in split", "off");
            AssertImpression(impression3, 1503956389520, "Test_Save_1", "nico_test", "in segment all", "off");

            //Validate impressions sent to the be.
            await AssertSentImpressionsAsync(3, httpClientMock, impression1, impression2, impression3);
        }
        #endregion

        #region Manager
        [TestMethod]
        public void Manager_SplitNames_ReturnsSplitNames()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

            var apikey = "base-apikey13";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            var manager = client.GetSplitManager();

            // Act.
            var result = manager.SplitNames();

            // Assert.
            Assert.AreEqual(30, result.Count);
            Assert.IsInstanceOfType(result, typeof(List<string>));

            client.Destroy();
        }

        [TestMethod]
        public void Manager_Splits_ReturnsSplitList()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

            var apikey = "base-apikey14";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(10000);

            // Act.
            var result = manager.Splits();

            // Assert.
            Assert.AreEqual(30, result.Count);
            Assert.IsInstanceOfType(result, typeof(List<SplitView>));

            splitFactory.Client().Destroy();
        }

        [TestMethod]
        public void Manager_Split_ReturnsSplit()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

            var splitName = "MAURO_TEST";
            var apikey = "base-apikey15";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(10000);

            // Act.
            var result = manager.Split(splitName);

            // Assert.
            Assert.IsNotNull(result);
            Assert.AreEqual(splitName, result.name);

            splitFactory.Client().Destroy();
        }

        [TestMethod]
        public void Manager_Split_WhenNameDoesntExist_ReturnsSplit()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

            var splitName = "Split_Name";
            var apikey = "base-apikey16";

            var splitFactory = new SplitFactory(apikey, configurations);
            var manager = splitFactory.Manager();

            manager.BlockUntilReady(10000);

            // Act.
            var result = manager.Split(splitName);

            // Assert.
            Assert.IsNull(result);

            splitFactory.Client().Destroy();
        }
        #endregion

        #region Async Manager
        [TestMethod]
        public async Task Manager_SplitNamesAsync_ReturnsSplitNames()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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

        #region Track
        [TestMethod]
        public async Task Track_WithValidData_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task Track_WithBUR_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task Track_WithInvalidData_ReturnsFalse()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

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
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task Track_WithLowQueue_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), eventsPushRate: 60, eventsQueueSize: 3);

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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            await AssertSentEventsAsync(events, httpClientMock, sleepTime: 1000, eventsCount: 3, validateEvents: false);
            client.Destroy();
        }
        #endregion

        #region TrackAsync
        [TestMethod]
        public async Task TrackAsync_WithValidData_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task TrackAsync_WithBUR_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task TrackAsync_WithInvalidData_ReturnsFalse()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

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
            await AssertSentEventsAsync(events, httpClientMock);
            client.Destroy();
        }

        [TestMethod]
        public async Task TrackAsync_WithLowQueue_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl(), eventsPushRate: 60, eventsQueueSize: 3);

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
            await AssertSentEventsAsync(events, httpClientMock, sleepTime: 1000, eventsCount: 3, validateEvents: false);
            client.Destroy();
        }
        #endregion

        #region Destroy
        [TestMethod]
        public void Destroy()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(httpClientMock?.GetUrl());

            var apikey = "base-apikey21";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            var manager = client.GetSplitManager();

            // Act.
            var treatmentResult = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var managerResult = manager.Split("MAURO_TEST");

            client.Destroy();

            var destroyResult = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var managerDestroyResult = manager.Split("MAURO_TEST");

            // Assert.
            Assert.AreEqual("on", treatmentResult);
            Assert.AreEqual("control", destroyResult);
            Assert.IsTrue(client.IsDestroyed());

            Assert.IsNotNull(managerResult);
            Assert.AreEqual("MAURO_TEST", managerResult.name);
            // TODO : Redis destroy doesn't work. Refactor this and uncomment assert
            //Assert.IsNull(managerDestroyResult);
        }
        #endregion

        #region Protected Methods
        protected static void AssertImpression(KeyImpression impression, long changeNumber, string feature, string keyName, string label, string treatment)
        {
            Assert.AreEqual(changeNumber, impression.changeNumber);
            Assert.AreEqual(feature, impression.feature);
            Assert.AreEqual(keyName, impression.keyName);
            Assert.AreEqual(label, impression.label);
            Assert.AreEqual(treatment, impression.treatment);
        }
        
        protected abstract ConfigurationOptions GetConfigurationOptions(string url = null, int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null);

        protected abstract Task AssertSentImpressionsAsync(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions);

        protected abstract Task AssertSentEventsAsync(List<EventBackend> eventsExcpected, HttpClientMock httpClientMock = null, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true);
        #endregion

        [TestCleanup]
        public void Cleanup()
        {
            httpClientMock.ResetLogEntries();
        }
    }
}