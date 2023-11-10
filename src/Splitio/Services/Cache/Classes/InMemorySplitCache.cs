using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
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
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemorySplitCache));
        private readonly IFlagSetsFilter _flagSetsFilter;

        private readonly ConcurrentDictionary<string, ParsedSplit> _featureFlags;
        private readonly ConcurrentDictionary<string, int> _trafficTypes;
        private readonly ConcurrentDictionary<string, HashSet<string>> _flagSets;

        private long _changeNumber;

        public InMemorySplitCache(ConcurrentDictionary<string, ParsedSplit> featureFlags,
            IFlagSetsFilter flagSetsFilter,
            long changeNumber = -1)
        {
            _featureFlags = featureFlags;
            _flagSetsFilter = flagSetsFilter;
            _changeNumber = changeNumber;
            _trafficTypes = new ConcurrentDictionary<string, int>();
            _flagSets = new ConcurrentDictionary<string, HashSet<string>>();

            if (!_featureFlags.IsEmpty)
            {
                foreach (var featureFlag in _featureFlags)
                {
                    if (featureFlag.Value != null)
                    {
                        IncreaseTrafficTypeCount(featureFlag.Value.trafficTypeName);
                        AddToFlagSets(featureFlag.Value);
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
                    RemoveFromFlagSets(existing.name, existing.Sets);
                }

                _featureFlags.AddOrUpdate(featureFlag.name, featureFlag, (key, oldValue) => featureFlag);

                IncreaseTrafficTypeCount(featureFlag.trafficTypeName);
                AddToFlagSets(featureFlag);
            }

            foreach (var featureFlagName in toRemove)
            {
                if (!_featureFlags.TryGetValue(featureFlagName, out ParsedSplit cached))
                    continue;

                _featureFlags.TryRemove(featureFlagName, out ParsedSplit removedSplit);

                DecreaseTrafficTypeCount(removedSplit);
                RemoveFromFlagSets(removedSplit.name, removedSplit.Sets);
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
            _featureFlags.Clear();
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

        public Dictionary<string, HashSet<string>> GetNamesByFlagSets(List<string> flagSets)
        {
            var toReturn = new Dictionary<string, HashSet<string>>();

            foreach (var fSet in flagSets)
            {
                _flagSets.TryGetValue(fSet, out HashSet<string> ffNames);
                toReturn.Add(fSet, ffNames ?? new HashSet<string>());
            }

            return toReturn;
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

        public Task<Dictionary<string, HashSet<string>>> GetNamesByFlagSetsAsync(List<string> flagSets)
        {
            return Task.FromResult(GetNamesByFlagSets(flagSets));
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

        private void AddToFlagSets(ParsedSplit featureFlag)
        {
            if (featureFlag.Sets == null) return;

            foreach (var fSet in featureFlag.Sets)
            {
                if (!_flagSetsFilter.Intersect(fSet))
                    continue;

                _flagSets.AddOrUpdate(fSet, new HashSet<string>() { featureFlag.name }, (_, ffNames) =>
                {
                    ffNames.Add(featureFlag.name);
                    return ffNames;
                });
            }
        }

        private void RemoveFromFlagSets(string featureFlagName, HashSet<string> sets)
        {
            if (sets == null) return;

            foreach (var fSet in sets)
            {
                RemoveNames(fSet, featureFlagName);
            }
        }

        private void RemoveNames(string key, string name)
        {
            var names = _flagSets.AddOrUpdate(key, new HashSet<string>() { name }, (_, ffNames) =>
            {
                ffNames.Remove(name);
                return ffNames;
            });

            if (names.Count == 0) _flagSets.TryRemove(key, out HashSet<string> _);
        }
        #endregion
    }
}
