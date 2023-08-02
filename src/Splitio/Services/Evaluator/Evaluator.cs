using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Evaluator
{
    public class Evaluator : EvaluatorAsync, IEvaluator
    {
        public Evaluator(ISplitCache splitCache, ISplitter splitter) : base(splitCache, splitter)
        {
        }

        #region Public Method
        public TreatmentResult EvaluateFeature(Key key, string featureName, Dictionary<string, object> attributes = null)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var parsedSplit = _splitCache.GetSplit(featureName);

                    return EvaluateTreatment(key, parsedSplit, featureName, clock, attributes);
                }
                catch (Exception e)
                {
                    return EvaluateFeatureException(e, featureName, clock);
                }
            }
        }

        public MultipleEvaluatorResult EvaluateFeatures(Key key, List<string> featureNames, Dictionary<string, object> attributes = null)
        {
            var exception = false;
            var treatmentsForFeatures = new Dictionary<string, TreatmentResult>();

            using(var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var splits = _splitCache.FetchMany(featureNames);

                    foreach (var feature in featureNames)
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = EvaluateTreatment(key, split, feature, attributes: attributes);

                        treatmentsForFeatures.Add(feature, result);
                    }
                }
                catch (Exception e)
                {
                    exception = EvaluateFeaturesException(e, featureNames, clock, out treatmentsForFeatures);
                }

                return new MultipleEvaluatorResult(treatmentsForFeatures, clock.ElapsedMilliseconds, exception);
            }
        }
        #endregion

        #region Private Methods
        private TreatmentResult EvaluateTreatment(Key key, ParsedSplit parsedSplit, string featureFlagName, Util.SplitStopwatch clock = null, Dictionary<string, object> attributes = null)
        {
            try
            {
                if (clock == null)
                {
                    clock = new Util.SplitStopwatch();
                    clock.Start();
                }

                if (IsSplitNotFound(featureFlagName, parsedSplit, clock, out TreatmentResult resultNotFound)) return resultNotFound;

                var treatmentResult = GetTreatmentResult(key, parsedSplit, attributes);

                return ParseConfigurationAndReturnTreatment(parsedSplit, treatmentResult, clock);
            }
            catch (Exception e)
            {
                return EvaluateFeatureException(e, featureFlagName, clock);
            }
            finally { clock.Dispose(); }
        }

        private TreatmentResult GetTreatmentResult(Key key, ParsedSplit split, Dictionary<string, object> attributes = null)
        {
            if (IsSplitKilled(split, out TreatmentResult result)) return result;

            var inRollout = false;

            // use the first matching condition
            foreach (var condition in split.conditions)
            {
                inRollout = IsInRollout(inRollout, condition, key, split, out TreatmentResult rResult);

                if (rResult != null) return rResult;

                var matched = condition.matcher.Match(key, attributes, this);
                var treatment = IfConditionMatched(matched, key, split, condition);

                if (treatment != null) return treatment;
            }

            return ReturnDefaultTreatment(split);
        }
        #endregion
    }
}
