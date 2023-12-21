using Splitio.Services.Localhost;

namespace Splitio.Domain
{
    public class LocalhostClientConfigurations : BaseConfig
    {
        public string FilePath { get; set; }
        public ILocalhostFileSync FileSync { get; set; }
    }
}
