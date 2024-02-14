using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class ContainsStringMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public ContainsStringMatcher(WhitelistData whitelistData)
        {
            if (whitelistData.whitelist != null)
            {
                _itemsToCompare.UnionWith(whitelistData.whitelist);
            }
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _itemsToCompare.Any(i => key.Contains(i));
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}