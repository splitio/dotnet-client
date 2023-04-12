using Splitio.Services.Evaluator;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Domain
{
    public class CombiningMatcher
    {
        public CombinerEnum Combiner { get; set; }
        public List<AttributeMatcher> Delegates { get; set; }
        
        public virtual bool Match(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (Delegates == null || Delegates.Count() == 0)
            {
                return false;
            }

            switch (Combiner)
            {
                case CombinerEnum.AND:
                default:
                    return Delegates.All(matcher => matcher.Match(key, attributes, evaluator).Result);
            }
        }
    }
}
