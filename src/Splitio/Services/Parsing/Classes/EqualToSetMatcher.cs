using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public class EqualToSetMatcher : BaseMatcher
    {
        private readonly HashSet<string> _itemsToCompare = new HashSet<string>();

        public EqualToSetMatcher(WhitelistData whitelistData)
        {
            if (whitelistData.whitelist != null)
            {
                _itemsToCompare.UnionWith(whitelistData.whitelist);
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