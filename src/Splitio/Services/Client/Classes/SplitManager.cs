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
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitManager));

        private readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private readonly ISplitNameValidator _splitNameValidator;
        private readonly IBlockUntilReadyService _blockUntilReadyService;

        public SplitManager(IFeatureFlagCacheConsumer featureFlagCacheConsumer, IBlockUntilReadyService blockUntilReadyService)
        {
            _featureFlagCacheConsumer = featureFlagCacheConsumer;
            _splitNameValidator = new SplitNameValidator(_log);
            _blockUntilReadyService = blockUntilReadyService;
        }

        #region Public Async Methods
        public async Task<List<SplitView>> SplitsAsync()
        {
            if (!IsSdkReady(nameof(SplitsAsync))) return null;

            var currentFeatureFlags = await _featureFlagCacheConsumer.GetAllSplitsAsync();

            return currentFeatureFlags
                .Select(x => x.ToSplitView())
                .ToList();
        }

        public async Task<SplitView> SplitAsync(string featureName)
        {
            if (!IsSdkReady(nameof(SplitAsync))) return null;

            if (!IsFeatureFlagNameValid(Enums.API.SplitAsync, featureName, out featureName)) return null;

            var featureFlag = await _featureFlagCacheConsumer.GetSplitAsync(featureName);

            return GetFeatureFlagView(featureFlag, featureName);
        }

        public async Task<List<string>> SplitNamesAsync()
        {
            if (!IsSdkReady(nameof(SplitNamesAsync))) return null;

            return await _featureFlagCacheConsumer.GetSplitNamesAsync();
        }
        #endregion

        #region Public Sync Methods
        public List<SplitView> Splits()
        {
            if (!IsSdkReady(nameof(Splits))) return null;

            var currentFeatureFlags = _featureFlagCacheConsumer.GetAllSplits();

            return currentFeatureFlags
                .Select(x => x.ToSplitView())
                .ToList();
        }

        public SplitView Split(string featureName)
        {
            if (!IsSdkReady(nameof(Split))) return null;

            if (!IsFeatureFlagNameValid(Enums.API.Split, featureName, out featureName)) return null;

            var featureFlag = _featureFlagCacheConsumer.GetSplit(featureName);

            return GetFeatureFlagView(featureFlag, featureName);
        }

        public List<string> SplitNames()
        {
            if (!IsSdkReady(nameof(SplitNames))) return null;

            return _featureFlagCacheConsumer.GetSplitNames();
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

            if (_featureFlagCacheConsumer == null) return false;

            return true;
        }

        private bool IsFeatureFlagNameValid(Enums.API method, string featureName, out string featureNameUpdated)
        {
            var result = _splitNameValidator.SplitNameIsValid(featureName, method);

            featureNameUpdated = result.Value;

            return result.Success;
        }

        private SplitView GetFeatureFlagView(ParsedSplit featureFlag, string featureName)
        {
            if (featureFlag == null)
            {
                _log.Warn($"split: you passed {featureName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

                return null;
            }

            return featureFlag.ToSplitView();
        }
        #endregion
    }
}
