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

        private readonly ConcurrentDictionary<string, ParsedSplit> _splits;
        private readonly ConcurrentDictionary<string, int> _trafficTypes;
        private long _changeNumber;

        public InMemorySplitCache(ConcurrentDictionary<string, ParsedSplit> splits, long changeNumber = -1)
        {
            _splits = splits;
            _changeNumber = changeNumber;
            _trafficTypes = new ConcurrentDictionary<string, int>();

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

        #region Sync Methods
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

            return exists;
        }

        public void AddSplit(string splitName, SplitBase split)
        {
            var parsedSplit = (ParsedSplit)split;

            if (_splits.TryAdd(splitName, parsedSplit))
            {
                IncreaseTrafficTypeCount(parsedSplit.trafficTypeName);
            }
        }

        public bool RemoveSplit(string splitName)
        {            
            var removed = _splits.TryRemove(splitName, out ParsedSplit removedSplit);

            if (removed)
            {
                DecreaseTrafficTypeCount(removedSplit);
            }            

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
        #endregion

        #region Async Methods
        public Task AddSplitAsync(string splitName, SplitBase split)
        {
            AddSplit(splitName, split);

            return Task.FromResult(0);
        }

        public Task<bool> RemoveSplitAsync(string splitName)
        {
            return Task.FromResult(RemoveSplit(splitName));
        }

        public Task<bool> AddOrUpdateAsync(string splitName, SplitBase split)
        {
            return Task.FromResult(AddOrUpdate(splitName, split));
        }

        public Task SetChangeNumberAsync(long changeNumber)
        {
            SetChangeNumber(changeNumber);
            
            return Task.FromResult(0);
        }

        public Task<long> GetChangeNumberAsync()
        {
            return Task.FromResult(GetChangeNumber());
        }

        public Task<ParsedSplit> GetSplitAsync(string splitName)
        {
            return Task.FromResult(GetSplit(splitName));
        }

        public Task<List<ParsedSplit>> GetAllSplitsAsync()
        {
            return Task.FromResult(GetAllSplits());
        }

        public Task ClearAsync()
        {
            Clear();

            return Task.FromResult(0);
        }

        public Task<bool> TrafficTypeExistsAsync(string trafficType)
        {
            return Task.FromResult(TrafficTypeExists(trafficType));
        }

        public Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames)
        {
            return Task.FromResult(FetchMany(splitNames));
        }

        public Task KillAsync(long changeNumber, string splitName, string defaultTreatment)
        {
            Kill(changeNumber, splitName, defaultTreatment);
            
            return Task.FromResult(0);
        }

        public Task<List<string>> GetSplitNamesAsync()
        {
            return Task.FromResult(GetSplitNames());
        }

        public Task<int> SplitsCountAsync()
        {
            return Task.FromResult(SplitsCount());
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
