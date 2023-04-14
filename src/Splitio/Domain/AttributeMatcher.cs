using Splitio.Services.Evaluator;
using Splitio.Services.Parsing;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class AttributeMatcher
    {
        public string Attribute { get; set; }
        public IMatcher Matcher { get; set; }
        public bool Negate { get; set; }

        public virtual bool Match(Key key, Dictionary<string, object> attributes, IEvaluator evaluator = null)
        {
            if (Attribute == null)
            {
                return (Negate ^ Matcher.Match(key, attributes, evaluator));
            }

            if (attributes == null)
            {
                return false;
            }

            attributes.TryGetValue(Attribute, out object value);

            if (value == null)
            {
                return false;
            }

            return (Negate ^ Matcher.Match(value, attributes, evaluator));
        }
    }
}
