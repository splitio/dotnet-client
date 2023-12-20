using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class LocalhostClientTests : BaseLocalhostClientTests
    {
        public LocalhostClientTests() : base("watcher")
        {
        }

        protected override ConfigurationOptions GetConfiguration(string fileName)
        {
            return new ConfigurationOptions
            {
                LocalhostFilePath = fileName,
                Logger = SplitLogger.Console(Level.Debug)
            };
        }
    }
}
