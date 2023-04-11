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
    public class InMemorySplitCache : ISplitCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemorySplitCache));

        private ConcurrentDictionary<string, ParsedSplit> _splits;
        private ConcurrentDictionary<string, int> _trafficTypes;
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
                _log.Error("ChangeNumber for splits cache is less than previous");
            }

            _changeNumber = changeNumber;
        }

        public Task<long> GetChangeNumberAsync()
        {
            return Task.FromResult(_changeNumber);
        }

        public Task<ParsedSplit> GetSplitAsync(string splitName)
        {
            _splits.TryGetValue(splitName, out ParsedSplit value);

            return Task.FromResult(value);
        }

        public Task<List<ParsedSplit>> GetAllSplitsAsync()
        {
            var splits = _splits
                .Values
                .Where(s => s != null)
                .ToList();

            return Task.FromResult(splits);
        }

        public void Clear()
        {
            _splits.Clear();            
            _trafficTypes.Clear();
        }

        public Task<bool> TrafficTypeExistsAsync(string trafficType)
        {
            if (string.IsNullOrEmpty(trafficType))
            {
                return Task.FromResult(false);
            }

            var exists = _trafficTypes.ContainsKey(trafficType);

            return Task.FromResult(exists);
        }

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
                    _trafficTypes.TryRemove(split.trafficTypeName, out int value);

                    return;
                }

                var newQuantity = quantity - 1;

                _trafficTypes.TryUpdate(split.trafficTypeName, newQuantity, quantity);
            }
        }

        public async Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames)
        {
            var splits = new List<ParsedSplit>();

            foreach (var name in splitNames)
            {
                var split = await GetSplitAsync(name);
                splits.Add(split);
            }

            var toReturn = splits
                .Where(s => s != null)
                .ToList();

            return toReturn;
        }

        public async Task KillAsync(long changeNumber, string splitName, string defaultTreatment)
        {
            var split = await GetSplitAsync(splitName);

            if (split == null) return;

            split.defaultTreatment = defaultTreatment;
            split.killed = true;
            split.changeNumber = changeNumber;

            AddOrUpdate(splitName, split);
        }

        public Task<List<string>> GetSplitNamesAsync()
        {
            var names = _splits
                .Keys
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            return Task.FromResult(names);
        }

        public async Task<int> SplitsCountAsync()
        {
            var names = await GetSplitNamesAsync();

            return names.Count;
        }
    }
}
