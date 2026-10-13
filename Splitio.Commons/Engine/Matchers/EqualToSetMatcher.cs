using Splitio.Commons.Engine.Evaluator;
using System.Collections.Generic;

namespace Splitio.Commons.Engine.Matchers
{
    public class EqualToSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public EqualToSetMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                _itemsToCompare.UnionWith(compareTo);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null)
            {
                return false;
            }

            return _itemsToCompare.SetEquals(key);
        }
    }
}