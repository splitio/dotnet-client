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

        public virtual async Task<bool> Match(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (attribute == null)
            {
                return (negate ^ await matcher.Match(key, attributes, evaluator));
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

            return (negate ^ await matcher.Match(value, attributes, evaluator));
        }
    }
}
