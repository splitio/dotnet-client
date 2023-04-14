using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public abstract class CompareMatcher : BaseMatcher
    {
        protected DataTypeEnum? dataType;
        protected long value;
        protected long start;
        protected long end;

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            switch (dataType)
            {
                case DataTypeEnum.DATETIME:
                    var date = key.ToDateTime();
                    return date.HasValue && Match(date.Value);
                case DataTypeEnum.NUMBER:
                    var result = long.TryParse(key, out long number);
                    return result && Match(number);
                default:
                    return false;
            }
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}
