using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public class LessOrEqualToMatcher : CompareMatcher
    {
        public LessOrEqualToMatcher(UnaryNumericData unaryNumericData)
        {
            _dataType = unaryNumericData.dataType;
            _value = unaryNumericData.value;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return key <= _value;
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            var date = _value.ToDateTime();
            key = key.Truncate(TimeSpan.FromMinutes(1)); // Truncate to whole minute
            return key.ToUniversalTime() <= date.ToUniversalTime();
        }
    }
}
