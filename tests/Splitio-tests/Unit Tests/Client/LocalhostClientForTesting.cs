using Splitio.Services.Client.Classes;

namespace Splitio_Tests.Unit_Tests.Client
{
    public class LocalhostClientForTesting : LocalhostClient
    {
        public LocalhostClientForTesting(string filePath) : base(new ConfigurationOptions { LocalhostFilePath = filePath })
        { }
    }
}
