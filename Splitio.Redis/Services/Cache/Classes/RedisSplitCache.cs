using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSplitCache : RedisCacheBase, IFeatureFlagCacheConsumer
    {
        protected const string SplitKeyPrefix = "split.";
        protected const string SplitsKeyPrefix = "splits.";

        private readonly IParser<Split, ParsedSplit> _splitParser;
        private readonly IRedisAdapterConsumer _redisAdapter;

        public RedisSplitCache(IRedisAdapterConsumer redisAdapter, IParser<Split, ParsedSplit> splitParser, RedisConfig redisConfig, bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapter = redisAdapter;
            _splitParser = splitParser;
        }

        #region Sync Methods
        public long GetChangeNumber()
        {
            var key = $"{RedisKeyPrefix}{SplitsKeyPrefix}till";
            var changeNumberString = _redisAdapter.Get(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public ParsedSplit GetSplit(string splitName)
        {
            var key = $"{RedisKeyPrefix}{SplitKeyPrefix}{splitName}";
            var splitJson = _redisAdapter.Get(key);

            if (string.IsNullOrEmpty(splitJson))
                return null;

            var split = JsonConvert.DeserializeObject<Split>(splitJson);

            return _splitParser.Parse(split);
        }

        public List<ParsedSplit> GetAllSplits()
        {
            var pattern = $"{RedisKeyPrefix}{SplitKeyPrefix}*";
            var splitKeys = _redisAdapter.Keys(pattern);
            var splitValues = _redisAdapter.MGet(splitKeys);

            if (splitValues == null || !splitValues.Any())
                return new List<ParsedSplit>();

            var splits = splitValues
                .Where(x => !x.IsNull)
                .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public bool TrafficTypeExists(string trafficType)
        {
            if (string.IsNullOrEmpty(trafficType)) return false;

            var value = _redisAdapter.Get(GetTrafficTypeKey(trafficType));

            var quantity = value ?? "0";

            int.TryParse(quantity, out int quantityInt);

            return quantityInt > 0;
        }

        public List<ParsedSplit> FetchMany(List<string> splitNames)
        {
            if (!splitNames.Any()) return new List<ParsedSplit>();

            var redisKey = new List<RedisKey>();

            foreach (var name in splitNames)
            {
                redisKey.Add($"{RedisKeyPrefix}{SplitKeyPrefix}{name}");
            }

            var splitValues = _redisAdapter.MGet(redisKey.ToArray());

            if (splitValues == null || !splitValues.Any()) return new List<ParsedSplit>();

            var splits = splitValues
                .Where(s => !s.IsNull)
                .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public List<string> GetSplitNames()
        {
            return GetAllSplits()
                .Select(s => s.name)
                .ToList();
        }

        public int SplitsCount()
        {
            return 0; // No-op
        }

        public Dictionary<string, HashSet<string>> GetNamesByFlagSets(List<string> flagSets)
        {
            var namesByFlagSets = new Dictionary<string, RedisValue[]>();

            foreach (var flagSet in flagSets)
            {
                var key = GetFlagSetKey(flagSet);

                namesByFlagSets.Add(key, _redisAdapter.SMembers(key));
            }

            return BuildNamesByFlagSetResponse(flagSets, namesByFlagSets);
        }
        #endregion

        #region Async Methods
        public async Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames)
        {
            if (!splitNames.Any()) return new List<ParsedSplit>();

            var redisKey = new List<RedisKey>();

            foreach (var name in splitNames)
            {
                redisKey.Add($"{RedisKeyPrefix}{SplitKeyPrefix}{name}");
            }

            var splitValues = await _redisAdapter.MGetAsync(redisKey.ToArray());

            if (splitValues == null || !splitValues.Any()) return new List<ParsedSplit>();

            var splits = splitValues
                .Where(s => !s.IsNull)
                .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public async Task<List<ParsedSplit>> GetAllSplitsAsync()
        {
            var pattern = $"{RedisKeyPrefix}{SplitKeyPrefix}*";
            var splitKeys = await Task.FromResult(_redisAdapter.Keys(pattern));
            var splitValues = await _redisAdapter.MGetAsync(splitKeys);

            if (splitValues == null || !splitValues.Any()) return new List<ParsedSplit>();

            var splits = splitValues
                .Where(x => !x.IsNull)
                .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public async Task<ParsedSplit> GetSplitAsync(string splitName)
        {
            var key = $"{RedisKeyPrefix}{SplitKeyPrefix}{splitName}";
            var splitJson = await _redisAdapter.GetAsync(key);

            if (string.IsNullOrEmpty(splitJson)) return null;

            var split = JsonConvert.DeserializeObject<Split>(splitJson);

            return _splitParser.Parse(split);
        }

        public async Task<List<string>> GetSplitNamesAsync()
        {
            var splits = await GetAllSplitsAsync();

            return splits
                .Select(s => s.name)
                .ToList();
        }

        public async Task<Dictionary<string, HashSet<string>>> GetNamesByFlagSetsAsync(List<string> flagSets)
        {
            var keys = new List<RedisKey>();

            foreach (var flagSet in flagSets)
            {
                keys.Add(GetFlagSetKey(flagSet));
            }

            var namesByFlagSets = await _redisAdapter.PipelineSMembersAsync(keys);

            return BuildNamesByFlagSetResponse(flagSets, namesByFlagSets);
        }
        #endregion

        #region Private Methods

        private string GetTrafficTypeKey(string type)
        {
            return $"{RedisKeyPrefix}trafficType.{type}";
        }

        private string GetFlagSetKey(string name)
        {
            return $"{RedisKeyPrefix}flagSet.{name}";
        }

        private Dictionary<string, HashSet<string>> BuildNamesByFlagSetResponse(List<string> flagSets, Dictionary<string, RedisValue[]> namesByFlagSets)
        {
            var toReturn = new Dictionary<string, HashSet<string>>();

            foreach (var flagSet in flagSets)
            {
                if (!namesByFlagSets.TryGetValue(GetFlagSetKey(flagSet), out var values)) continue;

                toReturn.Add(flagSet, new HashSet<string>(Array.ConvertAll(values, x => (string)x)));
            }

            return toReturn;
        }
        #endregion
    }
}
 