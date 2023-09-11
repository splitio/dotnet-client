using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Util;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Evaluator
{
    public class BaseEvaluator
    {
        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Evaluator));

        protected const string Control = "control";

        protected readonly ISplitter _splitter;
        protected readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;

        public BaseEvaluator(IFeatureFlagCacheConsumer featureFlagCacheConsumer,
            ISplitter splitter)
        {
            _featureFlagCacheConsumer = featureFlagCacheConsumer;
            _splitter = splitter;
        }

        protected bool IsSplitKilled(ParsedSplit split, out TreatmentResult result)
        {
            if (split.killed)
            {
                result = new TreatmentResult(Labels.Killed, split.defaultTreatment, split.changeNumber);
                return true;
            }

            result = null;
            return false;
        }

        protected bool IsInRollout(bool inRollout, ConditionWithLogic condition, Key key, ParsedSplit split, out TreatmentResult result)
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
                        result = new TreatmentResult(Labels.TrafficAllocationFailed, split.defaultTreatment, split.changeNumber);
                    }
                }

                return true;
            }

            return false;
        }

        protected TreatmentResult ReturnDefaultTreatment(ParsedSplit split)
        {
            return new TreatmentResult(Labels.DefaultRule, split.defaultTreatment, split.changeNumber);
        }

        protected TreatmentResult IfConditionMatched(bool matched, Key key, ParsedSplit split, ConditionWithLogic condition)
        {
            if (!matched) return null;

            var treatment = _splitter.GetTreatment(key.bucketingKey, split.seed, condition.partitions, split.algo);

            return new TreatmentResult(condition.label, treatment, split.changeNumber);
        }

        protected TreatmentResult EvaluateFeatureException(Exception e, string featureName, SplitStopwatch clock)
        {
            _log.Error($"Exception caught getting treatment for feature flag: {featureName}", e);

            return new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds, exception: true);
        }

        protected bool EvaluateFeaturesException(Exception e, List<string> featureNames, SplitStopwatch clock, out Dictionary<string, TreatmentResult> results)
        {
            results = new Dictionary<string, TreatmentResult>();

            _log.Error($"Exception caught getting treatments", e);

            foreach (var name in featureNames)
            {
                results.Add(name, new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds));
            }

            return true;
        }

        protected bool IsSplitNotFound(string featureFlagName, ParsedSplit parsedSplit, SplitStopwatch clock, out TreatmentResult result)
        {
            result = null;

            if (parsedSplit != null)
                return false;

            _log.Warn($"GetTreatment: you passed {featureFlagName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

            result =  new TreatmentResult(Labels.SplitNotFound, Control, elapsedMilliseconds: clock.ElapsedMilliseconds);

            return true;
        }

        protected TreatmentResult ParseConfigurationAndReturnTreatment(ParsedSplit parsedSplit, TreatmentResult treatmentResult, SplitStopwatch clock)
        {
            if (parsedSplit.configurations != null && parsedSplit.configurations.ContainsKey(treatmentResult.Treatment))
            {
                treatmentResult.Config = parsedSplit.configurations[treatmentResult.Treatment];
            }

            treatmentResult.ElapsedMilliseconds = clock.ElapsedMilliseconds;

            return treatmentResult;
        }
    }
}
