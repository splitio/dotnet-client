using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Interfaces
{
    public interface IMatcherAsync
    {
        Task<bool> MatchAsync(object value, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(string key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(DateTime key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(long key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(List<string> key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);

        Task<bool> MatchAsync(bool key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null);
    }
}
