using Splitio.Domain;
using Splitio.Enums;
using Splitio.Enums.Extensions;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using Splitio.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public class Evaluator : IEvaluator
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Evaluator));
        
        private readonly ISplitter _splitter;
        private readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private readonly ITelemetryEvaluationProducer _telemetryEvaluationProducer;

        public Evaluator(IFeatureFlagCacheConsumer featureFlagCache,
            ISplitter splitter,
            ITelemetryEvaluationProducer telemetryEvaluationProducer)
        {
            _featureFlagCacheConsumer = featureFlagCache;
            _splitter = splitter;
            _telemetryEvaluationProducer = telemetryEvaluationProducer;
        }

        #region Public Sync Methods
        public List<TreatmentResult> EvaluateFeatures(API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true)
        {
            var treatmentsForFeatures = new List<TreatmentResult>();

            try
            {
                var clock = new Stopwatch();
                clock.Start();

                var splits = _featureFlagCacheConsumer.FetchMany(featureNames);

                foreach (var feature in featureNames)
                {
                    try
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = EvaluateTreatment(method, key, split, feature, attributes: attributes);

                        treatmentsForFeatures.Add(result);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"{method}: Something went wrong evaluation feature: {feature}", e);

                        _telemetryEvaluationProducer?.RecordException(method.ConvertToMethodEnum());
                        treatmentsForFeatures.Add(new TreatmentResult(feature, Labels.Exception, Constants.Gral.Control));
                    }
                }

                clock.Stop();
                
                if (trackLatency)
                    _telemetryEvaluationProducer?.RecordLatency(method.ConvertToMethodEnum(), Metrics.Bucket(clock.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                treatmentsForFeatures = EvaluateFeaturesException(ex, featureNames);

                _telemetryEvaluationProducer?.RecordException(method.ConvertToMethodEnum());
            }

            return treatmentsForFeatures;
        }

        public List<TreatmentResult> EvaluateFeaturesByFlagSets(API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var evaluations = new List<TreatmentResult>();

            try
            {
                var clock = new Stopwatch();
                clock.Start();

                var flagSetsWithNames = GetFeatureFlagNamesByFlagSets(method, flagSets);

                evaluations = EvaluateFeatures(method, key, flagSetsWithNames, attributes, false);

                clock.Stop();

                _telemetryEvaluationProducer?.RecordLatency(method.ConvertToMethodEnum(), Metrics.Bucket(clock.ElapsedMilliseconds));
            }
            catch
            {
                _telemetryEvaluationProducer?.RecordException(method.ConvertToMethodEnum());
            }

            return evaluations;
        }
        #endregion

        #region Public Async Methods
        public async Task<List<TreatmentResult>> EvaluateFeaturesAsync(API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true)
        {
            var treatmentsForFeatures = new List<TreatmentResult>();

            try
            {
                var clock = new Stopwatch();
                clock.Start();

                var splits = await _featureFlagCacheConsumer.FetchManyAsync(featureNames);

                foreach (var feature in featureNames)
                {
                    try
                    {
                        var split = splits.FirstOrDefault(s => feature.Equals(s?.name));

                        var result = await EvaluateTreatmentAsync(method, key, split, feature, attributes: attributes);

                        treatmentsForFeatures.Add(result);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"{method}: Something went wrong evaluation feature: {feature}", e);

                        if (_telemetryEvaluationProducer != null)
                            await _telemetryEvaluationProducer.RecordExceptionAsync(method.ConvertToMethodEnum());

                        treatmentsForFeatures.Add(new TreatmentResult(feature, Labels.Exception, Constants.Gral.Control));
                    }
                }

                clock.Stop();

                if (trackLatency && _telemetryEvaluationProducer != null)
                    await _telemetryEvaluationProducer.RecordLatencyAsync(method.ConvertToMethodEnum(), Metrics.Bucket(clock.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                treatmentsForFeatures = EvaluateFeaturesException(ex, featureNames);

                if (_telemetryEvaluationProducer != null)
                    await _telemetryEvaluationProducer.RecordExceptionAsync(method.ConvertToMethodEnum());
            }

            return treatmentsForFeatures;
        }

        public async Task<List<TreatmentResult>> EvaluateFeaturesByFlagSetsAsync(API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null)
        {
            var evaluations = new List<TreatmentResult>();

            try
            {
                var clock = new Stopwatch();

                clock.Start();

                var flagSetsWithNames = await GetFeatureFlagNamesByFlagSetsAsync(method, flagSets);

                evaluations = await EvaluateFeaturesAsync(method, key, flagSetsWithNames, attributes, false);

                clock.Stop();

                if (_telemetryEvaluationProducer != null)
                    await _telemetryEvaluationProducer.RecordLatencyAsync(method.ConvertToMethodEnum(), Metrics.Bucket(clock.ElapsedMilliseconds));
            }
            catch
            {
                if (_telemetryEvaluationProducer != null)
                    await _telemetryEvaluationProducer.RecordExceptionAsync(method.ConvertToMethodEnum());
            }

            return evaluations;
        }
        #endregion

        #region Private Sync Methods
        private TreatmentResult EvaluateTreatment(API method, Key key, ParsedSplit parsedSplit, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            try
            {
                if (IsSplitNotFound(method, featureFlagName, parsedSplit, out TreatmentResult resultNotFound)) return resultNotFound;

                var treatmentResult = GetTreatmentResult(key, parsedSplit, attributes);

                return ParseConfigurationAndReturnTreatment(parsedSplit, treatmentResult);
            }
            catch (Exception e)
            {
                return EvaluateFeatureException(e, featureFlagName);
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
                var treatment = IfConditionMatched(matched, key, split, condition);

                if (treatment != null) return treatment;
            }

            return ReturnDefaultTreatment(split);
        }

        private List<string> GetFeatureFlagNamesByFlagSets(API method, List<string> flagSets)
        {
            var namesByFlagSets = _featureFlagCacheConsumer.GetNamesByFlagSets(flagSets);

            return GetAndValidateFeatureFlagNamesByFlagSets(method, namesByFlagSets);
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

        private TreatmentResult IfConditionMatched(bool matched, Key key, ParsedSplit split, ConditionWithLogic condition)
        {
            if (!matched) return null;

            var treatment = _splitter.GetTreatment(key.bucketingKey, split.seed, condition.partitions, split.algo);

            return new TreatmentResult(split.name, condition.label, treatment, split.changeNumber);
        }
        #endregion

        #region Private Async Methods
        private async Task<TreatmentResult> EvaluateTreatmentAsync(API method, Key key, ParsedSplit parsedSplit, string featureFlagName, Dictionary<string, object> attributes = null)
        {
            try
            {
                if (IsSplitNotFound(method, featureFlagName, parsedSplit, out TreatmentResult resultNotFound)) return resultNotFound;

                var treatmentResult = await GetTreatmentResultAsync(key, parsedSplit, attributes);

                return ParseConfigurationAndReturnTreatment(parsedSplit, treatmentResult);
            }
            catch (Exception e)
            {
                return EvaluateFeatureException(e, featureFlagName);
            }
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

        private async Task<List<string>> GetFeatureFlagNamesByFlagSetsAsync(API method, List<string> flagSets)
        {
            var namesByFlagSets = await _featureFlagCacheConsumer.GetNamesByFlagSetsAsync(flagSets);

            return GetAndValidateFeatureFlagNamesByFlagSets(method, namesByFlagSets);
        }
        #endregion

        #region Private Statics Methods
        private static bool IsSplitKilled(ParsedSplit split, out TreatmentResult result)
        {
            result = null;

            if (split.killed)
            {
                result = new TreatmentResult(split.name, Labels.Killed, split.defaultTreatment, split.changeNumber);
                return true;
            }

            return false;
        }

        private static TreatmentResult ReturnDefaultTreatment(ParsedSplit split)
        {
            return new TreatmentResult(split.name, Labels.DefaultRule, split.defaultTreatment, split.changeNumber);
        }

        private TreatmentResult EvaluateFeatureException(Exception e, string featureName)
        {
            _log.Error($"Exception caught getting treatment for feature flag: {featureName}", e);

            return new TreatmentResult(featureName, Labels.Exception, Constants.Gral.Control, exception: true);
        }

        private List<TreatmentResult> EvaluateFeaturesException(Exception e, List<string> featureNames)
        {
            var toReturn = new List<TreatmentResult>();

            _log.Error($"Exception caught getting treatments", e);

            foreach (var name in featureNames)
            {
                toReturn.Add(new TreatmentResult(name, Labels.Exception, Constants.Gral.Control));
            }

            return toReturn;
        }

        private bool IsSplitNotFound(API method, string featureFlagName, ParsedSplit parsedSplit, out TreatmentResult result)
        {
            result = null;

            if (parsedSplit != null)
                return false;

            _log.Warn($"{method}: you passed {featureFlagName} that does not exist in this environment, please double check what feature flags exist in the Split user interface.");

            result = new TreatmentResult(featureFlagName, Labels.SplitNotFound, Constants.Gral.Control);

            return true;
        }

        private static TreatmentResult ParseConfigurationAndReturnTreatment(ParsedSplit parsedSplit, TreatmentResult treatmentResult)
        {
            if (parsedSplit.configurations != null && parsedSplit.configurations.ContainsKey(treatmentResult.Treatment))
            {
                treatmentResult.Config = parsedSplit.configurations[treatmentResult.Treatment];
            }

            return treatmentResult;
        }

        private List<string> GetAndValidateFeatureFlagNamesByFlagSets(API method, Dictionary<string, HashSet<string>> namesByFlagSets)
        {
            var ffNamesToReturn = new HashSet<string>();
            foreach (var item in namesByFlagSets)
            {
                if (!item.Value.Any())
                {
                    _log.Warn($"{method}: you passed {item.Key} Flag Set that does not contain cached feature flag names, please double check what Flag Sets are in use in the Split user interface.");
                    continue;
                }

                ffNamesToReturn.UnionWith(item.Value);
            }

            return ffNamesToReturn.ToList();
        }
        #endregion
    }
}
