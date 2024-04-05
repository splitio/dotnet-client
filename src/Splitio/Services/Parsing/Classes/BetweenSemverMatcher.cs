using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public  class BetweenSemverMatcher : BaseMatcher
    {
        private readonly Semver _startTarget;
        private readonly Semver _endTarget;

        public BetweenSemverMatcher(string start, string end)
        {
            _startTarget = Semver.Build(start);
            _endTarget = Semver.Build(end);
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _startTarget == null || _endTarget == null)
            {
                return false;
            }

            var keySemver = Semver.Build(key);
            if (keySemver == null)
            {
                return false;
            }

            return keySemver.Between(_startTarget, _endTarget);
        }
    }
}
