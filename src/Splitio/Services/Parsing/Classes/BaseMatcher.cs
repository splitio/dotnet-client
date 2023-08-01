using Splitio.Domain;
using Splitio.Services.Evaluator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Classes
{
    public abstract class BaseMatcher : IMatcher
    {
        #region Sync Methods
        public bool Match(object value, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (value is bool)
            {
                return Match((bool)value, attributes, evaluator);
            }
            else if (value is string)
            {
                return Match((string)value, attributes, evaluator);
            }
            else if (value is DateTime)
            {
                return Match((DateTime)value, attributes, evaluator);
            }
            else if (value is long)
            {
                return Match((long)value, attributes, evaluator);
            }
            else if (value is int)
            {
                return Match((int)value, attributes, evaluator);
            }
            else if (value is List<string>)
            {
                return Match((List<string>)value, attributes, evaluator);
            }
            else if (value is Key)
            {
                return Match((Key)value, attributes, evaluator);
            }

            return false;
        }

        public virtual bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public virtual bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public virtual bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public virtual bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public virtual bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public virtual bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
        #endregion

        #region Async Methods
        public Task<bool> MatchAsync(object value, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            if (value is bool bValue)
            {
                return MatchAsync(bValue, attributes, evaluator);
            }
            if (value is string sValue)
            {
                return MatchAsync(sValue, attributes, evaluator);
            }
            
            if (value is DateTime dValue)
            {
                return MatchAsync(dValue, attributes, evaluator);
            }
            
            if (value is long lValue)
            {
                return MatchAsync(lValue, attributes, evaluator);
            }
            
            if (value is int iValue)
            {
                return MatchAsync(iValue, attributes, evaluator);
            }
            
            if (value is List<string> listValue)
            {
                return MatchAsync(listValue, attributes, evaluator);
            }
            
            if (value is Key kValue)
            {
                return MatchAsync(kValue, attributes, evaluator);
            }

            return Task.FromResult(false);
        }

        public virtual Task<bool> MatchAsync(string key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }

        public virtual Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }

        public virtual Task<bool> MatchAsync(DateTime key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }

        public virtual Task<bool> MatchAsync(long key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }

        public virtual Task<bool> MatchAsync(List<string> key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }

        public virtual Task<bool> MatchAsync(bool key, Dictionary<string, object> attributes = null, IEvaluatorAsync evaluator = null)
        {
            return Task.FromResult(Match(key, attributes));
        }
        #endregion
    }
}