using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Client.Classes
{
    public class SplitManager : ISplitManager
    {
        private readonly ISplitLogger _log;
        private readonly ISplitCache _featureFlagCache;
        private readonly ISplitNameValidator _splitNameValidator;
        private readonly IBlockUntilReadyService _blockUntilReadyService;

        public SplitManager(ISplitCache featureFlagCache, IBlockUntilReadyService blockUntilReadyService)
        {
            _featureFlagCache = featureFlagCache;
            _log = WrapperAdapter.Instance().GetLogger(typeof(SplitManager));
            _splitNameValidator = new SplitNameValidator(_log);
            _blockUntilReadyService = blockUntilReadyService;
        }

        public List<SplitView> Splits()
        {
            if (!IsSdkReady(nameof(Splits)) || _featureFlagCache == null)
            {
                return null;
            }

            var currentFeatureFlags = _featureFlagCache.GetAllSplits();

            var lightFeatureFlags = currentFeatureFlags
                .Select(x =>
                new SplitView()
                {
                    name = x.name,
                    killed = x.killed,
                    changeNumber = x.changeNumber,
                    treatments = (x.conditions.Where(z => z.conditionType == ConditionType.ROLLOUT).FirstOrDefault() ?? x.conditions.FirstOrDefault())?.partitions.Select(y => y.treatment).ToList(),
                    trafficType = x.trafficTypeName,
                    configs = x.configurations,
                    sets = GetFlagSets(x)
                });

            return lightFeatureFlags.ToList();
        }

        public SplitView Split(string featureName)
        {
            if (!IsSdkReady(nameof(Split)) || _featureFlagCache == null)
            {
                return null;
            }

            var result = _splitNameValidator.SplitNameIsValid(featureName, nameof(Split));

            if (!result.Success)
            {
                return null;
            }

            featureName = result.Value;

            var featureFlag = _featureFlagCache.GetSplit(featureName);

            if (featureFlag == null)
            {
                _log.Warn($"split: you passed {featureName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

                return null;
            }

            var condition = featureFlag.conditions.Where(x => x.conditionType == ConditionType.ROLLOUT).FirstOrDefault() ?? featureFlag.conditions.FirstOrDefault();

            var treatments = condition != null ? condition.partitions.Select(y => y.treatment).ToList() : new List<string>();

            var lightSplit = new SplitView()
            {
                name = featureFlag.name,
                killed = featureFlag.killed,
                changeNumber = featureFlag.changeNumber,
                treatments = treatments,
                trafficType = featureFlag.trafficTypeName,
                configs = featureFlag.configurations,
                sets = GetFlagSets(featureFlag)
            };

            return lightSplit;
        }
        
        public List<string> SplitNames()
        {
            if (!IsSdkReady(nameof(SplitNames)) || _featureFlagCache == null)
            {
                return null;
            }

            return _featureFlagCache.GetSplitNames();
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            _blockUntilReadyService.BlockUntilReady(blockMilisecondsUntilReady);
        }

        private bool IsSdkReady(string methodName)
        {
            if (!_blockUntilReadyService.IsSdkReady())
            {
                _log.Error($"{methodName}: the SDK is not ready, the operation cannot be executed.");
                return false;
            }

            return true;
        }

        private static List<string> GetFlagSets(ParsedSplit featureFlag)
        {
            return featureFlag.Sets == null ? new List<string>() : featureFlag.Sets.ToList();
        }
    }
}
