using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class PartOfSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> itemsToCompare = new HashSet<string>();

        public PartOfSetMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                itemsToCompare.UnionWith(compareTo);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || key.Count == 0)
            {
                return false;
            }

            return key.All(k => itemsToCompare.Contains(k));
        }
    }
}