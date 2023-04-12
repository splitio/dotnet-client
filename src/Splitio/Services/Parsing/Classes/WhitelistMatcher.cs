using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing
{
    public class WhitelistMatcher: BaseMatcher
    {
        private readonly List<string> _list;

        public WhitelistMatcher(List<string> list)
        {
            _list = list ?? new List<string>();
        }
        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(_list.Contains(key));
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
    }
}
