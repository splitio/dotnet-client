﻿using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Classes
{
    public class EqualToBooleanMatcher : BaseMatcher
    {
        private readonly bool Value;

        public EqualToBooleanMatcher(bool value)
        {
            Value = value;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return key.Equals(Value);
        }

        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (bool.TryParse(key, out bool boolValue))
            {
                return Task.FromResult(Match(boolValue, attributes, evaluator));
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(false);
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
    }
}
