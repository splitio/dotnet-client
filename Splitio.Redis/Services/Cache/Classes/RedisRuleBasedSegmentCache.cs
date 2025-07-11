using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisRuleBasedSegmentCache : RedisCacheBase, IRuleBasedSegmentCacheConsumer
    {
        private const string RuleBasedSegmentKeyPrefix = "rbsegment";
        private string TillKey => $"{RedisKeyPrefix}rbsegments.till";

        private readonly IRedisAdapterConsumer _redisAdapter;
        private readonly IParser<RuleBasedSegmentDto, RuleBasedSegment> _rbsParser;

        public RedisRuleBasedSegmentCache(IRedisAdapterConsumer redisAdapter,
            IParser<RuleBasedSegmentDto, RuleBasedSegment> rbsParser,
            RedisConfig redisConfig,
            bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapter = redisAdapter;
            _rbsParser = rbsParser;
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

        private string GetRuleBasedSegmentKey(string name)
        {
            return $"{RedisKeyPrefix}{RuleBasedSegmentKeyPrefix}.{name}";
        }

        private RuleBasedSegment DeserializeAndParse(string rbsJSON)
        {
            if (string.IsNullOrEmpty(rbsJSON))
                return null;

            var rbsDto = JsonConvertWrapper.DeserializeObject<RuleBasedSegmentDto>(rbsJSON);

            return _rbsParser.Parse(rbsDto, this);
        }

        private static long ParseChangeNumber(string cnString)
        {
            var result = long.TryParse(cnString, out long changeNumberParsed);

            return result ? changeNumberParsed : -1;
        }

        public bool Contains(List<string> names)
        {
            // no-op
            return true;
        }
    }
}
