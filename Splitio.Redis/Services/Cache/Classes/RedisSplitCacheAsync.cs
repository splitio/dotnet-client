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
    public class RedisSplitCacheAsync : RedisCacheBase, ISplitCacheAsync
    {
        protected const string SplitKeyPrefix = "split.";
        protected const string SplitsKeyPrefix = "splits.";

        protected readonly ISplitParser _splitParser;

        public RedisSplitCacheAsync(IRedisAdapter redisAdapter, ISplitParser splitParser, string userPrefix = null) : base(redisAdapter, userPrefix)
        {
            _splitParser = splitParser;
        }

        #region Producer
        public Task<bool> AddOrUpdateAsync(string splitName, SplitBase split)
        {
            return Task.FromResult(false);
        }

        public Task AddSplitAsync(string splitName, SplitBase split)
        {
            return Task.FromResult(false);
        }

        public Task ClearAsync()
        {
            return Task.FromResult(false);
        }

        public Task KillAsync(long changeNumber, string splitName, string defaultTreatment)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> RemoveSplitAsync(string splitName)
        {
            return Task.FromResult(false);
        }

        public Task SetChangeNumberAsync(long changeNumber)
        {
            return Task.FromResult(false);
        }
        #endregion

        #region Consumer
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
            var splitKeys = _redisAdapter.Keys(pattern);
            var splitValues = await _redisAdapter.MGetAsync(splitKeys);

            if (splitValues == null || !splitValues.Any()) return new List<ParsedSplit>();

            var splits = splitValues
                .Where(x => !x.IsNull)
                .Select(s => _splitParser.Parse(JsonConvert.DeserializeObject<Split>(s)));

            return splits
                .Where(s => s != null)
                .ToList();
        }

        public async Task<long> GetChangeNumberAsync()
        {
            var key = $"{RedisKeyPrefix}{SplitsKeyPrefix}till";
            var changeNumberString = await _redisAdapter.GetAsync(key);
            var result = long.TryParse(changeNumberString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
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

        public Task<int> SplitsCountAsync()
        {
            return Task.FromResult(0);
        }

        public async Task<bool> TrafficTypeExistsAsync(string trafficType)
        {
            if (string.IsNullOrEmpty(trafficType)) return false;

            var value = await _redisAdapter.GetAsync(GetTrafficTypeKey(trafficType));

            var quantity = value ?? "0";

            int.TryParse(quantity, out int quantityInt);

            return quantityInt > 0;
        }
        #endregion

        #region Protected Methods
        protected string GetTrafficTypeKey(string type)
        {
            return $"{RedisKeyPrefix}trafficType.{type}";
        }
        #endregion
    }
}
