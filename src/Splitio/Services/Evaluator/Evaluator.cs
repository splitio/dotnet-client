using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public class Evaluator : IEvaluator
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Evaluator));

        protected const string Control = "control";
        
        private readonly ISplitter _splitter;
        private readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;

        public Evaluator(IFeatureFlagCacheConsumer featureFlagCache,
            ISplitter splitter)
        {
            _featureFlagCacheConsumer = featureFlagCache;
            _splitter = splitter;
        }

        #region Public Sync Methods
        public MultipleEvaluatorResult EvaluateFeatures(Key key, List<string> featureNames, Dictionary<string, object> attributes = null)
        {
            var exception = false;
            var treatmentsForFeatures = new List<TreatmentResult>();

            using (var clock = new SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var splits = _featureFlagCacheConsumer.FetchMany(featureNames);

                    foreach (var feature in featureNames)
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = EvaluateTreatment(key, split, feature, attributes: attributes);

                        treatmentsForFeatures.Add(result);
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

        #region Public Async Methods
        public async Task<MultipleEvaluatorResult> EvaluateFeaturesAsync(Key key, List<string> featureNames, Dictionary<string, object> attributes = null)
        {
            var exception = false;
            var treatmentsForFeatures = new List<TreatmentResult>();

            using (var clock = new SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var splits = await _featureFlagCacheConsumer.FetchManyAsync(featureNames);

                    foreach (var feature in featureNames)
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = await EvaluateTreatmentAsync(key, split, feature, attributes: attributes);

                        treatmentsForFeatures.Add(result);
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

        #region Private Sync Methods
        private TreatmentResult EvaluateTreatment(Key key, ParsedSplit parsedSplit, string featureFlagName, SplitStopwatch clock = null, Dictionary<string, object> attributes = null)
        {
            try
            {
                if (clock == null)
                {
                    clock = new SplitStopwatch();
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

        #region Private Async Methods
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
        #endregion

        #region Private Methods
        private static bool IsSplitKilled(ParsedSplit split, out TreatmentResult result)
        {
            if (split.killed)
            {
                result = new TreatmentResult(split.name, Labels.Killed, split.defaultTreatment, split.changeNumber);
                return true;
            }

            result = null;
            return false;
        }

        private bool IsInRollout(bool inRollout, ConditionWithLogic condition, Key key, ParsedSplit split, out TreatmentResult result)
        {
            result = null;

            if (!inRollout && condition.conditionType == ConditionType.ROLLOUT)
            {
                if (split.trafficAllocation < 100)
                {
                    // bucket ranges from 1-100.
                    var bucket = _splitter.GetBucket(key.bucketingKey, split.trafficAllocationSeed, split.algo);

                    if (bucket > split.trafficAllocation)
                    {
                        result = new TreatmentResult(split.name, Labels.TrafficAllocationFailed, split.defaultTreatment, split.changeNumber);
                    }
                }

                return true;
            }

            return false;
        }

        private static TreatmentResult ReturnDefaultTreatment(ParsedSplit split)
        {
            return new TreatmentResult(split.name, Labels.DefaultRule, split.defaultTreatment, split.changeNumber);
        }

        private TreatmentResult IfConditionMatched(bool matched, Key key, ParsedSplit split, ConditionWithLogic condition)
        {
            if (!matched) return null;

            var treatment = _splitter.GetTreatment(key.bucketingKey, split.seed, condition.partitions, split.algo);

            return new TreatmentResult(split.name, condition.label, treatment, split.changeNumber);
        }

        private static TreatmentResult EvaluateFeatureException(Exception e, string featureName, SplitStopwatch clock)
        {
            _log.Error($"Exception caught getting treatment for feature flag: {featureName}", e);

            return new TreatmentResult(featureName, Labels.Exception, Control, exception: true);
        }

        private static bool EvaluateFeaturesException(Exception e, List<string> featureNames, SplitStopwatch clock, out List<TreatmentResult> results)
        {
            results = new List<TreatmentResult>();

            _log.Error($"Exception caught getting treatments", e);

            foreach (var name in featureNames)
            {
                results.Add(new TreatmentResult(name, Labels.Exception, Control));
            }

            return true;
        }

        private static bool IsSplitNotFound(string featureFlagName, ParsedSplit parsedSplit, SplitStopwatch clock, out TreatmentResult result)
        {
            result = null;

            if (parsedSplit != null)
                return false;

            _log.Warn($"GetTreatment: you passed {featureFlagName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

            result = new TreatmentResult(featureFlagName, Labels.SplitNotFound, Control);

            return true;
        }

        private static TreatmentResult ParseConfigurationAndReturnTreatment(ParsedSplit parsedSplit, TreatmentResult treatmentResult, SplitStopwatch clock)
        {
            if (parsedSplit.configurations != null && parsedSplit.configurations.ContainsKey(treatmentResult.Treatment))
            {
                treatmentResult.Config = parsedSplit.configurations[treatmentResult.Treatment];
            }

            return treatmentResult;
        }
        #endregion
    }
}
