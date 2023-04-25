using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public class BetweenMatcher : CompareMatcher
    {

        public BetweenMatcher(DataTypeEnum? dataType, long start, long end)
        {
            _dataType = dataType;
            _start = start;
            _end = end;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return (_start <= key) && (key <= _end);         
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            var startDate = _start.ToDateTime();
            var endDate = _end.ToDateTime();
            key = key.Truncate(TimeSpan.FromMinutes(1)); // Truncate to whole minute
            return (startDate.ToUniversalTime() <= key.ToUniversalTime()) && (key.ToUniversalTime() <= endDate.ToUniversalTime());
        }
    }
}
