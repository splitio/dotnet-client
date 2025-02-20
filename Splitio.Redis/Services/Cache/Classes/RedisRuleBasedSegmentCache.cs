using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Cache.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisRuleBasedSegmentCache : RedisCacheBase, IRuleBasedSegmentCacheConsumer
    {
        private const string RuleBasedSegmentKeyPrefix = "rbs";
        private string TillKey => $"{RedisKeyPrefix}{RuleBasedSegmentKeyPrefix}.till";

        private readonly IRedisAdapterConsumer _redisAdapter;

        public RedisRuleBasedSegmentCache(IRedisAdapterConsumer redisAdapter,
            RedisConfig redisConfig,
            bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapter = redisAdapter;
        }

        public RuleBasedSegment Get(string name)
        {
            var rbsJSON = _redisAdapter.Get(GetRuleBasedSegmentKey(name));

            return DeserializeAndParse(rbsJSON);
        }

        public long GetChangeNumber()
        {
            var cnString = _redisAdapter.Get(TillKey);
            
            return ParseChangeNumber(cnString);
        }

        public async Task<RuleBasedSegment> GetAsync(string name)
        {
            var rbsJSON = await _redisAdapter.GetAsync(GetRuleBasedSegmentKey(name));

            return DeserializeAndParse(rbsJSON);
        }

        public async Task<long> GetChangeNumberAsync()
        {
            var cnString = await _redisAdapter.GetAsync(TillKey);

            return ParseChangeNumber(cnString);
        }

        private string GetRuleBasedSegmentKey(string name)
        {
            return $"{RedisKeyPrefix}{RuleBasedSegmentKeyPrefix}.{name}";
        }

        private RuleBasedSegment DeserializeAndParse(string rbsJSON)
        {
            if (string.IsNullOrEmpty(rbsJSON))
                return null;

            var ruleBasedSegment = JsonConvert.DeserializeObject<RuleBasedSegmentDTO>(rbsJSON);

            // TODO: implement rbsParser
            //return _rbsParser.Parse(ruleBasedSegment);
            return null;
            // TODO: implement rbsParser
        }

        private static long ParseChangeNumber(string cnString)
        {
            var result = long.TryParse(cnString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }
    }
}
