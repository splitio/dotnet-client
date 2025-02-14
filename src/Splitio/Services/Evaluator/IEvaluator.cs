using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public interface IEvaluator
    {
        List<ExpectedTreatmentResult> EvaluateFeatures(Enums.API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        Task<List<ExpectedTreatmentResult>> EvaluateFeaturesAsync(Enums.API method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null, bool trackLatency = true);
        List<ExpectedTreatmentResult> EvaluateFeaturesByFlagSets(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
        Task<List<ExpectedTreatmentResult>> EvaluateFeaturesByFlagSetsAsync(Enums.API method, Key key, List<string> flagSets, Dictionary<string, object> attributes = null);
    }
}
