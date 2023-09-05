using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
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

        private readonly IFlagSetsFilter _flagSetsFilter;
        private readonly ConcurrentDictionary<string, ParsedSplit> _splits;
        private readonly ConcurrentDictionary<string, int> _trafficTypes;
        private readonly ConcurrentDictionary<string, HashSet<string>> _flagSets;

        private long _changeNumber;

        public InMemorySplitCache(ConcurrentDictionary<string, ParsedSplit> splits, IFlagSetsFilter flagSetsFilter, long changeNumber = -1)
        {
            _splits = splits;
            _flagSetsFilter = flagSetsFilter;
            _changeNumber = changeNumber;
            _trafficTypes = new ConcurrentDictionary<string, int>();
            _flagSets = new ConcurrentDictionary<string, HashSet<string>>();

            if (!splits.IsEmpty)
            {
                foreach (var split in splits)
                {
                    if (split.Value == null)
                        continue;

                    IncreaseTrafficTypeCount(split.Value.trafficTypeName);
                    AddToFlagSets(split.Value);
                }
            }
        }

        #region Public Producer Methods
        public void Update(List<ParsedSplit> toAdd, List<ParsedSplit> toRemove, long till)
        {
            foreach (var featureFlag in toAdd)
            {
                if (_splits.TryGetValue(featureFlag.name, out ParsedSplit existing))
                {
                    DecreaseTrafficTypeCount(existing);
                    DeleteFromFlagSets(existing);
                }

                _splits.AddOrUpdate(featureFlag.name, featureFlag, (key, oldValue) => featureFlag);

                IncreaseTrafficTypeCount(featureFlag.trafficTypeName);
                AddToFlagSets(featureFlag);
            }

            foreach (var featureFlag in toRemove)
            {
                if (_splits.TryGetValue(featureFlag.name, out ParsedSplit cached))
                {
                    _splits.TryRemove(featureFlag.name, out ParsedSplit removedSplit);
                    
                    DecreaseTrafficTypeCount(removedSplit);
                    DeleteFromFlagSets(removedSplit);
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
        public void Clear()
        {
            _splits.Clear();
            _trafficTypes.Clear();
        }

        public void Kill(long changeNumber, string splitName, string defaultTreatment)
        {
            var featureFlag = GetSplit(splitName);

            if (featureFlag == null) return;

            featureFlag.defaultTreatment = defaultTreatment;
            featureFlag.killed = true;
            featureFlag.changeNumber = changeNumber;

            _splits.AddOrUpdate(featureFlag.name, featureFlag, (key, oldValue) => featureFlag);
        }
        #endregion

        #region Public Consumer Methods

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

        public Dictionary<string, HashSet<string>> GetNamesByFlagSets(List<string> flagSets)
        {
            var toReturn = new Dictionary<string, HashSet<string>>();

            foreach (var fSet in flagSets)
            {
                _flagSets.TryGetValue(fSet, out HashSet<string> ffNames);
                toReturn.Add(fSet, ffNames);
            }

            return toReturn;
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
                if (!_flagSetsFilter.Match(fSet))
                    continue;

                _flagSets.AddOrUpdate(fSet, new HashSet<string>() { featureFlag.name }, (_, ffNames) =>
                {
                    ffNames.Add(featureFlag.name);
                    return ffNames;
                });
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
