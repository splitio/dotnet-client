using Splitio.Commons.Engine.Evaluator;
using Splitio.Commons.Engine.Matchers.SemverImp;
using System.Collections.Generic;

namespace Splitio.Commons.Engine.Matchers
{
    public class InListSemverMatcher : BaseMatcher
    {
        private readonly List<Semver> _targetList = new List<Semver>();

        public InListSemverMatcher(List<string> list)
        {
            list = list ?? new List<string>();

            foreach (var item in list)
            {
                var semver = Semver.Build(item);

                if (semver != null)
                {
                    _targetList.Add(semver);
                }
            }
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _targetList.Count == 0)
            {
                return false;
            }

            var keySemver = Semver.Build(key);
            if (keySemver == null)
            {
                return false;
            }

            foreach (var item in _targetList)
            {
                if (keySemver.EqualTo(item))
                    return true;
            }

            return false;
        }
    }
}
