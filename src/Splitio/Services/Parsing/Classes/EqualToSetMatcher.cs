using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public class EqualToSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> itemsToCompare = new HashSet<string>();

        public EqualToSetMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                itemsToCompare.UnionWith(compareTo);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null)
            {
                return false;
            }

            return itemsToCompare.SetEquals(key);
        }
    }
}