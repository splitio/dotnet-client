using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Commons.Engine.Evaluator
{
    public interface IEvaluator
    {
        List<TreatmentResult> EvaluateFeatures(API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        Task<List<TreatmentResult>> EvaluateFeaturesAsync(Dto.API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        List<TreatmentResult> EvaluateFeaturesByFlagSets(Dto.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
        Task<List<TreatmentResult>> EvaluateFeaturesByFlagSetsAsync(Dto.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
    }
}
