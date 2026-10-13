using Splitio.Commons.Dto;
using Splitio.Commons.Engine.Evaluator;
using System.Collections.Generic;

namespace Splitio.Commons.Engine.Matchers
{
    public class WhitelistMatcher: BaseMatcher
    {
        private readonly List<string> _list;

        public WhitelistMatcher(List<string> list)
        {
            _list = list ?? new List<string>();
        }
        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return _list.Contains(key);
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}
