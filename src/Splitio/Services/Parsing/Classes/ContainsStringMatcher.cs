using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing
{
    public class ContainsStringMatcher : BaseMatcher
    {
        private readonly HashSet<string> itemsToCompare = new HashSet<string>();

        public ContainsStringMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                itemsToCompare.UnionWith(compareTo);
            }
        }

        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(itemsToCompare.Any(i => key.Contains(i)));
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
    }
}