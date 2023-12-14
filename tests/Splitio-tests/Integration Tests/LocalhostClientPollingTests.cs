using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class LocalhostClientPollingTests : BaseLocalhostClientTests
    {
        public LocalhostClientPollingTests() : base("polling")
        {
        }

        protected override ConfigurationOptions GetConfiguration(string fileName)
        {
            return new ConfigurationOptions
            {
                LocalhostFilePath = fileName,
                LocalhostPolling = true,
                LocalhostIntervalMs = 5
            };
        }
    }
}
