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
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public class SplitManager : ISplitManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitManager));

        private readonly ISplitCache _featureFlagCache;
        private readonly ISplitNameValidator _splitNameValidator;
        private readonly IBlockUntilReadyService _blockUntilReadyService;

        public SplitManager(ISplitCache featureFlagCache, IBlockUntilReadyService blockUntilReadyService)
        {
            _featureFlagCache = featureFlagCache;
            _splitNameValidator = new SplitNameValidator(_log);
            _blockUntilReadyService = blockUntilReadyService;
        }

        #region Public Async Methods
        public async Task<List<SplitView>> SplitsAsync()
        {
            if (!IsSdkReady(nameof(SplitsAsync))) return null;

            var currentFeatureFlags = await _featureFlagCache.GetAllSplitsAsync();

            return GetFeatureFlagViewList(currentFeatureFlags);
        }

        public async Task<SplitView> SplitAsync(string featureName)
        {
            if (!IsSdkReady(nameof(SplitAsync))) return null;

            if (!IsFeatureFlagNameValid(nameof(SplitAsync), featureName, out featureName)) return null;

            var featureFlag = await _featureFlagCache.GetSplitAsync(featureName);

            return GetFeatureFlagView(featureFlag, featureName);
        }

        public async Task<List<string>> SplitNamesAsync()
        {
            if (!IsSdkReady(nameof(SplitNamesAsync))) return null;

            return await _featureFlagCache.GetSplitNamesAsync();
        }
        #endregion

        #region Public Sync Methods
        public List<SplitView> Splits()
        {
            if (!IsSdkReady(nameof(Splits))) return null;

            var currentFeatureFlags = _featureFlagCache.GetAllSplits();

            return GetFeatureFlagViewList(currentFeatureFlags);
        }

        public SplitView Split(string featureName)
        {
            if (!IsSdkReady(nameof(Split))) return null;

            if (!IsFeatureFlagNameValid(nameof(Split), featureName, out featureName)) return null;

            var featureFlag = _featureFlagCache.GetSplit(featureName);

            return GetFeatureFlagView(featureFlag, featureName);
        }
        
        public List<string> SplitNames()
        {
            if (!IsSdkReady(nameof(SplitNames))) return null;

            return _featureFlagCache.GetSplitNames();
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            _blockUntilReadyService.BlockUntilReady(blockMilisecondsUntilReady);
        }
        #endregion

        #region Private Methods
        private bool IsSdkReady(string methodName)
        {
            if (!_blockUntilReadyService.IsSdkReady())
            {
                _log.Error($"{methodName}: the SDK is not ready, the operation cannot be executed.");
                return false;
            }

            if (_featureFlagCache == null) return false;

            return true;
        }

        private bool IsFeatureFlagNameValid(string method, string featureName, out string featureNameUpdated)
        {
            var result = _splitNameValidator.SplitNameIsValid(featureName, method);

            featureNameUpdated = result.Value;

            return result.Success;
        }

        private List<SplitView> GetFeatureFlagViewList(List<ParsedSplit> featureFlags)
        {
            return featureFlags
                .Select(x =>
                new SplitView()
                {
                    name = x.name,
                    killed = x.killed,
                    changeNumber = x.changeNumber,
                    treatments = (x.conditions.Where(z => z.conditionType == ConditionType.ROLLOUT).FirstOrDefault() ?? x.conditions.FirstOrDefault())?.partitions.Select(y => y.treatment).ToList(),
                    trafficType = x.trafficTypeName,
                    configs = x.configurations
                })
                .ToList();
        }

        private SplitView GetFeatureFlagView(ParsedSplit featureFlag, string featureName)
        {
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
                configs = featureFlag.configurations
            };

            return lightSplit;
        }
        #endregion
    }
}
