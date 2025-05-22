using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Matchers
{
    public class PrerequisitesMatcher : BaseMatcher
    {
        public List<PrerequisitesDto> _prerequisites { get; set; }
        public PrerequisitesMatcher(List<PrerequisitesDto> prerequisites = null)
        {
            _prerequisites = prerequisites ?? new List<PrerequisitesDto>();
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (_prerequisites != null)
            {
                foreach (var pr in _prerequisites)
                {
                    var evaluations = evaluator.EvaluateFeatures(Enums.API.Prerequisites, key, new List<string> { pr.FeatureFlagName }, attributes, trackLatency: false);
                    if (!pr.Treatments.Contains(evaluations.FirstOrDefault().Treatment))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override async Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            if (_prerequisites != null)
            {
                foreach (var pr in _prerequisites)
                {
                    var evaluations = await evaluator.EvaluateFeaturesAsync(Enums.API.Prerequisites, key, new List<string> { pr.FeatureFlagName }, attributes, trackLatency: false);
                    if (!pr.Treatments.Contains(evaluations.FirstOrDefault().Treatment))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
