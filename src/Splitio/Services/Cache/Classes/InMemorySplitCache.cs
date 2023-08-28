using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Cache.Classes
{
    public class InMemorySplitCache : ISplitCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemorySplitCache));

        private readonly ConcurrentDictionary<string, ParsedSplit> _splits;
        private readonly ConcurrentDictionary<string, int> _trafficTypes;
        private readonly ConcurrentDictionary<string, HashSet<string>> _flagSets;
        private long _changeNumber;

        public InMemorySplitCache(ConcurrentDictionary<string, ParsedSplit> splits, long changeNumber = -1)
        {
            _splits = splits;
            _changeNumber = changeNumber;
            _trafficTypes = new ConcurrentDictionary<string, int>();
            _flagSets = new ConcurrentDictionary<string, HashSet<string>>();

            if (!splits.IsEmpty)
            {
                foreach (var split in splits)
                {
                    if (split.Value != null)
                    {
                        IncreaseTrafficTypeCount(split.Value.trafficTypeName);
                    }
                }
            }
        }

        #region Public Methods
        public bool AddOrUpdate(string splitName, SplitBase split)
        {
            if (split == null) return false;

            var parsedSplit = (ParsedSplit)split;

            var exists = _splits.TryGetValue(splitName, out ParsedSplit oldSplit);

            if (exists)
            {
                DecreaseTrafficTypeCount(oldSplit);
            }

            _splits.AddOrUpdate(splitName, parsedSplit, (key, oldValue) => parsedSplit);
            
            IncreaseTrafficTypeCount(parsedSplit?.trafficTypeName);

            AddOrUpdateFlagSets(parsedSplit);

            return exists;
        }

        public void AddSplit(string splitName, SplitBase split)
        {
            var parsedSplit = (ParsedSplit)split;

            if (_splits.TryAdd(splitName, parsedSplit))
            {
                IncreaseTrafficTypeCount(parsedSplit.trafficTypeName);

                AddOrUpdateFlagSets(parsedSplit);
            }
        }

        public bool RemoveSplit(string splitName)
        {            
            var removed = _splits.TryRemove(splitName, out ParsedSplit removedSplit);

            if (removed)
            {
                DecreaseTrafficTypeCount(removedSplit);
            }

            DeleteFromFlagSets(removedSplit);

            return removed;
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
            _splits.TryGetValue(splitName, out ParsedSplit value);

            return value;
        }

        public List<ParsedSplit> GetAllSplits()
        {            
            return _splits
                .Values
                .Where(s => s != null)
                .ToList();
        }

        public void Clear()
        {
            _splits.Clear();            
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
            var split = GetSplit(splitName);

            if (split == null) return;

            split.defaultTreatment = defaultTreatment;
            split.killed = true;
            split.changeNumber = changeNumber;

            AddOrUpdate(splitName, split);
        }

        public List<string> GetSplitNames()
        {
            return _splits
                .Keys
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        public int SplitsCount()
        {
            return GetSplitNames().Count;
        }

        public HashSet<string> GetNamesByFlagSets(List<string> flagSets)
        {
            var namesToReturn = new HashSet<string>();

            foreach (var fSet in flagSets)
            {
                if (_flagSets.TryGetValue(fSet, out HashSet<string> ffNames))
                    namesToReturn.UnionWith(ffNames);
                else
                    _log.Warn();
            }

            return namesToReturn;
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

        private void AddOrUpdateFlagSets(ParsedSplit featureFlag)
        {
            foreach (var fSet in featureFlag.Sets)
            {
                _flagSets.AddOrUpdate(fSet, new HashSet<string>() { featureFlag.name }, (_, ffNames) =>
                {
                    ffNames.Add(featureFlag.name);
                    return ffNames;
                });
            }

            DeleteFromFlagSetsIfNecessary(featureFlag);
        }

        private void DeleteFromFlagSetsIfNecessary(ParsedSplit featureFlag)
        {
            var keys = _flagSets.Keys;

            foreach (var key in keys)
            {
                // if featureFlag.Sets is empty or Contains return true that means that should not remove from Flag Sets cache.
                if (featureFlag.Sets.Contains(key))
                    continue;

                RemoveNames(key, featureFlag.name);
            }
        }
        private void DeleteFromFlagSets(ParsedSplit featureFlag)
        {
            var keys = _flagSets.Keys;

            foreach (var key in keys)
            {
                RemoveNames(key, featureFlag.name);
            }
        }

        private void RemoveNames(string key, string name)
        {
            var ffNames = _flagSets.AddOrUpdate(key, new HashSet<string>() { name }, (_, ffNames) =>
            {
                ffNames.Remove(name);
                return ffNames;
            });

            if (ffNames.Count == 0)
                _flagSets.TryRemove(key, out HashSet<string> _);
        }
        #endregion
    }
}
