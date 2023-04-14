using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class StartsWithMatcher : BaseMatcher
    {
        private readonly HashSet<string> itemsToCompare = new HashSet<string>();

        public StartsWithMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                itemsToCompare.UnionWith(compareTo);
            }
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return itemsToCompare.Any(i => key.StartsWith(i));
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}