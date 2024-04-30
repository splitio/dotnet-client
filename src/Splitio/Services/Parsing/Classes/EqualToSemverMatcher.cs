using Splitio.Services.Evaluator;
using Splitio.Services.Logger;
using Splitio.Services.SemverImp;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class EqualToSemverMatcher : BaseMatcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(EqualToSemverMatcher));
        private readonly Semver _target;

        public EqualToSemverMatcher(string target)
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
            
            var result = keySemver.EqualTo(_target);
            _log.Debug($"{keySemver.Version} == {_target.Version} | Result: {result}");

            return result;
        }
    }
}
