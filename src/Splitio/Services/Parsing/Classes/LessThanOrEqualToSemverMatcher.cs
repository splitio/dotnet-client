using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class LessThanOrEqualToSemverMatcher : BaseMatcher
    {
        private readonly Semver _target;

        public LessThanOrEqualToSemverMatcher(string target)
        {
            _target = Semver.Build(target);
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _target == null)
            {
                return false;
            }

            var keySemver = Semver.Build(key);
            if (keySemver == null)
            {
                return false;
            }

            return keySemver.LessThanOrEqualTo(_target);
        }
    }
}
