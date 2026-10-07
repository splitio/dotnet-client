using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Commons.Shared.Logger;
using Splitio.Services.Client.Classes;

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
