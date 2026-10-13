using Splitio.Commons.Dto;
using Splitio.Commons.Engine.Evaluator;
using Splitio.Commons.Shared.Utils;
using System.Collections.Generic;

namespace Splitio.Commons.Engine.Matchers
{
    public abstract class CompareMatcher : BaseMatcher
    {
        protected DataTypeEnum? _dataType;
        protected long _value;
        protected long _start;
        protected long _end;

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            switch (_dataType)
            {
                case DataTypeEnum.DATETIME:
                    var date = key.ToDateTime();
                    return date != null && Match(date.Value);
                case DataTypeEnum.NUMBER:
                    long number;
                    var result = long.TryParse(key, out number);
                    return result && Match(number);
                default: return false;
            }
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}
