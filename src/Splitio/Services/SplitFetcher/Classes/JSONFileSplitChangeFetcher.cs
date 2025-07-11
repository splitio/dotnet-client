using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.SplitFetcher.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Splitio.Common;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class JSONFileSplitChangeFetcher : SplitChangeFetcher, ISplitChangeFetcher 
    {
        private readonly string _filePath;

        public JSONFileSplitChangeFetcher(string filePath)
        {
            _filePath = filePath;
        }

        protected override Task<TargetingRulesDto> FetchFromBackendAsync(FetchOptions fetchOptions)
        {
            var json = File.ReadAllText(_filePath);
            var targetingRulesDto = JsonConvert.DeserializeObject<TargetingRulesDto>(json, SerializerSettings.DefaultSerializerSettings);
            if (targetingRulesDto != null)
            {
                return Task.FromResult(targetingRulesDto);
            }

            var splitsResult = JsonConvert.DeserializeObject<OldSplitChangesDto>(json, SerializerSettings.DefaultSerializerSettings);

            return Task.FromResult(splitsResult.ToTargetingRulesDto());
        }
    }
}
