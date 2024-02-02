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
            var result = _adapter.BuildSdkMetadata(new ConfigurationOptions(), _log.Object);

            // Assert.
            Assert.IsFalse(string.IsNullOrEmpty(result.MachineName));
            Assert.AreNotEqual(Splitio.Constants.Gral.NA, result.MachineName);
            Assert.AreNotEqual(Splitio.Constants.Gral.Unknown, result.MachineName);
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
            var result = _adapter.BuildSdkMetadata(config, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.Unknown, result.MachineName);
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
            var result = _adapter.BuildSdkMetadata(config, _log.Object);

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.NA, result.MachineName);
        }
    }
}
