using Splitio.Domain;
using Splitio.Services.Evaluator;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var evaluatorResult = evaluator.EvaluateFeatures(key, new List<string> { _split }, attributes);
            var result = evaluatorResult.Results.FirstOrDefault();

            return _treatments.Contains(result.Treatment);
        }

        public override async Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (evaluator == null)
            {
                return false;
            }

            var evaluatorResult = await evaluator.EvaluateFeaturesAsync(key, new List<string> { _split }, attributes);
            var result = evaluatorResult.Results.FirstOrDefault();

            return _treatments.Contains(result.Treatment);
        }
    }
}
