using Splitio.Domain;
using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class DependencyMatcher : BaseMatcher
    {
        private readonly string Split;
        private readonly List<string> Treatments;

        public DependencyMatcher(string split, List<string> treatments)
        {
            Split = split;
            Treatments = treatments;
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (evaluator == null)
            {
                return false;
            }

            var result = evaluator.EvaluateFeature(key, Split, attributes);

            return Treatments.Contains(result.Treatment);
        }
    }
}
