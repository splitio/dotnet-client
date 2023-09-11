using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public interface IEvaluator
    {
        TreatmentResult EvaluateFeature(Key key, string featureName, Dictionary<string, object> attributes = null);
        MultipleEvaluatorResult EvaluateFeatures(Key key, List<string> featureNames, Dictionary<string, object> attributes = null);
        Task<TreatmentResult> EvaluateFeatureAsync(Key key, string featureName, Dictionary<string, object> attributes = null);
        Task<MultipleEvaluatorResult> EvaluateFeaturesAsync(Key key, List<string> featureNames, Dictionary<string, object> attributes = null);
    }
}
