using Splitio.Domain;
using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class DependencyMatcher : BaseMatcher
    {
        private readonly string _split;
        private readonly List<string> _treatments;

        public DependencyMatcher(string split, List<string> treatments)
        {
            _split = split;
            _treatments = treatments;
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (evaluator == null)
            {
                return false;
            }

            var result = evaluator.EvaluateFeature(key, _split, attributes);

            return _treatments.Contains(result.Treatment);
        }
    }
}
