using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Classes
{
    public class InMemorySplitCache : IFeatureFlagCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemorySplitCache));

        private readonly ConcurrentDictionary<string, ParsedSplit> _featureFlags;
        private readonly ConcurrentDictionary<string, int> _trafficTypes;
        private long _changeNumber;

        public InMemorySplitCache(ConcurrentDictionary<string, ParsedSplit> featureFlags, long changeNumber = -1)
        {
            _featureFlags = featureFlags;
            _changeNumber = changeNumber;
            _trafficTypes = new ConcurrentDictionary<string, int>();

            if (!_featureFlags.IsEmpty)
            {
                foreach (var featureFlag in _featureFlags)
                {
                    if (featureFlag.Value != null)
                    {
                        IncreaseTrafficTypeCount(featureFlag.Value.trafficTypeName);
                    }
                }
            }
        }

        #region Sync Methods
        public void Update(List<ParsedSplit> toAdd, List<string> toRemove, long till)
        {
            foreach (var featureFlag in toAdd)
            {
                if (_featureFlags.TryGetValue(featureFlag.name, out ParsedSplit existing))
                {
                    DecreaseTrafficTypeCount(existing);
                }

                _featureFlags.AddOrUpdate(featureFlag.name, featureFlag, (key, oldValue) => featureFlag);

                IncreaseTrafficTypeCount(featureFlag.trafficTypeName);
            }

            foreach (var featureFlagName in toRemove)
            {
                if (_featureFlags.TryGetValue(featureFlagName, out ParsedSplit cached))
                {
                    _featureFlags.TryRemove(featureFlagName, out ParsedSplit removedSplit);

                    DecreaseTrafficTypeCount(removedSplit);
                }
            }

            SetChangeNumber(till);
        }

        public void SetChangeNumber(long changeNumber)
        {
            if (changeNumber < _changeNumber)
            {
                _log.Error("ChangeNumber for feature flags cache is less than previous");
            }

            _changeNumber = changeNumber;
        }

        public long GetChangeNumber()
        {
            return _changeNumber;
        }

        public ParsedSplit GetSplit(string splitName)
        {
            _featureFlags.TryGetValue(splitName, out ParsedSplit value);

            return value;
        }

        public List<ParsedSplit> GetAllSplits()
        {            
            return _featureFlags
                .Values
                .Where(s => s != null)
                .ToList();
        }

        public void Clear()
        {
            _featureFlags.Clear();            
            _trafficTypes.Clear();
        }

        public bool TrafficTypeExists(string trafficType)
        {
            var exists = _trafficTypes.TryGetValue(trafficType, out int quantity);

            return exists && quantity > 0;
        }

        public List<ParsedSplit> FetchMany(List<string> splitNames)
        {
            var splits = new List<ParsedSplit>();

            foreach (var name in splitNames)
            {
                splits.Add(GetSplit(name));
            }

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public void Kill(long changeNumber, string splitName, string defaultTreatment)
        {
            var featureFlag = GetSplit(splitName);

            if (featureFlag == null) return;

            featureFlag.defaultTreatment = defaultTreatment;
            featureFlag.killed = true;
            featureFlag.changeNumber = changeNumber;

            _featureFlags.AddOrUpdate(featureFlag.name, featureFlag, (key, oldValue) => featureFlag);
        }

        public List<string> GetSplitNames()
        {
            return _featureFlags
                .Keys
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        public int SplitsCount()
        {
            return GetSplitNames().Count;
        }
        #endregion

        #region Async Methods
        public Task<ParsedSplit> GetSplitAsync(string splitName)
        {
            return Task.FromResult(GetSplit(splitName));
        }

        public Task<List<ParsedSplit>> GetAllSplitsAsync()
        {
            return Task.FromResult(GetAllSplits());
        }

        public Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames)
        {
            return Task.FromResult(FetchMany(splitNames));
        }

        public Task<List<string>> GetSplitNamesAsync()
        {
            return Task.FromResult(GetSplitNames());
        }
        #endregion

        #region Private Methods
        private void IncreaseTrafficTypeCount(string trafficType)
        {
            if (string.IsNullOrEmpty(trafficType)) return;

            _trafficTypes.AddOrUpdate(trafficType, 1, (key, oldValue) => oldValue + 1);
        }

        private void DecreaseTrafficTypeCount(ParsedSplit split)
        {
            if (split == null || string.IsNullOrEmpty(split.trafficTypeName)) return;

            if (_trafficTypes.TryGetValue(split.trafficTypeName, out int quantity))
            {
                if (quantity <= 1)
                {
                    _trafficTypes.TryRemove(split.trafficTypeName, out _);

                    return;
                }

                var newQuantity = quantity - 1;

                _trafficTypes.TryUpdate(split.trafficTypeName, newQuantity, quantity);
            }
        }
        #endregion
    }
}
