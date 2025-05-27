using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class EqualToBooleanMatcher : BaseMatcher
    {
        private readonly bool _value;

        public EqualToBooleanMatcher(bool value)
        {
            _value = value;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return key.Equals(_value);
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (bool.TryParse(key, out bool boolValue))
            {
                return Match(boolValue, attributes, evaluator);
            }
            else
            {
                return false;
            }
        }
    }
}
