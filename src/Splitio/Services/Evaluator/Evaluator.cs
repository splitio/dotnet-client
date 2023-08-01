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
                    _log.Error($"Exception caught getting treatment for feature flag: {featureName}", e);

                    return new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds, exception: true);
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
                    _log.Error($"Exception caught getting treatments", e);

                    foreach (var name in featureNames)
                    {
                        treatmentsForFeatures.Add(name, new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds));
                    }

                    exception = true;
                }

                return new MultipleEvaluatorResult
                {
                    TreatmentResults = treatmentsForFeatures,
                    ElapsedMilliseconds = clock.ElapsedMilliseconds,
                    Exception = exception
                };
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

                if (parsedSplit == null)
                {
                    _log.Warn($"GetTreatment: you passed {featureFlagName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

                    return new TreatmentResult(Labels.SplitNotFound, Control, elapsedMilliseconds: clock.ElapsedMilliseconds);
                }

                var treatmentResult = GetTreatmentResult(key, parsedSplit, attributes);

                if (parsedSplit.configurations != null && parsedSplit.configurations.ContainsKey(treatmentResult.Treatment))
                {
                    treatmentResult.Config = parsedSplit.configurations[treatmentResult.Treatment];
                }

                treatmentResult.ElapsedMilliseconds = clock.ElapsedMilliseconds;

                return treatmentResult;
            }
            catch (Exception e)
            {
                _log.Error($"Exception caught getting treatment for feature flag: {featureFlagName}", e);

                return new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds);
            }
            finally
            {
                clock.Dispose();
            }
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
                var treatment = IfMatchedGetTreatment(matched, key, split, condition);

                if (treatment != null) return treatment;
            }

            return ReturnDefaultTreatment(split);
        }
        #endregion
    }
}
