using Splitio.Commons.Dto;
using Splitio.Commons.Engine.Evaluator;
using Splitio.Commons.Shared.Utils;
using System;
using System.Collections.Generic;

namespace Splitio.Commons.Engine.Matchers
{
    public class EqualToMatcher : CompareMatcher
    {
        public EqualToMatcher(DataTypeEnum? dataType, long value)
        {
            _dataType = dataType;
            _value = value;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (_dataType == DataTypeEnum.DATETIME)
            {
                return Match(key.ToDateTime(), attributes, evaluator);
            }

            return _value == key;
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            var date = _value.ToDateTime();

            return date.ToUniversalTime().Date == key.ToUniversalTime().Date; // Compare just date part
        }
    }
}
