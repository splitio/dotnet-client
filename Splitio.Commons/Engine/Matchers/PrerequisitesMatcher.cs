using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using Splitio.Commons.Engine.Evaluator;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Commons.Engine.Matchers
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
                    var evaluations = evaluator.EvaluateFeatures(Dto.API.Prerequisites, key, new List<string> { pr.FeatureFlagName }, attributes, trackLatency: false);

                    if (MatchRequisite(evaluations, pr)) return false;
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
                    var evaluations = await evaluator.EvaluateFeaturesAsync(Dto.API.Prerequisites, key, new List<string> { pr.FeatureFlagName }, attributes, trackLatency: false);
                    
                    if (MatchRequisite(evaluations, pr)) return false;
                }
            }

            return true;
        }

        public List<PrerequisitesDto> Get()
        {
            return _prerequisites ?? new List<PrerequisitesDto>();
        }

        private static bool MatchRequisite(List<TreatmentResult> evaluations, PrerequisitesDto prerequisites)
        {
            var evaluation = evaluations.FirstOrDefault();

            return evaluations.Count == 0 || evaluation != null && !prerequisites.Treatments.Contains(evaluation.Treatment);
        }
    }
}
