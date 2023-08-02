using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public class EvaluatorAsync : BaseEvaluator, IEvaluatorAsync
    {
        public EvaluatorAsync(ISplitCache splitCache, ISplitter splitter) : base(splitCache, splitter)
        {
        }

        public async Task<TreatmentResult> EvaluateFeatureAsync(Key key, string featureName, Dictionary<string, object> attributes = null)
        {
            using (var clock = new SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var parsedSplit = await _splitCache.GetSplitAsync(featureName);

                    return await EvaluateTreatmentAsync(key, parsedSplit, featureName, clock, attributes);
                }
                catch (Exception e)
                {
                    return EvaluateFeatureException(e, featureName, clock);
                }
            }
        }

        public async Task<MultipleEvaluatorResult> EvaluateFeaturesAsync(Key key, List<string> featureNames, Dictionary<string, object> attributes = null)
        {
            var exception = false;
            var treatmentsForFeatures = new Dictionary<string, TreatmentResult>();

            using (var clock = new SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var splits = await _splitCache.FetchManyAsync(featureNames);

                    foreach (var feature in featureNames)
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = await EvaluateTreatmentAsync(key, split, feature, attributes: attributes);

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

        private async Task<TreatmentResult> EvaluateTreatmentAsync(Key key, ParsedSplit parsedSplit, string featureFlagName, SplitStopwatch clock = null, Dictionary<string, object> attributes = null)
        {
            try
            {
                if (clock == null)
                {
                    clock = new SplitStopwatch();
                    clock.Start();
                }

                if (IsSplitNotFound(featureFlagName, parsedSplit, clock, out TreatmentResult resultNotFound)) return resultNotFound;

                var treatmentResult = await GetTreatmentResultAsync(key, parsedSplit, attributes);

                return ParseConfigurationAndReturnTreatment(parsedSplit, treatmentResult, clock);
            }
            catch (Exception e)
            {
                return EvaluateFeatureException(e, featureFlagName, clock);
            }
            finally { clock.Dispose(); }
        }

        private async Task<TreatmentResult> GetTreatmentResultAsync(Key key, ParsedSplit split, Dictionary<string, object> attributes = null)
        {
            if (IsSplitKilled(split, out TreatmentResult result)) return result;

            var inRollout = false;

            // use the first matching condition
            foreach (var condition in split.conditions)
            {
                inRollout = IsInRollout(inRollout, condition, key, split, out TreatmentResult rResult);

                if (rResult != null) return rResult;

                var matched = await condition.matcher.MatchAsync(key, attributes, this);
                var treatment = IfConditionMatched(matched, key, split, condition);

                if (treatment != null) return treatment;
            }

            return ReturnDefaultTreatment(split);
        }
    }
}
