using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;

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
            var result = _adapter.ReadConfig(new ConfigurationOptions(), _log.Object);

            // Assert.
            Assert.IsFalse(string.IsNullOrEmpty(result.SdkMachineName));
            Assert.AreNotEqual(Splitio.Constants.Gral.NA, result.SdkMachineName);
            Assert.AreNotEqual(Splitio.Constants.Gral.Unknown, result.SdkMachineName);
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
            var result = _adapter.ReadConfig(config, _log.Object);

            // Assert.
            Assert.IsTrue(string.IsNullOrEmpty(result.SdkMachineName));
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
            var result = _adapter.ReadConfig(config, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.NA, result.SdkMachineName);
        }

        [TestMethod]
        public void ReadConfigWithNonASCIICharactesReturnsUnknown()
        {
            // Arrange.
            var config = new ConfigurationOptions
            {
                SdkMachineName = "TEST-志"
            };

            // Act.
            var result = _adapter.ReadConfig(config, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.Unknown, result.SdkMachineName);
        }
    }
}
