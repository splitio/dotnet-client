using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public interface IEvaluator
    {
        List<TreatmentResult> EvaluateFeatures(Enums.API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        Task<List<TreatmentResult>> EvaluateFeaturesAsync(Enums.API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        List<TreatmentResult> EvaluateFeaturesByFlagSets(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
        Task<List<TreatmentResult>> EvaluateFeaturesByFlagSetsAsync(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
    }
}
