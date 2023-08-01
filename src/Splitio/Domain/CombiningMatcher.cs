using Splitio.Services.Evaluator;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Domain
{
    public class CombiningMatcher
    {
        public CombinerEnum combiner { get; set; }
        public List<AttributeMatcher> delegates { get; set; }
        
        public virtual bool Match(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (delegates == null || delegates.Count() == 0)
            {
                return false;
            }

            switch (combiner)
            {
                case CombinerEnum.AND:
                default:
                    return delegates.All(matcher => matcher.Match(key, attributes, evaluator));
            }
        }

        public virtual async Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes, IEvaluatorAsync evaluator = null)
        {
            if (delegates == null || delegates.Count() == 0)
            {
                return false;
            }

            switch (combiner)
            {
                case CombinerEnum.AND:
                default:
                    foreach (var matcher in delegates)
                    {
                        var matched = await matcher.MatchAsync(key, attributes, evaluator);

                        if (!matched) return false;
                    }

                    return true;
            }
        }
    }
}
