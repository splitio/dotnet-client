﻿using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Classes
{
    public class DependencyMatcher : BaseMatcher
    {
        private readonly string Split;
        private readonly List<string> Treatments;

        public DependencyMatcher(string split, List<string> treatments)
        {
            Split = split;
            Treatments = treatments;
        }

        public override async Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (evaluator == null)
            {
                return false;
            }

            var result = await evaluator.EvaluateFeatureAsync(key, Split, attributes);

            return Treatments.Contains(result.Treatment);
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

        public override Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Task.FromResult(false);
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
    }
}
