using Splitio.Services.Localhost;
using Splitio.Commons.Dto;

namespace Splitio.Domain
{
    public class LocalhostClientConfigurations : BaseConfig
    {
        public string FilePath { get; set; }
        public ILocalhostFileSync FileSync { get; set; }
    }
}
