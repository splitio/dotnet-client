using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Client.Classes;
using Splitio.Commons.Dto;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Shared.Logger;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class WrapperAdapterTests
    {
        private readonly Mock<ISplitLogger> _log;
        private readonly IWrapperAdapter _adapter;

        public WrapperAdapterTests()
        {
            _log = new Mock<ISplitLogger>();
            _adapter = WrapperAdapter.Instance();
        }

        [TestMethod]
        public void ReadConfigReturnsMachineName()
        {
            // Act.
            var config = new ConfigurationOptions();
            var result = _adapter.BuildSdkMetadata(config.SdkMachineName, config.SdkMachineIP, config.IPAddressesEnabled, null, _log.Object);

            // Assert.
            Assert.IsFalse(string.IsNullOrEmpty(result.MachineName));
            Assert.AreNotEqual(Splitio.Commons.Shared.Constants.Gral.NA, result.MachineName);
            Assert.AreNotEqual(Splitio.Commons.Shared.Constants.Gral.Unknown, result.MachineName);
        }

        [TestMethod]
        public void ReadConfigWithIPAddressesDisabledReturnsNameEmpty()
        {
            // Arrange.
            var config = new ConfigurationOptions
            {
                IPAddressesEnabled = false,
            };

            // Act.
            var result = _adapter.BuildSdkMetadata(config.SdkMachineName, config.SdkMachineIP, config.IPAddressesEnabled, null, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Commons.Shared.Constants.Gral.Unknown, result.MachineName);
        }

        [TestMethod]
        public void ReadConfigWithIPAddressesDisabledAndRedisReturnsNA()
        {
            // Arrange.
            var config = new ConfigurationOptions
            {
                IPAddressesEnabled = false,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions
                {
                    Type = AdapterType.Redis
                }
            };

            // Act.
            var result = _adapter.BuildSdkMetadata(config.SdkMachineName, config.SdkMachineIP, config.IPAddressesEnabled, config.CacheAdapterConfig.Type, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Commons.Shared.Constants.Gral.NA, result.MachineName);
        }
    }
}
