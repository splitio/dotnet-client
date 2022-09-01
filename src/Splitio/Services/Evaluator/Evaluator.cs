using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Evaluator
{
    public class Evaluator : IEvaluator
    {
        protected const string Control = "control";

        private readonly ISplitter _splitter;
        private readonly ISplitLogger _log;
        private readonly ISplitCache _splitCache;

        public Evaluator(ISplitCache splitCache,
            ISplitter splitter)
        {
            _splitCache = splitCache;
            _splitter = splitter;
            _log = WrapperAdapter.Instance().GetLogger(typeof(Evaluator));
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
                    _log.Error($"Exception caught getting treatment for feature: {featureName}", e);

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
        private TreatmentResult EvaluateTreatment(Key key, ParsedSplit parsedSplit, string featureName, Util.SplitStopwatch clock = null, Dictionary<string, object> attributes = null)
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
                    _log.Warn($"GetTreatment: you passed {featureName} that does not exist in this environment, please double check what Splits exist in the web console.");

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
                _log.Error($"Exception caught getting treatment for feature: {featureName}", e);

                return new TreatmentResult(Labels.Exception, Control, elapsedMilliseconds: clock.ElapsedMilliseconds);
            }
            finally
            {
                clock.Dispose();
            }
        }

        private TreatmentResult GetTreatmentResult(Key key, ParsedSplit split, Dictionary<string, object> attributes = null)
        {
            if (split.killed)
            {
                return new TreatmentResult(Labels.Killed, split.defaultTreatment, split.changeNumber);
            }

            var inRollout = false;

            // use the first matching condition
            foreach (var condition in split.conditions)
            {
                if (!inRollout && condition.conditionType == ConditionType.ROLLOUT)
                {
                    if (split.trafficAllocation < 100)
                    {
                        // bucket ranges from 1-100.
                        var bucket = _splitter.GetBucket(key.bucketingKey, split.trafficAllocationSeed, split.algo);

                        if (bucket > split.trafficAllocation)
                        {
                            return new TreatmentResult(Labels.TrafficAllocationFailed, split.defaultTreatment, split.changeNumber);
                        }
                    }

                    inRollout = true;
                }

                var combiningMatcher = condition.matcher;

                if (combiningMatcher.Match(key, attributes, this))
                {
                    var treatment = _splitter.GetTreatment(key.bucketingKey, split.seed, condition.partitions, split.algo);

                    return new TreatmentResult(condition.label, treatment, split.changeNumber);
                }
            }

            return new TreatmentResult(Labels.DefaultRule, split.defaultTreatment, split.changeNumber);   
        }
        #endregion
    }
}
