using Splitio.Services.Evaluator;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public class InListSemverMatcher : BaseMatcher
    {
        private readonly List<string> _list;

        public InListSemverMatcher(List<string> list)
        {
            _list = list;
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _list == null)
            {
                return false;
            }

            return _list.Contains(key);
        }
    }
}
