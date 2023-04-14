﻿using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Evaluator
{
    public interface IEvaluator
    {
        TreatmentResult EvaluateFeature(Key key, string featureName, Dictionary<string, object> attributes = null);
        MultipleEvaluatorResult EvaluateFeatures(Key key, List<string> featureNames, Dictionary<string, object> attributes = null);        
    }
}
