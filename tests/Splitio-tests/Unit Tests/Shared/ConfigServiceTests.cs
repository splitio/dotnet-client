using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class ConfigServiceTests
    {
        private readonly Mock<IWrapperAdapter> _wrapperAdapter;
        private readonly IFlagSetsValidator _flagSetsValidator;

        private readonly IConfigService _configService;

        public ConfigServiceTests()
        {
            _wrapperAdapter = new Mock<IWrapperAdapter>();
            _flagSetsValidator = new FlagSetsValidator();

            _configService = new ConfigService(_wrapperAdapter.Object, _flagSetsValidator, new SdkMetadataValidator());
        }

        [TestMethod]
        public void GetInMemoryDefatulConfig()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(new ConfigurationOptions(), ConfigTypes.InMemory);

            // Assert.
            Assert.AreEqual(true, result.LabelsEnabled);
            Assert.AreEqual("https://sdk.split.io", result.BaseUrl);
            Assert.AreEqual("https://events.split.io", result.EventsBaseUrl);
            Assert.AreEqual(60, result.SplitsRefreshRate);
            Assert.AreEqual(60, result.SegmentRefreshRate);
            Assert.AreEqual(15000, result.HttpConnectionTimeout);
            Assert.AreEqual(15000, result.HttpReadTimeout);
            Assert.AreEqual(false, result.RandomizeRefreshRates);
            Assert.AreEqual(300, result.TreatmentLogRefreshRate);
            Assert.AreEqual(30000, result.TreatmentLogSize);
            Assert.AreEqual(5000, result.ImpressionsBulkSize);
            Assert.AreEqual(60, result.EventLogRefreshRate);
            Assert.AreEqual(10000, result.EventLogSize);
            Assert.AreEqual(500, result.EventsBulkSize);
            Assert.AreEqual(10, result.EventsFirstPushWindow);
            Assert.AreEqual(5, result.NumberOfParalellSegmentTasks);
            Assert.AreEqual(true, result.StreamingEnabled);
            Assert.AreEqual(1, result.AuthRetryBackoffBase);
            Assert.AreEqual(1, result.StreamingReconnectBackoffBase);
            Assert.AreEqual("https://auth.split.io/api/v2/auth", result.AuthServiceURL);
            Assert.AreEqual("https://streaming.split.io/sse", result.StreamingServiceURL);
            Assert.AreEqual(ImpressionsMode.Optimized, result.ImpressionsMode);
            Assert.AreEqual("ip-test", result.SdkMachineIP);
            Assert.AreEqual("name-test", result.SdkMachineName);
            Assert.AreEqual("version-test", result.SdkVersion);
            Assert.AreEqual(true, result.LabelsEnabled);
            Assert.IsFalse(result.FlagSetsFilter.Any());
            Assert.AreEqual(0, result.FlagSetsInvalid);
        }

        [TestMethod]
        public void GetInMemoryCustomConfig()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions
            {
                ImpressionsMode = ImpressionsMode.Debug,
                FeaturesRefreshRate = 100,
                ImpressionsRefreshRate = 150,
                SegmentsRefreshRate = 80,
                StreamingEnabled = false,
                FlagSetsFilter = new List<string> { "set1", "set_2", "set-3", "ASDASDASD", "ajdlaisdiaposidopasiopdipaosidoasidpoaisdpoaispodiaspodiaspd", null, string.Empty }
            };

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);

            // Assert.
            Assert.AreEqual(true, result.LabelsEnabled);
            Assert.AreEqual("https://sdk.split.io", result.BaseUrl);
            Assert.AreEqual("https://events.split.io", result.EventsBaseUrl);
            Assert.AreEqual(100, result.SplitsRefreshRate);
            Assert.AreEqual(80, result.SegmentRefreshRate);
            Assert.AreEqual(15000, result.HttpConnectionTimeout);
            Assert.AreEqual(15000, result.HttpReadTimeout);
            Assert.AreEqual(false, result.RandomizeRefreshRates);
            Assert.AreEqual(150, result.TreatmentLogRefreshRate);
            Assert.AreEqual(30000, result.TreatmentLogSize);
            Assert.AreEqual(5000, result.ImpressionsBulkSize);
            Assert.AreEqual(60, result.EventLogRefreshRate);
            Assert.AreEqual(10000, result.EventLogSize);
            Assert.AreEqual(500, result.EventsBulkSize);
            Assert.AreEqual(10, result.EventsFirstPushWindow);
            Assert.AreEqual(5, result.NumberOfParalellSegmentTasks);
            Assert.AreEqual(false, result.StreamingEnabled);
            Assert.AreEqual(1, result.AuthRetryBackoffBase);
            Assert.AreEqual(1, result.StreamingReconnectBackoffBase);
            Assert.AreEqual("https://auth.split.io/api/v2/auth", result.AuthServiceURL);
            Assert.AreEqual("https://streaming.split.io/sse", result.StreamingServiceURL);
            Assert.AreEqual(ImpressionsMode.Debug, result.ImpressionsMode);
            Assert.AreEqual("ip-test", result.SdkMachineIP);
            Assert.AreEqual("name-test", result.SdkMachineName);
            Assert.AreEqual("version-test", result.SdkVersion);
            Assert.AreEqual(true, result.LabelsEnabled);
            Assert.AreEqual(3, result.FlagSetsFilter.Count);
            Assert.IsTrue(result.FlagSetsFilter.Contains("set1"));
            Assert.IsTrue(result.FlagSetsFilter.Contains("set_2"));
            Assert.AreEqual(4, result.FlagSetsInvalid);
        }

        [TestMethod]
        public void GetRedisDefaultConfig()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            // Act.
            var result = _configService.ReadConfig(new ConfigurationOptions(), ConfigTypes.Redis);

            // Assert
            Assert.AreEqual("ip-test", result.SdkMachineIP);
            Assert.AreEqual("name-test", result.SdkMachineName);
            Assert.AreEqual("version-test", result.SdkVersion);
            Assert.AreEqual(true, result.LabelsEnabled);
        }

        [TestMethod]
        public void GetConfigWithOptimizedImp()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions
            {
                ImpressionsMode = ImpressionsMode.Optimized,
            };

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);

            // Assert.
            Assert.AreEqual(300, result.TreatmentLogRefreshRate);

            // Should return 60 because is the min allowed.
            config.ImpressionsRefreshRate = 30;
            result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);
            Assert.AreEqual(60, result.TreatmentLogRefreshRate);

            // Should return custom value.
            config.ImpressionsRefreshRate = 120;
            result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);
            Assert.AreEqual(120, result.TreatmentLogRefreshRate);
        }

        [TestMethod]
        public void GetConfigWithDebugImp()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions
            {
                ImpressionsMode = ImpressionsMode.Debug,
            };

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);

            // Assert.
            Assert.AreEqual(60, result.TreatmentLogRefreshRate);

            // Should return 60 because is the min allowed.
            config.ImpressionsRefreshRate = 30;
            result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);
            Assert.AreEqual(30, result.TreatmentLogRefreshRate);

            // Should return custom value.
            config.ImpressionsRefreshRate = 120;
            result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);
            Assert.AreEqual(120, result.TreatmentLogRefreshRate);
        }

        [TestMethod]
        public void GetInMemoryConfigWithFlagSetsNull()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions();

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);

            // Assert.
            Assert.IsFalse(result.FlagSetsFilter.Any());
        }

        [TestMethod]
        public void GetInMemoryConfigWithFlagSetsWithValue()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions
            {
                FlagSetsFilter = new List<string> { "Hola", "hola", "@@@" }
            };

            // Act.
            var result = (SelfRefreshingConfig)_configService.ReadConfig(config, ConfigTypes.InMemory);

            // Assert.
            Assert.AreEqual(1, result.FlagSetsFilter.Count);
        }

        [TestMethod]
        public void GetRedisConfigWithFlagSetsWithValue()
        {
            // Arrange.
            _wrapperAdapter
                .Setup(mock => mock.BuildSdkMetadata(It.IsAny<ConfigurationOptions>(), It.IsAny<ISplitLogger>()))
                .Returns(new SdkMetadata
                {
                    MachineIP = "ip-test",
                    MachineName = "name-test",
                    Version = "version-test",
                });

            var config = new ConfigurationOptions
            {
                FlagSetsFilter = new List<string> { "Hola", "hola", "@@@" }
            };

            // Act.
            var result = _configService.ReadConfig(config, ConfigTypes.Redis);

            // Assert.
            Assert.AreEqual(0, result.FlagSetsFilter.Count);
            Assert.AreEqual(0, result.FlagSetsInvalid);
        }
    }
}
