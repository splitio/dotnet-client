using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class ContainsAllOfSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public ContainsAllOfSetMatcher(WhitelistData whitelistData)
        {
            if (whitelistData.whitelist != null)
            {
                _itemsToCompare.UnionWith(whitelistData.whitelist);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || _itemsToCompare.Count == 0)
            {
                return false;
            }

            return _itemsToCompare.All(i => key.Contains(i));
        }
    }
}