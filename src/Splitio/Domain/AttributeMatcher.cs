using Splitio.Services.Evaluator;
using Splitio.Services.Parsing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Domain
{
    public class AttributeMatcher
    {
        public string attribute { get; set; }
        public IMatcher matcher { get; set; }
        public bool negate { get; set; }

        public virtual bool Match(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (attribute == null)
            {
                return (negate ^ matcher.Match(key, attributes, evaluator));
            }

            if (attributes == null)
            {
                return false;
            }

            attributes.TryGetValue(attribute, out object value);

            if (value == null)
            {
                return false;
            }

            return (negate ^ matcher.Match(value, attributes, evaluator));
        }

        public virtual async Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (attribute == null)
            {
                return (negate ^ await matcher.MatchAsync(key, attributes, evaluator));
            }

            if (attributes == null)
            {
                return false;
            }

            attributes.TryGetValue(attribute, out object value);

            if (value == null)
            {
                return false;
            }

            return (negate ^ await matcher.MatchAsync(value, attributes, evaluator));
        }
    }
}
