using Splitio.Services.Evaluator;
using Splitio.Services.Logger;
using Splitio.Services.SemverImp;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public  class BetweenSemverMatcher : BaseMatcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(BetweenSemverMatcher));
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

            var result = keySemver.Compare(_startTarget) >= 0 && keySemver.Compare(_endTarget) <= 0;
            _log.Debug($"{_startTarget} <= {keySemver} <= {_endTarget} | Result: {result}");

            return result;
        }
    }
}
