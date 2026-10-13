using Splitio.Commons.Cache.Interfaces;
using Splitio.Commons.Dto;
using Splitio.Services.Client.Classes;
using Splitio.Services.Shared.Classes;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IConfigService
    {
        BaseConfig ReadConfig(ConfigurationOptions config, ConfigTypes confingTypes, IStatusManager statusManager = null);
    }
}
