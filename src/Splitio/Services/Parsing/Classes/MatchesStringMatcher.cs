using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Classes
{
    public class MatchesStringMatcher : BaseMatcher
    {
        private readonly Regex regex;

        public MatchesStringMatcher(string pattern)
        {
            regex = new Regex(pattern);
        }

        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(regex.IsMatch(key));
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(regex.IsMatch(key.matchingKey));
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
