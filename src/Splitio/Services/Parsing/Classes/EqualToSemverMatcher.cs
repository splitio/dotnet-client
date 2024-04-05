using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class EqualToSemverMatcher : BaseMatcher
    {
        private readonly string _target;

        public EqualToSemverMatcher(string target)
        {
            _target = target;
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _target == null)
            {
                return false;
            }

            return key == _target;
        }
    }
}
