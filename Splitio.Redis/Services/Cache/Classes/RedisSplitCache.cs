using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSplitCache : RedisCacheBase, ISplitCache
    {
        private const string splitKeyPrefix = "split.";
        private const string splitsKeyPrefix = "splits.";

        private readonly ISplitParser _splitParser;

        public RedisSplitCache(IRedisAdapter redisAdapter,
            ISplitParser splitParser,
            string userPrefix = null)
            : base(redisAdapter, userPrefix)
        {
            _splitParser = splitParser;
        }

        public async Task<long> GetChangeNumberAsync()
        {
            var key = $"{RedisKeyPrefix}{splitsKeyPrefix}till";
            var changeNumberString = await _redisAdapter.GetAsync(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public async Task<ParsedSplit> GetSplitAsync(string splitName)
        {
            var key = $"{RedisKeyPrefix}{splitKeyPrefix}{splitName}";
            var splitJson = await _redisAdapter.GetAsync(key);

            if (string.IsNullOrEmpty(splitJson))
                return null;

            var split = JsonConvert.DeserializeObject<Split>(splitJson);

            return _splitParser.Parse(split);
        }

        public async Task<List<ParsedSplit>> GetAllSplitsAsync()
        {
            var pattern = $"{RedisKeyPrefix}{splitKeyPrefix}*";
            var splitKeys = _redisAdapter.Keys(pattern);
            var splitValues = await _redisAdapter.MGetAsync(splitKeys);

            if (splitValues != null && splitValues.Any())
            {
                var splits = splitValues
                    .Where(x => !x.IsNull)
                    .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

                return splits
                    .Where(s => s != null)
                    .ToList();
            }

            return new List<ParsedSplit>();
        }

        public List<string> GetKeys()
        {
            var pattern = $"{RedisKeyPrefix}{splitKeyPrefix}*";
            var splitKeys = _redisAdapter.Keys(pattern);
            var result = splitKeys.Select(x => x.ToString()).ToList();

            return result;
        }

        public void Clear()
        {
            return;
        }

        public async Task<bool> TrafficTypeExistsAsync(string trafficType)
        {
            if (string.IsNullOrEmpty(trafficType)) return false;

            var value = await _redisAdapter.GetAsync(GetTrafficTypeKey(trafficType));

            var quantity = value ?? "0";

            int.TryParse(quantity, out int quantityInt);

            return quantityInt > 0;
        }

        private string GetTrafficTypeKey(string type)
        {
            return $"{RedisKeyPrefix}trafficType.{type}";
        }

        public void AddSplit(string splitName, SplitBase split)
        {
            // No-op
        }

        public bool RemoveSplit(string splitName)
        {
            return false; // No-op
        }

        public void SetChangeNumber(long changeNumber)
        {
            // No-op
        }

        public bool AddOrUpdate(string splitName, SplitBase split)
        {
            return false; // No-op
        }

        public long RemoveSplits(List<string> splitNames)
        {
            return 0;// No-op
        }

        public void Flush()
        {
            // No-op
        }

        public async Task<List<ParsedSplit>> FetchManyAsync(List<string> splitNames)
        {
            if (!splitNames.Any()) return new List<ParsedSplit>();

            var redisKey = new List<RedisKey>();

            foreach (var name in splitNames)
            {
                redisKey.Add($"{RedisKeyPrefix}{splitKeyPrefix}{name}");
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

        public Task KillAsync(long changeNumber, string splitName, string defaultTreatment)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<string>> GetSplitNamesAsync()
        {
            var splits = await GetAllSplitsAsync();
            
            return splits.Select(s => s.name).ToList();
        }

        public Task<int> SplitsCountAsync()
        {
            return Task.FromResult(0); // No-op
        }
    }
}
