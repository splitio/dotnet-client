using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisSplitCache : RedisSplitCacheAsync, ISplitCache
    {
        public RedisSplitCache(IRedisAdapter redisAdapter, ISplitParser splitParser, string userPrefix = null) : base(redisAdapter, splitParser, userPrefix) { }

        #region Producer
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

        public void Kill(long changeNumber, string splitName, string defaultTreatment)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            return;
        }
        #endregion

        #region Consumer
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
            var pattern = $"{RedisKeyPrefix}{SplitKeyPrefix}*";
            var splitKeys = _redisAdapter.Keys(pattern);
            var result = splitKeys.Select(x => x.ToString()).ToList();

            return result;
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
        #endregion
    }
}
 