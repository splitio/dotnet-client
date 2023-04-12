using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing
{
    public interface IMatcher
    {
        Task<bool> Match(object value, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);

        bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null);
    }
}
