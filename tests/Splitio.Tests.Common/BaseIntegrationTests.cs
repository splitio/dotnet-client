using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public abstract class BaseIntegrationTests
    {
        private readonly string _mode;

        public BaseIntegrationTests(string mode)
        {
            _mode = mode;
        }

        #region GetTreatment
        [TestMethod]
        public void GetTreatment_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatment("mauro_test", "FACUNDO_TEST");
            var result3 = client.GetTreatment("1", "Test_Save_1");
            var result4 = client.GetTreatment("24", "Test_Save_1");
            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("off", result2);
            Assert.AreEqual("on", result3);
            Assert.AreEqual("off", result4);

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impressionExpected2 = GetImpressionExpected("FACUNDO_TEST", "mauro_test");
            var impressionExpected3 = GetImpressionExpected("Test_Save_1", "1");
            var impressionExpected4 = GetImpressionExpected("Test_Save_1", "24");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impressionExpected1, impressionExpected2, impressionExpected3, impressionExpected4);

            // Validate impressions in listener.
            AssertImpressionListener(4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "mauro_test"), impressionExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "1"), impressionExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "24"), impressionExpected4);
        }

        [TestMethod]
        public void GetTreatment_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey2";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatment("nico_test", "FACUNDO_TEST");
            var result2 = client.GetTreatment(string.Empty, "FACUNDO_TEST");
            var result3 = client.GetTreatment("1", string.Empty);
            var result4 = client.GetTreatment("24", "Test_Save_1");
            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result1);
            Assert.AreEqual("control", result2);
            Assert.AreEqual("control", result3);
            Assert.AreEqual("off", result4);

            var impressionExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impressionExpected2 = GetImpressionExpected("Test_Save_1", "24");

            //Validate impressions sent to the be.
            AssertSentImpressions(2, impressionExpected1, impressionExpected2);

            // Validate impressions in listener.
            AssertImpressionListener(2, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "24"), impressionExpected2);
        }

        [TestMethod]
        public void GetTreatment_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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
            AssertSentImpressions(0);

            client.Destroy();
        }
        #endregion

        #region GetTreatmentWithConfig        
        [TestMethod]
        public void GetTreatmentWithConfig_WithtBUR_WithMultipleCalls_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("FACUNDO_TEST", "mauro_test");
            var impExpected3 = GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = GetImpressionExpected("MAURO_TEST", "test");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            AssertImpressionListener(4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "mauro_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "test"), impExpected4);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("MAURO_TEST", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(2, impExpected1, impExpected2);

            // Validate impressions.
            AssertImpressionListener(2, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected2);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WithtBUR_WhenTreatmentDoesntExist_ReturnsControl()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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
            AssertSentImpressions(0);

            client.Destroy();
        }
        #endregion

        #region GetTreatments
        [TestMethod]
        public void GetTreatments_WithtBUR_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey7";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"]);
            Assert.AreEqual("off", result["MAURO_TEST"]);
            Assert.AreEqual("off", result["Test_Save_1"]);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);

            // Validate impressions.
            AssertImpressionListener(3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }

        [TestMethod]
        public void GetTreatments_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey8";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatments("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = client.GetTreatments("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = client.GetTreatments(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result1["FACUNDO_TEST"]);
            Assert.AreEqual("off", result1["Test_Save_1"]);
            Assert.AreEqual("on", result2["MAURO_TEST"]);
            Assert.AreEqual("off", result2["Test_Save_1"]);
            Assert.AreEqual("control", result3["FACUNDO_TEST"]);
            Assert.AreEqual("control", result3["MAURO_TEST"]);
            Assert.AreEqual("control", result3["Test_Save_1"]);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("Test_Save_1", "nico_test");
            var impExpected3 = GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = GetImpressionExpected("Test_Save_1", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            AssertImpressionListener(4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "mauro"), impExpected4);
        }
        #endregion

        #region GetTreatmentsWithConfig
        [TestMethod]
        public void GetTreatmentsWithConfig_WithtBUR_ReturnsTreatments()
        {
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

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

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);
            
            // Validate impressions.
            AssertImpressionListener(3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WithtInputValidation_ReturnsTreatments()
        {
            // Arrange.           
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey11";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result1 = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", string.Empty, "Test_Save_1" });
            var result2 = client.GetTreatmentsWithConfig("mauro", new List<string> { string.Empty, "MAURO_TEST", "Test_Save_1" });
            var result3 = client.GetTreatmentsWithConfig(string.Empty, new List<string> { "FACUNDO_TEST", "MAURO_TEST", "Test_Save_1" });
            client.Destroy();

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

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("Test_Save_1", "nico_test");
            var impExpected3 = GetImpressionExpected("MAURO_TEST", "mauro");
            var impExpected4 = GetImpressionExpected("Test_Save_1", "mauro");

            //Validate impressions sent to the be.
            AssertSentImpressions(4, impExpected1, impExpected2, impExpected3, impExpected4);

            // Validate impressions.
            AssertImpressionListener(4, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "mauro"), impExpected3);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "mauro"), impExpected4);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WithtBUR_WhenTreatmentsDoesntExist_ReturnsTreatments()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "base-apikey12";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfig("nico_test", new List<string> { "FACUNDO_TEST", "Random_Treatment", "MAURO_TEST", "Test_Save_1", "Random_Treatment_1" });
            client.Destroy();

            // Assert.
            Assert.AreEqual("on", result["FACUNDO_TEST"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment"].Treatment);
            Assert.AreEqual("off", result["MAURO_TEST"].Treatment);
            Assert.AreEqual("off", result["Test_Save_1"].Treatment);
            Assert.AreEqual("control", result["Random_Treatment_1"].Treatment);
            Assert.AreEqual("{\"color\":\"green\"}", result["FACUNDO_TEST"].Config);
            Assert.AreEqual("{\"version\":\"v1\"}", result["MAURO_TEST"].Config);
            Assert.IsNull(result["Test_Save_1"].Config);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");
            var impExpected2 = GetImpressionExpected("MAURO_TEST", "nico_test");
            var impExpected3 = GetImpressionExpected("Test_Save_1", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(3, impExpected1, impExpected2, impExpected3);

            // Validate impressions.
            AssertImpressionListener(3, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
            Helper.AssertImpression(impressionListener.Get("MAURO_TEST", "nico_test"), impExpected2);
            Helper.AssertImpression(impressionListener.Get("Test_Save_1", "nico_test"), impExpected3);
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSets
        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSets_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            client.Destroy();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);
            Assert.AreEqual("{\"color\":\"green\"}", treatment.Value.Config);

            //Validate impressions sent to the be.
            var impressionExpected = new KeyImpression("nico_test", "FACUNDO_TEST", "on", 0, 1506703262916, "whitelisted", null, null, false);
            
            AssertSentImpressions(1, impressionExpected);
            AssertImpressionListener(1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impressionExpected);
        }

        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSets_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSets("key", null);
            var result2 = client.GetTreatmentsWithConfigByFlagSets("key", new List<string> { string.Empty, null });
            client.Destroy();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsByFlagSets
        [TestMethod]
        public void GetTreatmentsByFlagSets_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSets("nico_test", new List<string> { "set_1", "set_2", "set_3", string.Empty, null });
            client.Destroy();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);
            AssertImpressionListener(1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public void GetTreatmentsByFlagSets_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSets1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSets("key", null);
            var result2 = client.GetTreatmentsByFlagSets("key", new List<string> { string.Empty, null });
            client.Destroy();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsWithConfigByFlagSet
        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSet_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSet1";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSet("nico_test", "set_1");
            client.Destroy();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value.Treatment);
            Assert.AreEqual("{\"color\":\"green\"}", treatment.Value.Config);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);
            AssertImpressionListener(1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public void GetTreatmentsWithConfigByFlagSet_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsWithConfigByFlagSet";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsWithConfigByFlagSet("key", null);
            var result2 = client.GetTreatmentsWithConfigByFlagSet("key", string.Empty);
            client.Destroy();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region GetTreatmentsByFlagSet
        [TestMethod]
        public void GetTreatmentsByFlagSet_WithoutFlagSetsInConfig()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSet";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSet("nico_test", "set_1");
            client.Destroy();

            // Assert.
            var treatment = result.FirstOrDefault();
            Assert.AreEqual("FACUNDO_TEST", treatment.Key);
            Assert.AreEqual("on", treatment.Value);

            var impExpected1 = GetImpressionExpected("FACUNDO_TEST", "nico_test");

            //Validate impressions sent to the be.
            AssertSentImpressions(1, impExpected1);
            AssertImpressionListener(1, impressionListener);
            Helper.AssertImpression(impressionListener.Get("FACUNDO_TEST", "nico_test"), impExpected1);
        }

        [TestMethod]
        public void GetTreatmentsByFlagSet_WithWrongFlagSets()
        {
            // Arrange.
            var impressionListener = new IntegrationTestsImpressionListener(50);
            var configurations = GetConfigurationOptions(impressionListener: impressionListener);

            var apikey = "GetTreatmentsByFlagSet";

            var splitFactory = new SplitFactory(apikey, configurations);
            var client = splitFactory.Client();

            client.BlockUntilReady(10000);

            // Act.
            var result = client.GetTreatmentsByFlagSet("key", null);
            var result2 = client.GetTreatmentsByFlagSet("key", string.Empty);
            client.Destroy();

            // Assert.
            Assert.IsFalse(result.Any());
            Assert.IsFalse(result2.Any());
            Assert.AreEqual(0, impressionListener.Count(), $"{_mode}: Impression Listener not match");
        }
        #endregion

        #region Manager
        [TestMethod]
        public void Manager_SplitNames_ReturnsSplitNames()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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
            var configurations = GetConfigurationOptions();

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
            var configurations = GetConfigurationOptions();

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
            var configurations = GetConfigurationOptions();

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

        #region Track
        [TestMethod]
        public void Track_WithValidData_ReturnsTrue()
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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            AssertSentEvents(events);
            client.Destroy();
        }

        [TestMethod]
        public void Track_WithBUR_ReturnsTrue()
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
                var result = client.Track(_event.Key, _event.TrafficTypeName, _event.EventTypeId, _event.Value, _event.Properties);

                // Assert. 
                Assert.IsTrue(result);
            }

            //Validate Events sent to the be.
            AssertSentEvents(events);
            client.Destroy();
        }

        [TestMethod]
        public void Track_WithInvalidData_ReturnsFalse()
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
            AssertSentEvents(events);
            client.Destroy();
        }

        [TestMethod]
        public void Track_WithLowQueue_ReturnsTrue()
        {
            // Arrange.
            var configurations = GetConfigurationOptions(eventsPushRate: 60, eventsQueueSize: 3);

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
            AssertSentEvents(events, eventsCount: 3, validateEvents: false);
            client.Destroy();
        }
        #endregion

        #region Destroy
        [TestMethod]
        public void Destroy()
        {
            // Arrange.
            var configurations = GetConfigurationOptions();

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
        protected abstract ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null);
        protected abstract void AssertSentImpressions(int sentImpressionsCount, params KeyImpression[] expectedImpressions);
        protected abstract void AssertSentEvents(List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true);
        
        protected virtual void AssertImpressionListener(int expected, IntegrationTestsImpressionListener impressionListener)
        {
            Helper.AssertImpressionListener(_mode, expected, impressionListener);
        }
        protected KeyImpression GetImpressionExpected(string featureName, string key)
        {
            return Helper.GetImpressionExpected(featureName, key);
        }
        #endregion
    }
}