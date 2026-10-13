using Splitio.Commons.Engine.Evaluator;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Commons.Engine.Matchers
{
    public class ContainsAnyOfSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public ContainsAnyOfSetMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                _itemsToCompare.UnionWith(compareTo);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _itemsToCompare.Count == 0)
            {
                return false;
            }

            return _itemsToCompare.Any(i => key.Contains(i));
        }
    }
}