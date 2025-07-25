﻿using Splitio.Domain;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System.IO;
using System.Threading.Tasks;

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
            var targetingRulesDto = JsonConvertWrapper.DeserializeObject<TargetingRulesDto>(json);
            if (targetingRulesDto != null)
            {
                return Task.FromResult(targetingRulesDto);
            }

            var splitsResult = JsonConvertWrapper.DeserializeObject<OldSplitChangesDto>(json);

            return Task.FromResult(splitsResult.ToTargetingRulesDto());
        }
    }
}
