using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using Splitio.Services.Localhost;
using Splitio.Services.Logger;

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
                LocalhostFileSync = LocalhostFileSync.FileSyncPolling(intervalMs: 2),
                Logger = SplitLogger.Console(Level.Debug)
            };
        }
    }
}
