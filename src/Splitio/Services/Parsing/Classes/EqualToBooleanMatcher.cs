using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class EqualToBooleanMatcher : BaseMatcher
    {
        private readonly bool Value;

        public EqualToBooleanMatcher(bool value)
        {
            Value = value;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return key.Equals(Value);
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (bool.TryParse(key, out bool boolValue))
            {
                return Match(boolValue, attributes, evaluator);
            }
            
            return false;
        }
    }
}
