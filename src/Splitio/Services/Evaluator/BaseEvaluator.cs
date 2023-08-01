using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;

namespace Splitio.Services.Evaluator
{
    public class BaseEvaluator
    {
        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Evaluator));

        protected const string Control = "control";

        protected readonly ISplitter _splitter;
        protected readonly ISplitCache _splitCache;

        public BaseEvaluator(ISplitCache splitCache,
            ISplitter splitter)
        {
            _splitCache = splitCache;
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

        protected TreatmentResult IfMatchedGetTreatment(bool matched, Key key, ParsedSplit split, ConditionWithLogic condition)
        {
            if (!matched) return null;

            var treatment = _splitter.GetTreatment(key.bucketingKey, split.seed, condition.partitions, split.algo);

            return new TreatmentResult(condition.label, treatment, split.changeNumber);
        }
    }
}
