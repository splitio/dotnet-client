using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing
{
    public class AllKeysMatcher : BaseMatcher
    {
        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(key != null);
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return true;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return true;
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(key.matchingKey != null);
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
    }
}
