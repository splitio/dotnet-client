using Splitio.Domain;
using Splitio.Services.Evaluator;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Splitio.Services.Parsing.Classes
{
    public class MatchesStringMatcher : BaseMatcher
    {
        private readonly Regex _regex;

        public MatchesStringMatcher(string pattern)
        {
            _regex = new Regex(pattern);
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return _regex.IsMatch(key);
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return _regex.IsMatch(key.matchingKey);
        }
    }
}
