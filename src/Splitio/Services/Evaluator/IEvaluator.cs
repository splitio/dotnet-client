using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Evaluator
{
    public interface IEvaluator
    {
        TreatmentResult EvaluateFeature(string method, Key key, string featureName, Dictionary<string, object> attributes = null);
        MultipleEvaluatorResult EvaluateFeatures(string method, Key key, List<string> featureNames, Dictionary<string, object> attributes = null);        
    }
}
