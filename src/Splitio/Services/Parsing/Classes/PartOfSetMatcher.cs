using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class PartOfSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public PartOfSetMatcher(WhitelistData whitelistData)
        {
            if (whitelistData.whitelist != null)
            {
                _itemsToCompare.UnionWith(whitelistData.whitelist);
            }
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (key == null || key.Count == 0)
            {
                return false;
            }

            return key.All(k => _itemsToCompare.Contains(k));
        }
    }
}